using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net.Sockets;
using System.Net;
using System.Collections.ObjectModel;
using System.IO;
using System.Security.AccessControl;

namespace ProxyServer
{
    public class ClientHandler
    {
        public static double CacheAge = 3600 * 24;
        public static int InstanceCount { get; private set; } = 0;

        private Socket m_ClientConnected;
        private Thread m_processThread;
        public byte[] Buffer { get; set; }
        public int BufferSize { get; set; } = 65000;

        public ObservableCollection<WebDomain> BannedDomains { get; set; } = new ObservableCollection<WebDomain>();
        /// <summary>
        /// the amount of bytes read or received from last I/O operation
        /// </summary>
        public int ByteCount { get; private set; }
        /// <summary>
        /// Create a new client handler, this should be used with ProxyListener
        /// </summary>
        /// <param name="capacity">The size of buffer used for sending and recieving data</param>
        /// <param name="connected">Client's socket to be handled</param>

        public ClientHandler(TcpClient connected, ObservableCollection<WebDomain> domains)
        {
            m_ClientConnected = connected.Client ?? throw new ArgumentNullException("ClientHandler.ClientHandler");
            BannedDomains = domains ?? throw new ArgumentNullException("ClientHandler.ClientHandler");

            Buffer = new byte[BufferSize];
        }
        public void Start()
        {
            m_processThread = new Thread(DoTransferData)
            {
                IsBackground = true,
                Name = "Handling Client"
            };

            m_processThread.Start();

            // Store running threads
            InstanceCount++;

            Logging.Log(string.Format("One ClientHandler instance starts. Current: {0}", InstanceCount),LoggingLevel.Warning);
        }

        /// <summary>
        /// Receive client's request in byte array and convert it to a request string
        /// </summary>
        /// <returns>request string</returns>
        protected string GetClientRequest(byte[] ClientBuffer)
        {
            string rqst = string.Empty;

            if (ClientBuffer == null) return string.Empty;

            // Translate bytes into request string
            foreach (byte b in ClientBuffer)
            {
                if (b == 0) break;
                rqst = rqst.Insert(rqst.Length, string.Format("{0}", Convert.ToChar(b)));
            }

            return rqst;
        }

        /// <summary>
        /// Receive data from given source, clear buffer before starting receiving
        /// </summary>
        /// <param name="source">TcpClient to get data from, if source is null or is not connected then <see langword="null"/> is return</param>
        protected byte[] ReceiveBytesFromClient(Socket source)
        {

            if (source == null || source.Connected == false) return null;
            try
            {
                List<byte> DataBuffer = new List<byte>();


                bool CheckEndReqeust()
                {
                    if (Buffer[ByteCount - 1] == 10 && Buffer[ByteCount - 2] == 13 && Buffer[ByteCount - 3] == 10 && Buffer[ByteCount - 4] == 13)
                        return true;
                    else return false;
                }

                Array.Clear(Buffer, 0, BufferSize);
                ByteCount = source.Receive(Buffer);

                while (ByteCount > 0)
                {
                    for (int i = 0; i < ByteCount; i++)
                        DataBuffer.Add(Buffer[i]);

                    if (CheckEndReqeust()) break;

                    Array.Clear(Buffer, 0, BufferSize);
                    ByteCount = source.Receive(Buffer);
                }

                return DataBuffer.ToArray();
            }
            catch (SocketException e)
            {
                if (e.ErrorCode == 10053) return null;
                Logging.Log(SocketExceptionInfo(e), LoggingLevel.Error);
                throw e;
            }
        }

        protected void TrySendRequestToServer(ref Socket s, string host, int port, byte[] RequestData, out int IsSuccess)
        {
            try
            {
                Task task = s.ConnectAsync(host, port);

                bool flag = task.Wait(10000);
                if (flag)
                {
                    s.Send(RequestData);
                }

                else IsSuccess = 0;
                IsSuccess = 1;
            }
            catch (SocketException e)
            {
                Logging.Log(SocketExceptionInfo(e), LoggingLevel.Warning);
                IsSuccess = 0;
            }
            catch (AggregateException e)
            {
                try
                {
                    throw e.InnerException;
                }
                catch (SocketException inner)
                {
                    Logging.Log(SocketExceptionInfo(inner), LoggingLevel.Warning);
                    IsSuccess = -1;
                }
                catch(Exception inner)
                {
                    Logging.Log(ExceptionInfo(inner), LoggingLevel.Error);
                    throw inner;
                }
            }

        }

