using Core.ProxyServer.Deleagates;
using Core.ProxyServer.DisposePattern;
using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Core.ProxyServer.ComponentBase
{
    public enum EIPType
    {
        LocalInternal, LocalExternal
    }
    // bộ lắng nghe đợi socket client kết nối
    public class BaseListener : DerivedClass
    {
        public IPAddress IPAddress { get; }
        public int Port { get; }
        public Socket ServerSocket { get; set; }
        public BaseListener(IPAddress iPAddress, int port)
        {
            IPAddress = iPAddress;
            Port = port;
        }
        public event OnAceptListener OnAcept;

        public async Task RunAsync()
        {
            ServerSocket = new Socket(IPAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            ServerSocket.Bind(new IPEndPoint(IPAddress, Port));
            ServerSocket.Listen(50);
            while (!disposed)
            {
                var SocketClient = await ServerSocket.AcceptAsync();
                Console.WriteLine("Accept Connect!");
                OnAcept?.Invoke(new Entities.StateListener
                {
                    Client = SocketClient
                });
            }
        }

        protected override void DisposeManaged()
        {
            try
            {
                ServerSocket.Shutdown(SocketShutdown.Both);
            }
            catch { }
            if (ServerSocket != null)
                ServerSocket.Close();
        }

        ///<summary>Checks whether the specified IP address is a remote IP address or not.</summary>
        ///<param name="IP">The IP address to check.</param>
        ///<returns>True if the specified IP address is a remote address, false otherwise.</returns>

        protected static bool IsRemoteIP(IPAddress IP)
        {
            var ips = IP.GetAddressBytes();
            byte First = ips.First();
            byte Second = ips[1];
            //Not 10.x.x.x And Not 172.16.x.x <-> 172.31.x.x And Not 192.168.x.x
            //And Not Any And Not Loopback And Not Broadcast
            return (First != 10) &&
                (First != 172 || (Second < 16 || Second > 31)) &&
                (First != 192 || Second != 168) &&
                (!IP.Equals(IPAddress.Any)) &&
                (!IP.Equals(IPAddress.Loopback)) &&
                (!IP.Equals(IPAddress.Broadcast));
        }
        ///<summary>Checks whether the specified IP address is a local IP address or not.</summary>
        ///<param name="IP">The IP address to check.</param>
        ///<returns>True if the specified IP address is a local address, false otherwise.</returns>
        protected static bool IsLocalIP(IPAddress IP)
        {
            var ips = IP.GetAddressBytes();
            byte First = ips.First();
            byte Second = ips[1];
            //10.x.x.x Or 172.16.x.x <-> 172.31.x.x Or 192.168.x.x
            return (First == 10) ||
                (First == 172 && (Second >= 16 && Second <= 31)) ||
                (First == 192 && Second == 168);
        }
        private static Func<IPAddress, bool> IsCheckIP(EIPType eIPType)
        {
            switch (eIPType)
            {
                case EIPType.LocalInternal:
                    return IsRemoteIP;
                case EIPType.LocalExternal:
                    return IsLocalIP;
                default:
                    return null;
            }
        }
        public static IPAddress GetIP(EIPType eIPType)
        {
            Func<IPAddress, bool> action = IsCheckIP(eIPType);
            try
            {
                IPHostEntry he = Dns.GetHostEntry(Dns.GetHostName());
                var TempIP = he.AddressList.FirstOrDefault(action);
                if (TempIP == null)
                {
                    TempIP = he.AddressList[0];
                }
                return TempIP;
            }
            catch
            {
                return IPAddress.Any;
            }
        }

    }
}
