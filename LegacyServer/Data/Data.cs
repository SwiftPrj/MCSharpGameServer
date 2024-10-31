using LegacyServer.Packets;
using NLog;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace LegacyServer
{
    public class Data
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        static int playercounter = 0;
        static List<Player> players = new List<Player>();
        static List<Dictionary<string, string>> instances = new List<Dictionary<string, string>>();
        TimerUtils timer = new TimerUtils();
        TimerUtils heartbeat = new TimerUtils();
        TimerUtils heartbeatKick = new TimerUtils();
        List<Player> heartbeats = new List<Player>();
        PacketHandler handler = new PacketHandler();

        public void LoadMap()
        {
            for (int i = 0; i < 15; i++)
            {
                Dictionary<string, string> keyValuePairs = new Dictionary<string, string>();
                Random x = new Random();
                Random y = new Random();
                keyValuePairs.Add("name", "par_solid");
                keyValuePairs.Add("x", x.Next(0, 1024).ToString());
                keyValuePairs.Add("y", y.Next(0, 1024).ToString());
                CreateInstance(keyValuePairs, "par_solid");
                Logger.Info("Solid Object Created at " + keyValuePairs.GetValueOrDefault("x") + " , " + keyValuePairs.GetValueOrDefault("y"));
                
            }
        }

        public bool ValidateAndSanitizeInput(string input, out string sanitizedOutput)
        {
            sanitizedOutput = input.Trim();

            if (string.IsNullOrWhiteSpace(sanitizedOutput))
            {
                Logger.Error("packet is either null or contains illegal characters");
                return false; 
            }

            if (sanitizedOutput.Length > 1024) 
            {
                Logger.Error("packet exceeds 1024 bytes");
                return false; 
            }

            sanitizedOutput = sanitizedOutput.Replace("<", "&lt;").Replace(">", "&gt;");

            return true;
        }

        private string SanitizeString(string input)
        {
            return input.Replace("\0", "").Trim(); 
        }

        public async Task HandleData(string data, UdpClient c, IPEndPoint endpoint)
        {
            try
            {
                if (!ValidateAndSanitizeInput(data, out string sanitizedData))
                {
                    Logger.Error($"Invalid data received: {data}");
                    return;
                }

                var parse = sanitizedData.Trim();


                try
                {

                    PacketType packetType;
                    var packetTypeString = SanitizeString(parse.Contains(":") ? parse.Split(":")[0] : parse);

                    if (!Enum.TryParse<PacketType>(packetTypeString, true, out packetType))
                    {
                        Logger.Error($"Failed to parse packet type from string: {packetTypeString}");
                        return; 
                    }


                    var dictionaryData = new Dictionary<PacketType, string>();

                    if (!parse.Contains(":"))
                    {
                        dictionaryData.Add(packetType, ""); // empty data means that we are only interested in the packet type
                    } else
                    {
                        int colonIndex = sanitizedData.IndexOf(':');
                        dictionaryData.Add(packetType, sanitizedData.Substring(colonIndex + 1).Trim());
                    }

                    handler.HandlePacket(new Packet(packetType, dictionaryData, c, endpoint), this);
                } catch (Exception e) { Logger.Warn(e.Message); }

                
                if (GetPlayerCounter() > 0)
                {
                    if (timer.Wait(100))
                    {
                        Logger.Info("sent player counter to all connected clients");

                        Dictionary<PacketType, string> keyValuePairs = new Dictionary<PacketType, string>();

                        keyValuePairs.Add(PacketType.S06PacketStatusUpdate, GetPlayerCounter().ToString());

                        Packet packet = new Packet(PacketType.S06PacketStatusUpdate, keyValuePairs, c, endpoint);

                        handler.SendPacket(packet);
                    }
                }
            }
            catch (JsonException jsonEx)
            {
                Logger.Error($"JSON parsing error: {jsonEx.Message}");
            }
            catch (Exception ex)
            {
                Logger.Error($"Error handling data: {ex.Message}");
            }
        }

        public List<Dictionary<string, string>> GetInstances()
        {
            return instances;
        }

        public void CreateInstance(Dictionary<string, string> map, string name)
        {
            if (!instances.Contains(map))
            {
                if (Objects.GetObjects().Contains(name))
                {
                    if (map.GetValueOrDefault("name") != "obj_projectile")
                    {
                        instances.Add(map);
                    }

                    SendStringAll(SanitizeString("S02PacketInstanceCreate:" + JSONParser.EncodeString(map).Trim()));
                }
            }
        }

        public void DestroyInstance(Dictionary<string, string> map, string name, string x, string y)
        {

            if (Objects.GetObjects().Contains(name))
            {
                foreach (var keyValuePairs in instances)
                {
                    if (keyValuePairs.GetValueOrDefault("name") == name && keyValuePairs.GetValueOrDefault("x") == x && keyValuePairs.GetValueOrDefault("y") == y)
                    {
                        Logger.Info("instance {0} removed ", name);
                        instances.Remove(keyValuePairs);
                    }
                }   
                SendStringAll(SanitizeString("S02PacketInstanceDestroy:" + JSONParser.EncodeString(map).Trim()));
                
            }
        }

        public static void AddPlayer(Player player) 
        {
            if (!players.Contains(player))
            {
                players.Add(player);
                playercounter = players.Count;
            }
        }

        public static void RemovePlayer(Player player) 
        {
            SendString("GAME_END", player.GetClient(), player.GetEndPoint());

            SendStringAll(PacketType.C02PacketPlayerDisconnected.ToString() + ":" + player.GetID());
            players.Remove(player);
            playercounter = players.Count;
        }

        public static Player? GetPlayer(string ip) // get player via IP
        {
            foreach (Player p in players)
            {
                if (p.GetInfo()[2] == ip)
                {
                    return p;
                }
            }
            return null;
        }
        public Player? GetPlayerPort(int port) // get player via Port
        {
            foreach (Player p in players)
            {
                if ((int)p.GetInfo()[3] == port)
                {
                    return p;
                }
            }
            return null;
        }

        public static Player? GetPlayerID(int id) // get player via ID
        {
            foreach (Player p in players)
            {
                if ((int)p.GetInfo()[1] == id)
                {
                    return p;
                }
            }
            return null;
        }

        public static List<Player> GetPlayers()
        {
            return players;
        }

        public int GetPlayerCounter()
        {
            return GetPlayers().Count;
        }

        public static void SendString(string data, UdpClient c, IPEndPoint client)
        {
            try
            {
                c.Send(Encoding.ASCII.GetBytes(data), Encoding.ASCII.GetBytes(data).Length, client);
            }
            catch (SocketException ex)
            {
                Logger.Warn($"SocketException: {ex.Message}, ErrorCode: {ex.SocketErrorCode}");
                Logger.Error(ex.StackTrace);
            }
        }

        public static void SendStringAll(string data)
        {
            try
            {
                foreach (Player p in GetPlayers())
                {
                    IPEndPoint endpoint = p.GetEndPoint();
                    UdpClient c = p.GetClient();
                    SendString(data, c, endpoint);
                }
            } catch (SocketException ex) {  Console.WriteLine(ex.StackTrace ); }

        }

        public void SendInt(int data, UdpClient c, IPEndPoint client)
        {
            c.Send(BitConverter.GetBytes(data), data, client);
        }

    }
}
