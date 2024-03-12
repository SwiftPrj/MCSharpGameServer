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
        int playercounter = 0;
        List<Player> players = new List<Player>();
        List<Dictionary<string, string>> instances = new List<Dictionary<string, string>>();
        private class PlayerInfo
        {
            public int id { get; set; } 
            public int x { get; set; }
            public int y { get; set; }
        }
        
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
                Console.WriteLine("Solid Object Created at " + keyValuePairs.GetValueOrDefault("x") + " , " + keyValuePairs.GetValueOrDefault("y"));
                
            }
        }

        public async Task HandleData(string data, UdpClient c, IPEndPoint endpoint)
        {
            try
            {
                await Task.Run(async () =>
                {
                    var parse = data.Trim();
                    if (parse.Contains("Client:Connected"))
                    {
                        List<Object> info = new List<Object>();
                        info.Add(100); // 0 = health 
                        info.Add(playercounter + 1); // 1 = ID
                        info.Add(endpoint.Address.ToString()); // 2 = IP
                        info.Add(endpoint.Port); // 3 = Port
                                                 // inventory 
                        Dictionary<Items, int> inventory = new Dictionary<Items, int>();
                        inventory.Add(Items.ASSAULTRIFLE, 1);
                        AddPlayer(new Player(c, endpoint, info, inventory));
                        var player = GetPlayer(endpoint.Address.ToString());
                        SendStringAll("SPAWN_PLAYER:" + player.GetID());
                        SendString("HEALTH_UPDATE:" + player.GetHealth(), c, endpoint);
                        
                        Console.WriteLine("Current players: " + GetPlayerCounter());
                        player.SetConnected(true);
                    }
                    if (parse.Contains("Client:Disconnected"))
                    {
                        var ID = int.Parse(parse.Replace("Client:Disconnected:", String.Empty));
                        RemovePlayer(GetPlayerID(ID));
                        Console.WriteLine("Current players: " + GetPlayerCounter());
                    }
                    if (parse.Contains("Instance:Create:"))
                    {
                        string raw = parse.Replace("Instance:Create:", String.Empty).Trim();
                        Dictionary<string, string> map = JSONParser.DecodeString(raw);
                        CreateInstance(map, map.GetValueOrDefault("name"));

                    }
                    if (parse.Contains("Instance:Destroy:"))
                    {
                        string raw = parse.Replace("Instance:Destroy:", String.Empty).Trim();
                        Dictionary<string, string> map = JSONParser.DecodeString(raw);
                        DestroyInstance(map, map.GetValueOrDefault("name"), map.GetValueOrDefault("x"), map.GetValueOrDefault("y"));
                    }

                    if (parse.Contains("Instance:Get"))
                    {
                        int count = instances.Count;
                        Dictionary<string, string> keyValuePairs = new Dictionary<string, string>();
                        foreach (var kv in instances)
                        {
                            SendString("INSTANCE_CREATE:" + JSONParser.EncodeString(kv), c, endpoint);
                        }

                    }
                    if (parse.Contains("Player:Update:"))
                    {
                        string raw = parse.Replace("Player:Update:", String.Empty).Trim();
                        Dictionary<string, float> map = JSONParser.Decode(raw);
                        int ID = (int)map.GetValueOrDefault("ID");
                        Player player = GetPlayerID(ID);
                        float[] pos = player.GetPosition();
                        pos[0] = map.GetValueOrDefault("x");
                        pos[1] = map.GetValueOrDefault("y");
                        player.SetPosition(pos);
                        SendStringAll("PLAYER_UPDATE:" + JSONParser.Encode(map));
                    }
                    if (GetPlayerCounter() > 0)
                    {
                        var periodicTimer = new PeriodicTimer(TimeSpan.FromSeconds(10));
                        SendStringAll("PLAYER_COUNTER:" + GetPlayerCounter());
                        
                    }

                    // "anti noclip" check
                    foreach (var dict in instances)
                    {
                        int x = int.Parse(dict.GetValueOrDefault("x"));
                        int y = int.Parse(dict.GetValueOrDefault("y"));
                        Player player = GetPlayer(endpoint.Address.ToString());
                        if (player != null)
                        {
                            int pX = (int)player.GetPosition()[0];
                            int pY = (int)player.GetPosition()[1];
                            double distanceSquared = CalculateDistance(pX, pY, x, y);
                            if (distanceSquared < 15)
                            {
                                SendString("GAME_END", c, endpoint);
                            }
                        }
                        
                    }

                });
            } catch (Exception ex ) { Console.WriteLine(ex.Message); }

        
        }

        static string ParseItem(Items item)
        {
            switch (item)
            {
                case Items.ASSAULTRIFLE:
                    return "ASSAULTRIFLE";
                case Items.MEDKIT:
                    return "MEDKIT";
                default:
                    break;
            }
            return "";
        }

        static double CalculateDistance(double x1, double y1, double x2, double y2)
        {
            // Calculate the squared differences in x and y
            double deltaX = x2 - x1;
            double deltaY = y2 - y1;

            // Calculate the squared distance
            double distanceSquared = deltaX * deltaX + deltaY * deltaY;

            // Calculate the square root of the squared distance to get the actual distance
            double distance = Math.Sqrt(distanceSquared);

            return distance;
        }

        public void CreateInstance(Dictionary<string, string> map, string name)
        {
            if (!instances.Contains(map))
            {
                if (Objects.GetObjects().Contains(name))
                {
                    instances.Add(map);
                    SendStringAll("INSTANCE_CREATE:" + JSONParser.EncodeString(map));
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
                        Console.WriteLine("instance {0} removed ", name);
                        instances.Remove(keyValuePairs);
                    }
                }
                SendStringAll("INSTANCE_DESTROY:" + JSONParser.EncodeString(map));
                
            }
        }

        public void AddPlayer(Player player) 
        {
            if (!players.Contains(player))
            {
                players.Add(player);
                playercounter = players.Count;
            }
        }

        public void RemovePlayer(Player player) 
        {
            SendStringAll("DISCONNECT_PLAYER:" + player.GetID());
            players.Remove(player);
            playercounter = players.Count;
        }

        public Player? GetPlayer(string ip) // get player via IP
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

        public Player? GetPlayerID(int id) // get player via ID
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

        public List<Player> GetPlayers()
        {
            return players;
        }

        public int GetPlayerCounter()
        {
            return playercounter;
        }

        public void SendString(string data, UdpClient c, IPEndPoint client)
        {
            try
            {
                c.Send(Encoding.ASCII.GetBytes(data), Encoding.ASCII.GetBytes(data).Length, client);
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"SocketException: {ex.Message}, ErrorCode: {ex.SocketErrorCode}");
                Console.WriteLine(ex.StackTrace);
            }
        }

        public void SendStringAll(string data)
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
