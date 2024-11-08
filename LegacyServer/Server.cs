using System.Net.Sockets;
using System.Net;
using System.Text;
using NLog;
using LegacyServer.Utils;

namespace LegacyServer
{
    class Server
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        static Data server = new Data();
        public static void Main(string[] args)
        {
            Console.WriteLine("loading");
            FileUtils.CreateConfigDirectory("settings");
            Thread.Sleep(1000);
            Console.Clear();
            int port = FileUtils.GetPort();
            Logger.Info("Port: {0}", port);
            string address = FileUtils.GetIP();
            UdpClient receiver = new UdpClient(port, AddressFamily.InterNetwork);

            Logger.Info("Server started on {0}:{1}", address, port);

            Logger.Info("GameLix Server Engine initalized");

            receiver.BeginReceive(DataReceived, receiver);
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            server.LoadMap();
            Console.ReadKey();
        }

        static string GetLocalIPAddress()
        {
            string localIP = string.Empty;
            IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());

            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    localIP = ip.ToString();
                    break;
                }
            }

            return localIP;
        }
        private static async void DataReceived(IAsyncResult ar)
        {
            try
            {
                UdpClient c = (UdpClient)ar.AsyncState;
                IPEndPoint client = new IPEndPoint(IPAddress.Parse(FileUtils.GetIP()), 0);
                Byte[] receivedBytes = c.EndReceive(ar, ref client);
                string data = Encoding.ASCII.GetString(receivedBytes);

                if (!data.Contains("C03PacketPlayerUpdate"))
                {
                    Logger.Info(("[+] " + client + ": " + data + Environment.NewLine).Trim());
                }

                await server.HandleData(data, c, client);

                c.BeginReceive(DataReceived, ar.AsyncState);
            }
            catch (Exception ex)
            {
                Logger.Error($"Error in DataReceived: {ex.Message}");
            }
        }
    }
}