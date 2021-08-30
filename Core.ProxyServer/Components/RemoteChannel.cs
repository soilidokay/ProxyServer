using Core.ProxyServer.ComponentBase;
using Core.ProxyServer.DisposePattern;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Core.ProxyServer.Components
{
    public class RemoteChannel : DerivedClass
    {
        public SocketClient SourceSocket { get; set; }
        public SocketClient TargetSocket { get; private set; }
        public HttpHeader HttpHeader { get; set; }
        public string Query { get; set; }
        public RemoteChannel(Socket socket)
        {
            SourceSocket = new SocketClient(socket);
        }
        //kết nối tới mục tiêu
        public async Task<Socket> ConnectTargetAsync()
        {
            if (!HttpHeader.Headers.ContainsKey("Host"))
            {
                throw new Exception("Host is invalid!");
            }
            int Port;
            string Host;
            int Ret;
            //kết nối là https sẽ có method là connect
            if (HttpHeader.Method.ToUpper().Equals("CONNECT"))
            { //HTTPS
                Ret = HttpHeader.QueryPath.IndexOf(":");
                if (Ret >= 0)
                {
                    Host = HttpHeader.QueryPath.Substring(0, Ret);
                    if (HttpHeader.QueryPath.Length > Ret + 1)
                        Port = int.Parse(HttpHeader.QueryPath.Substring(Ret + 1));
                    else
                        Port = 443;
                }
                else
                {
                    Host = HttpHeader.QueryPath;
                    Port = 443;
                }
            }
            else
            { //Normal HTTP
                Ret = HttpHeader.Headers["Host"].IndexOf(":");
                if (Ret > 0)
                {
                    Host = HttpHeader.Headers["Host"].Substring(0, Ret);
                    Port = int.Parse(HttpHeader.Headers["Host"][(Ret + 1)..]);
                }
                else
                {
                    Host = HttpHeader.Headers["Host"];
                    Port = 80;
                }
            }
            try
            {
               // mở socket tới mục tiêu và yêu cầu kết nối
                IPEndPoint TargetEndPoint = new IPEndPoint(Dns.GetHostEntry(Host).AddressList[0], Port);
                
                var socket = new Socket(TargetEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
               
                if (HttpHeader.Headers.ContainsKey("Proxy-Connection") && HttpHeader.Headers["Proxy-Connection"].ToLower().Equals("keep-alive"))
                    socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, 1);
               
                await socket.ConnectAsync(TargetEndPoint);
                
                return socket;
            }
            catch
            {
                throw new Exception($"Can not connect to {Host}");
            }

        }
        //thiết lập đường ống gữi và nhận giữa clien và mục tiêu
        public async Task SetupPipeAsync()
        {
            var Target = await ConnectTargetAsync();

            TargetSocket = new SocketClient(Target);
            TargetSocket.Name = "Target";

            try
            {
                string rq;
                if (HttpHeader.Method.ToUpper().Equals("CONNECT"))
                { //HTTPS
                    rq = HttpHeader.HttpVersion + " 200 Connection established\r\nProxy-Agent: Mentalis Proxy Server\r\n\r\n";
                    await SourceSocket.Socket.SendAsync(Encoding.ASCII.GetBytes(rq), SocketFlags.None);
                }
                else
                { //Normal HTTP
                    rq = RebuildQuery();
                    await Target.SendAsync(Encoding.ASCII.GetBytes(rq), SocketFlags.None);
                }
                //thiết lập đường ống
                var Task1 = SourceSocket.PipeSenderAsync(TargetSocket.Socket);
                var Task2 = TargetSocket.PipeSenderAsync(SourceSocket.Socket);
                //đợi cho đến khi truyền gữi kết thúc
                await Task.WhenAll(Task1, Task2);
            }
            catch
            {
                Dispose();
            }
        }
        private string RebuildQuery()
        {
            string ret = HttpHeader.Method + " " + HttpHeader.QueryPath + " " + HttpHeader.HttpVersion + "\r\n";
            if (HttpHeader.Headers != null)
            {
                foreach (string sc in HttpHeader.Headers.Keys)
                {
                    if (sc.Length < 6 || !sc.Substring(0, 6).Equals("proxy-"))
                    {
                        ret += System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(sc) + ": " + (string)HttpHeader.Headers[sc] + "\r\n";
                    }
                }
                ret += "\r\n";
                if (HttpHeader.Body != null)
                {
                    ret += HttpHeader.Body;
                }
            }
            return ret;
        }
        //hàm tổng hợp xử
        public async Task StartAsync()
        {
            try
            {
                //đọc header http từ client
                HttpHeader = await SourceSocket.ReadHeaderAsync();
                //thiết lập đường ống
                await SetupPipeAsync();
            }
            catch(Exception e)
            {
                await BadRequestAsync(e.Message);
            }

            Dispose();
        }
        private async Task BadRequestAsync(string message = null)
        {
            string brs = $"HTTP/1.1 400 Bad Request\r\nConnection: close\r\nContent-Type: text/html\r\n\r\n<html><head><title>400 Bad Request:{message}</title></head><body><div align=\"center\"><table border=\"0\" cellspacing=\"3\" cellpadding=\"3\" bgcolor=\"#C0C0C0\"><tr><td><table border=\"0\" width=\"500\" cellspacing=\"3\" cellpadding=\"3\"><tr><td bgcolor=\"#B2B2B2\"><p align=\"center\"><strong><font size=\"2\" face=\"Verdana\">400 Bad Request</font></strong></p></td></tr><tr><td bgcolor=\"#D1D1D1\"><font size=\"2\" face=\"Verdana\"> The proxy server could not understand the HTTP request!<br><br> Please contact your network administrator about this problem.</font></td></tr></table></center></td></tr></table></div></body></html>";
            try
            {
                await SourceSocket.Socket.SendAsync(Encoding.ASCII.GetBytes(brs), SocketFlags.None);
            }
            catch
            {
                Dispose();
            }
        }
        protected override void DisposeManaged()
        {
            base.DisposeManaged();
            SourceSocket?.Dispose();
            TargetSocket?.Dispose();
        }
    }
}
