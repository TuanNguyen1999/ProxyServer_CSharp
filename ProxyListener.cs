using System;
using System.Net.Sockets;
using System.Net;
using System.Threading;

namespace ProxyServer
{
    public class ProxyListener
    {
        private int m_proxyPort;
        private TcpListener m_tcpListener;
        private TcpClient m_tcpClient;
        private Thread m_processThread;



        public ProxyListener()
        {
            m_proxyPort = 8888;
            m_tcpListener = new TcpListener(IPAddress.Any, m_proxyPort);
            m_tcpClient = null;
        }

        public void Start()
        {
            m_processThread = new Thread(DoListening)
            {
                IsBackground = true,
                Name = "Listening for clients",
            };
            m_processThread.SetApartmentState(ApartmentState.STA);
            m_processThread.Start();
        }

        public void Stop()
        {
            m_tcpListener.Stop();
            m_processThread.Join();
        }

        protected void DoListening()
        {
            m_tcpListener.Start(20);
            while (true)
            {
                try
                {
                    m_tcpClient = m_tcpListener.AcceptTcpClient();
                    if (m_tcpClient == null) throw new InvalidOperationException("ProxyListener.DoListening");

                    ClientHandler handler = new ClientHandler(m_tcpClient, App.GetApp().BannedDomains);
                    handler.Start();
                }
                catch (SocketException e)
                {
                    if(e.ErrorCode != 10004)
                    {
                        Logging.Log(ClientHandler.SocketExceptionInfo(e), LoggingLevel.Error);
                        throw e;
                    }
                    return;
                }

                //try
                //{
                //    Logging.Log(string.Format("{0} connected", GetClientIP(m_tcpClient)));
                //}
                //catch (ObjectDisposedException)
                //{

                //}
            }
        }

        static public string GetClientIP(TcpClient tcpClient)
        {
            IPAddress ipaddress = ((IPEndPoint)tcpClient.Client.RemoteEndPoint).Address;
            return ipaddress.MapToIPv4().ToString();
        }
    }
}
