using Core.ProxyServer.Components;
using Core.ProxyServer.DisposePattern;
using System.Buffers;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Core.ProxyServer.ComponentBase
{
    //một lớp bọc socket class
    public class SocketClient : DerivedClass
    {
        public Socket Socket { get; set; }

        private readonly byte[] _Buffer = new byte[512];
        public string Query { get; set; }
        public string Name { get; set; }
        private CancellationTokenSource cancellationTokenSource;
        public SocketClient(Socket socket)
        {
            Socket = socket;
            cancellationTokenSource = new CancellationTokenSource();
        }
        //đọc phần header của request socket
        public async Task<HttpHeader> ReadHeaderAsync()
        {
            Query = "";
            HttpHeader httpHeader = null;
            var IsStop = false;
            while (!IsStop)
            {
                var ret = await Socket.ReceiveAsync(_Buffer, SocketFlags.None);

                if (ret <= 0) { break; }

                Query += Encoding.UTF8.GetString(_Buffer);

                if (HttpHeader.IsValidQuery(Query))
                {
                    httpHeader = HttpHeader.Parse(Query);
                    break;
                }
            }
            return httpHeader;
        }

        //thiết lập đường ống nhận từ nguồn và gữi đến đích
        public async Task PipeSenderAsync(Socket target)
        {
            while (true)
            {
                var ret = await Socket.ReceiveAsync(_Buffer, SocketFlags.None);
                if (ret <= 0) { break; }
                ret = await target.SendAsync(_Buffer.Take(ret).ToArray(), SocketFlags.None);
                if (ret <= 0) { break; }
            }
        }
        protected override void DisposeManaged()
        {
            base.DisposeManaged();
            cancellationTokenSource.Cancel();
            try
            {
                Socket?.Shutdown(SocketShutdown.Both);
            }
            catch {
            }
            Socket?.Close();
        }
    }
}