        /// <exception cref="SocketException"/>
        protected void SendResponseToClient(string msg)
        {
            if (string.IsNullOrEmpty(msg) || m_ClientConnected.Connected == false) return;
            try
            {
                byte[] LocalBuffer = new byte[msg.Length];
                Encoding.ASCII.GetBytes(msg, 0, msg.Length, LocalBuffer, 0);
                m_ClientConnected.Send(LocalBuffer);
            }
            catch (SocketException e)
            {
                if (e.ErrorCode == 10053)
                {
                    m_ClientConnected.Close();
                }
                Logging.Log(SocketExceptionInfo(e), LoggingLevel.Error);
                throw e;
            }
        }

        /// <exception cref="SocketException"/>
        protected void SendResponseToClient(FileStream stream)
        {
            if (stream == null || m_ClientConnected.Connected == false) return;
            try
            {
                Array.Clear(Buffer, 0, BufferSize);
                ByteCount = stream.Read(Buffer, 0, BufferSize);
                while (ByteCount > 0)
                {
                    m_ClientConnected.Send(Buffer, 0, ByteCount, SocketFlags.None);

                    Array.Clear(Buffer, 0, ByteCount);
                    ByteCount = stream.Read(Buffer, 0, BufferSize);
                }
            }
            catch (SocketException e)
            {
                if (e.ErrorCode == 10053)
                {
                    m_ClientConnected.Close();
                }
                Logging.Log(SocketExceptionInfo(e), LoggingLevel.Error);
                throw e;
            }
        }

        /// <exception cref="SocketException"/>
        protected void SendResponseToClient(byte[] buffer, int size, int offset = 0)
        {
            if (buffer == null || m_ClientConnected.Connected == false) return;
            try
            {
                m_ClientConnected.Send(buffer, offset, size, SocketFlags.None);
            }
            catch (SocketException e)
            {
                if (e.ErrorCode == 10053)
                {
                    m_ClientConnected.Close();
                }
                Logging.Log(SocketExceptionInfo(e), LoggingLevel.Error);
                throw e;
            }
        }

