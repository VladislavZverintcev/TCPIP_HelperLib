using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace TCPIP_HelperLib
{
    public class TCPIP_Helper
    {
        private bool isServerStarted = false;
        private int port;
        public int Port
        {
            get { return port; }
            set 
            { 
                if(port != value)
                {
                    if(value >= 0)
                    {
                        port = value;
                        StopServer();
                        StartServerAsync();
                    }
                }
            }
        }
        public event Action<string> Incoming;
        public event Action<IPAddress> ConnectionNonAvalible;
        public event Action<IPAddress, string> MessageSended;

        public bool IsServerStarted
        {
            get { return isServerStarted; }
        }
        public static IPAddress GetCurrentIp()
        {
            IPAddress localIP;
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
            {
                socket.Connect("8.8.8.8", 65530);
                IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
                localIP = endPoint.Address;
            }
            return localIP;
        }
        public TCPIP_Helper(int port_)
        {
            if(port_ < 0)
            {
                throw new ArgumentException();
            }
            port = port_;
            StartServerAsync();
        }
        public async void StartServerAsync()
        {
            await Task.Run(() => 
            {
                isServerStarted = true;
                var tcpEndPoint = new IPEndPoint(GetCurrentIp(), port);
                var tcpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                tcpSocket.Bind(tcpEndPoint);
                tcpSocket.Listen(5);
                while (isServerStarted)
                {
                    var listener = tcpSocket.Accept();
                    var buffer = new byte[256];
                    var size = 0;
                    var data = new StringBuilder();
                    do
                    {
                        size = listener.Receive(buffer);
                        data.Append(Encoding.UTF8.GetString(buffer, 0, size));
                    }
                    while (listener.Available > 0);
                    Incoming?.Invoke(data.ToString());
                    //Send answer massage
                    listener.Send(Encoding.UTF8.GetBytes("Server received an incoming message"));
                    listener.Shutdown(SocketShutdown.Both);
                    listener.Close();
                }
            }); 
        }
        public void StopServer()
        {
            isServerStarted = false;
        }
        public async void SendToAsync(IPAddress ip, string msg)
        {
            if(string.IsNullOrEmpty(msg))
            {
                return;
            }
            await Task.Run(() =>
            {
                var tcpEndPoint = new IPEndPoint(ip, port);
                var tcpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                var data = Encoding.UTF8.GetBytes(msg);
                try
                {
                    tcpSocket.Connect(tcpEndPoint);
                    tcpSocket.Send(data);
                    var buffer = new byte[256];
                    var size = 0;
                    var answer = new StringBuilder();
                    do
                    {
                        size = tcpSocket.Receive(buffer);
                        answer.Append(Encoding.UTF8.GetString(buffer, 0, size));
                    }
                    while (tcpSocket.Available > 0);
                    tcpSocket.Shutdown(SocketShutdown.Both);
                    tcpSocket.Close();
                    MessageSended?.Invoke(ip, msg);
                }
                catch (SocketException)
                {
                    ConnectionNonAvalible?.Invoke(ip);
                }
            });
        }
    }
}
