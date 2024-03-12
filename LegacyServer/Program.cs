using System.Net.Sockets;
using System.Net;
using System.Text;
using LegacyServer;
using System.Text.Json;

namespace Swift
{
    class Server
    {
        static Data server = new Data();
        public static void Main(string[] args)
        {
            int port = 3000;
            UdpClient receiver = new UdpClient(port, AddressFamily.InterNetwork);

            // Get local machine's IP address dynamically
            string address = GetLocalIPAddress();
            Console.WriteLine("[+] Server started on {0}:{1}", address, port);

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
            await Task.Run(() =>
            {
                UdpClient c = (UdpClient)ar.AsyncState;
                IPEndPoint client = new IPEndPoint(IPAddress.Any, 0);
                Byte[] receivedBytes = c.EndReceive(ar, ref client);
                string data = ASCIIEncoding.ASCII.GetString(receivedBytes);
                if (!data.Contains("Player:Update"))
                {
                    Console.WriteLine(("[+] " + client + ": " + data + Environment.NewLine).Trim());
                }
                
                server.HandleData(data, c, client);

                c.BeginReceive(DataReceived, ar.AsyncState);
            });



        }
    }
}