        public string GetFileName(string uri)
        {
            List<char> file = new List<char>();

            foreach (char ch in uri)
            {
                // any character which isnt a alphabet character is replaced with a '.'
                if (ch >= 'A' && ch <= 'Z' || ch >= 'a' && ch <= 'z') file.Add(ch);
                else file.Add(',');
            }

            return new string(file.ToArray());
        }
        protected void DoTransferData()
        {
            while (true)
            {
                if (!m_ClientConnected.Connected) break;

                string rqst = string.Empty;
                byte[] RequestBuffer = null;

                // Try receiving client's request,
                // If false to receive request then disconnect from client
                RequestBuffer = ReceiveBytesFromClient(m_ClientConnected);

                if (RequestBuffer != null && RequestBuffer.Length != 0)
                    rqst = GetClientRequest(RequestBuffer);

                if (string.IsNullOrEmpty(rqst)) break;

                // Parse client's request -> Method, Uri, HTTP version, Host, Port
                HttpRequestHeader httpHeader = HttpRequestHeader.HttpRequestParser(rqst);

                if(httpHeader == null)
                {
                    // Send "400 Bad Request" response and disonnect
                    SendResponseToClient(" 400 Bad Request\r\n");
                    break;
                }
                else
                {
                    if (httpHeader.Method == "GET" || httpHeader.Method == "POST")
                        Logging.Log(string.Format("{0} {1} {2}\nHost: {3}", httpHeader.Method, httpHeader.Uri, httpHeader.HttpVer, httpHeader.Host));
                }

                // Try connecting and sending request to server if successfully connected in a seperate thread,
                // which returns a flag indicating the result of operation
                int ResultFlag = 0;
                TcpClient tcp = new TcpClient();
                Socket HostSocket = tcp.Client;

                Thread ConnectHostThread = new Thread(() =>
                {
                    TrySendRequestToServer(ref HostSocket,
                                              httpHeader.Host, Convert.ToInt32(httpHeader.Port),
                                              RequestBuffer,
                                              out ResultFlag);
                })
                {
                    Name = "Try Send request to host",
                    IsBackground = true
                };
                ConnectHostThread.Start();

                // Check for supported methods
                if (httpHeader.Method != "GET" && httpHeader.Method != "POST")
                {

                    string httpResponse = httpHeader.HttpVer + " 501 Not Implemented\r\n";
                    SendResponseToClient(httpResponse);
                    ConnectHostThread.Abort();
                    break;
                }

                //
                // Close connection if requested domain is banned
                // 
                bool IsDomainBanned = false;
                foreach (var domain in BannedDomains)
                {
                    if (httpHeader.Host == domain.ToString())
                    {
                        string httpResponse = httpHeader.HttpVer + " 403 Forbbiden\r\n";
                        SendResponseToClient(httpResponse);
                        IsDomainBanned = true;
                        break;
                    }
                }
                if (IsDomainBanned) break;


                // Try using cached data
                bool ToUseCache = false;
                string CacheFile = Path.Combine(App.CacheFolder, GetFileName(httpHeader.Uri)) + ".FILE";

                if (File.Exists(CacheFile))
                {
                    // Get different in seconds between today and last modified day
                    DateTime LastModified = File.GetLastAccessTime(CacheFile);
                    TimeSpan diff = DateTime.Now.Subtract(LastModified);

                    // Use cache if the different is less than 24 hours
                    ToUseCache = diff.TotalSeconds < CacheAge;
                }

                if (ToUseCache)
                {
                    // Use cached data for server's response
                    FileStream ReadStream = null;
                    try
                    {
                        ReadStream = new FileStream(CacheFile, FileMode.Open, FileAccess.Read, FileShare.Read);
                        SendResponseToClient(ReadStream);

                        Logging.Log(string.Format("{0} {1} used cached data for server's response.", httpHeader.Method, httpHeader.Uri), LoggingLevel.Warning);
                    }
                    catch (SocketException e)
                    {
                        if (e.ErrorCode == 10053)
                        {
                            ReadStream.Close();
                            Logging.Log(string.Format("{0} {1} exited with SocketException. Error code {2}.",
                                    httpHeader.Method,
                                    httpHeader.Uri,
                                    e.ErrorCode),
                                LoggingLevel.Warning);

                            break;
                        }
                        Logging.Log(ExceptionInfo(e), LoggingLevel.Error);
                        throw e;
                    }
                    finally
                    {
                        // Close stream
                        ReadStream.Close();
                    }
                    continue;
                }

                // if connection attemp is success, redirect server's response to client
                ConnectHostThread.Join();
                if (ResultFlag == 1)
                {
                    // Get data from server and cache to a file
                    string TemporaryFile = Path.GetTempFileName();
                    FileStream WriteStream = new FileStream(TemporaryFile, FileMode.Open, FileAccess.Write, FileShare.Delete);
                    try
                    {
                        Array.Clear(Buffer, 0, BufferSize);
                        ByteCount = HostSocket.Receive(Buffer);
                        while (ByteCount > 0)
                        {
                            // Send data to client
                            SendResponseToClient(Buffer, ByteCount);

                            // Write data to cache file
                            WriteStream.Write(Buffer, 0, ByteCount);

                            if (Buffer[ByteCount - 1] == 0 && Buffer[ByteCount - 2] == 0)   //for gzip, deflate encoding
                                break;

                            int retry = 0;
                            while (++retry <= 10)   // for chunked encoding
                            {
                                if (HostSocket.Available != 0) break;
                                Thread.Sleep(100);
                            }
                            if (retry > 10) break;

                            Array.Clear(Buffer, 0, ByteCount);
                            ByteCount = HostSocket.Receive(Buffer);
                        }
                    }
                    catch (SocketException e)
                    {
                        Logging.Log(SocketExceptionInfo(e), LoggingLevel.Error);
                        if (e.ErrorCode == 10053)
                        {
                            // Data may be not fully tranfered so that cached file should be deleted
                            File.Delete(TemporaryFile);

                            Logging.Log(string.Format("{0} {1} exited with SocketException. Error code {2}.",
                                    httpHeader.Method,
                                    httpHeader.Uri,
                                    e.ErrorCode),
                                LoggingLevel.Warning);

                            break;
                        }
                        else throw e;
                    }
                    finally
                    {
                        HostSocket.Close();
                        WriteStream.Close();
                        // If successfully cached,
                        // Copy data from temporary file to new file stored in cache folder
                        try
                        {
                            // Temporary maybe deleted because SocketException 10053 is thrown
                            if(File.Exists(TemporaryFile))
                                File.Copy(TemporaryFile, CacheFile, true);
                        }
                        catch (DirectoryNotFoundException)
                        {
                            // Invalid Path from CacheFile
                        }
                        finally
                        {
                            File.Delete(TemporaryFile);
                        }
                    }
                }
                else
                {
                    if (ResultFlag == 0)
                        HostSocket.Close();
                    else
                    {
                        string httpResponse = httpHeader.HttpVer;
                        httpResponse += " 404 Not Found\r\n";

                        SendResponseToClient(httpResponse);

                        ConnectHostThread.Abort();
                        break;
                    }

                }
                if (httpHeader.Method == "GET" || httpHeader.Method == "POST")
                    Logging.Log(string.Format("{0} {1} is Handled successfully.", httpHeader.Method, httpHeader.Uri));

            }

            m_ClientConnected.Close();

            // Remove thread from list
            InstanceCount--;

            Logging.Log(string.Format("One ClientHandler instance exit. Remain: {0}", InstanceCount), LoggingLevel.Warning);
        }
        public static string SocketExceptionInfo(SocketException e)
        {
            return string.Format("{0} throws SocketException:\nMessage: {1}\nSource: {2}\nError code: {3} - {4}",
                                            e.TargetSite,
                                            e.Message,
                                            e.Source,
                                            e.ErrorCode,
                                            e.SocketErrorCode);
        }
        public static string ExceptionInfo(Exception e)
        {
            return string.Format("{0} throws Exception:\nMessage: {1}\nSource: {2}",
                                            e.TargetSite,
                                            e.Message,
                                            e.Source);
        }
    }

    public class HttpRequestHeader
    {
        public string Method { get; private set; } = "";
        public string Uri { get; private set; } = "";
        public string HttpVer { get; private set; } = "";
        public string Host { get; private set; } = "";
        public string Port { get; private set; } = "";

        private HttpRequestHeader() { }
        static public HttpRequestHeader HttpRequestParser(string httprequest)
        {
            if (string.IsNullOrEmpty(httprequest)) throw new ArgumentNullException("HttpRequestHeader.HttpRequestParser");

            try
            {
                string method, http, uri, hostName, strPort;

                // Get request's method
                int methodStartPos = httprequest.IndexOf(' ');
                method = httprequest.Substring(0, methodStartPos);

                // Get request's uri after method
                int uriStartPos = httprequest.IndexOf(' ', methodStartPos + 1);
                uri = httprequest.Substring(methodStartPos + 1, uriStartPos - methodStartPos - 1);

                // Get http version
                int httpStartPos = httprequest.IndexOfAny(new char[2] { '\r', '\n' }, uriStartPos + 1);
                http = httprequest.Substring(uriStartPos + 1, httpStartPos - uriStartPos - 1);

                //Get host name and its port
                int hostStartPos = httprequest.IndexOf("Host: ", httpStartPos + 1);
                int hostNameEndPos = httprequest.IndexOfAny(new char[3] { '\r', '\n', ':' }, hostStartPos + 6);
                hostName = httprequest.Substring(hostStartPos + 6, hostNameEndPos - hostStartPos - 6);

                if (httprequest[hostNameEndPos] == ':')
                {
                    int portEndPos = httprequest.IndexOfAny(new char[2] { '\r', '\n' }, hostNameEndPos + 1);
                    strPort = httprequest.Substring(hostNameEndPos + 1, portEndPos - hostNameEndPos - 1);
                }
                else strPort = "80";

                return new HttpRequestHeader() { Host = hostName, HttpVer = http, Method = method, Port = strPort, Uri = uri };
            }
            catch (ArgumentOutOfRangeException)
            {
                return null;
            }
        }
    }
}
