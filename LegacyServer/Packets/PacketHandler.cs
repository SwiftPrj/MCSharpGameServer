using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;


namespace LegacyServer.Packets
{
    public class PacketHandler
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        public void HandlePacket(Packet packet, Data data)
        {
            if (packet == null) return;

            string parse = packet.GetData().First().Value;
            //Logger.Info("data " + packet.GetType().ToString() + ":" + packet.GetData().First().Value);
            switch (packet.GetType())
            {
                case PacketType.C01PacketPlayerConnected:
                    foreach (Player p in Data.GetPlayers())
                    {
                        if (p.GetEndPoint().Address.ToString() == packet.GetIPEndPoint().Address.ToString())
                        {
                            Logger.Warn("same client tried to connect to server");
                            //return;
                        }
                    }
                    if (packet.GetData().First().Value != "Ping!")
                    {
                        Logger.Warn("client attempted to connect without handshake?");
                        return;
                    } else
                    {
                        Dictionary<PacketType, string> handshakekeys = new Dictionary<PacketType, string>();
                        handshakekeys.Add(PacketType.S06PacketStatusUpdate, "Pong!");
                        Packet handshake = new Packet(PacketType.S06PacketStatusUpdate, handshakekeys, packet.GetUdpClient(), packet.GetIPEndPoint());
                        SendPacketIndividual(handshake);
                        DisposePacket(handshake);
                    }
                    List<Object> info = new List<Object>();
                    Logger.Info("client connected");
                    info.Add(100); // 0 = health 
                    info.Add(data.GetPlayerCounter() + 1); // 1 = ID 
                    info.Add(packet.GetIPEndPoint().Address.ToString()); // 2 = IP
                    info.Add(packet.GetIPEndPoint().Port); // 3 = Port
                                             // inventory 
                    Dictionary<Items, int> inventory = new Dictionary<Items, int>();
                    inventory.Add(Items.SWORD, 1);
                    inventory.Add(Items.RED_POTION, 3);
                    Data.AddPlayer(new Player(packet.GetUdpClient(), packet.GetIPEndPoint(), info, inventory));
                    
                    var player = Data.GetPlayer(packet.GetIPEndPoint().Address.ToString());
                    packet.SetType(PacketType.S04PacketSpawnPlayer);
                    packet.SetValue(player.GetID().ToString());
                    SendPacket(packet);
                    packet.SetType(PacketType.S05PacketHealthUpdate);
                    packet.SetValue(player.GetHealth().ToString());
                    SendPacketIndividual(packet);

                    Logger.Info("Current players: " + data.GetPlayerCounter());
                    player.SetConnected(true);
                    break;
                case PacketType.C02PacketPlayerDisconnected:
                    var ID = int.Parse(parse);
                    Data.RemovePlayer(Data.GetPlayerID(ID));
                    Logger.Info("Current players: " + data.GetPlayerCounter());
                    break;
                case PacketType.S02PacketInstanceCreate:
                    ValidatePacket(packet, data, parse);
                    Dictionary<string, string> map = JSONParser.DecodeString(parse);
                    data.CreateInstance(map, map.GetValueOrDefault("name"));
                    break;
                case PacketType.S03PacketInstanceDestroy:
                    ValidatePacket(packet, data, parse);
                    Dictionary<string, string> destroyMap = JSONParser.DecodeString(parse);
                    data.DestroyInstance(destroyMap, destroyMap.GetValueOrDefault("name"), destroyMap.GetValueOrDefault("x"), destroyMap.GetValueOrDefault("y"));
                    break;
                case PacketType.C04PacketInstanceGet:
                    foreach (var kv in data.GetInstances())
                    {
                        Dictionary<PacketType, string> keyValuePairs = new Dictionary<PacketType, string>();
                        keyValuePairs.Add(PacketType.S02PacketInstanceCreate, JSONParser.EncodeString(kv));
                        Packet instance = new Packet(PacketType.S02PacketInstanceCreate, keyValuePairs, packet.GetUdpClient(), packet.GetIPEndPoint());
                        SendPacketIndividual(instance);
                        DisposePacket(instance);
                        DisposePacket(packet);
                    }
                    break;
                case PacketType.C03PacketPlayerUpdate:
                    ValidatePacket(packet, data, parse);
                    Dictionary<string, float> playerMap = JSONParser.Decode(parse);
                    int playerID = (int)(playerMap.GetValueOrDefault("ID"));
                    Player playerObj = Data.GetPlayerID(playerID);
                    float[] pos = playerObj.GetPosition();
                    pos[0] = (playerMap.GetValueOrDefault("x"));
                    pos[1] = (playerMap.GetValueOrDefault("y"));
                    pos[2] = (playerMap.GetValueOrDefault("z"));
                    playerObj.SetPosition(pos);
                    //packet.SetValue(playerMap);
                    /*                    Logger.Info(packet.GetData().First().Value);
                                        SendPacket(packet);*/

                    // TODO: implement a better way of handling C03
                    Data.SendStringAll(packet.GetType().ToString() + ":" + parse);
                    break;
                default:
                    break;
                }

            }
        

        public string FormatPacket(Packet packet)
        {
            if (packet == null) return "";

            return packet.GetData().First().Key.ToString() + ":" + packet.GetData().First().Value;
        }

        public void SendPacket(Packet packet)
        {
            if (packet == null) return;

            Data.SendStringAll(FormatPacket(packet));

            //DisposePacket(packet);
        }

        public void SendPacketIndividual(Packet packet) 
        {
            Data.SendString(FormatPacket(packet), packet.GetUdpClient(), packet.GetIPEndPoint());

            //DisposePacket(packet);
        }

        public void DisposePacket(Packet packet) { packet = null; }
        public void ValidatePacket(Packet packet, Data d, string data)
        {
            switch (packet.GetType())
            {

                case PacketType.C03PacketPlayerUpdate:
                    Dictionary<string, float> playerMap = JSONParser.Decode(data);
                    if (playerMap.Count < 4)
                    {
                        Logger.Warn("Unable to validate packet {0} ", packet.GetType().ToString(), " less than 4 components?");
                        Data.RemovePlayer(Data.GetPlayer(packet.GetIPEndPoint().Address.ToString())); // kick player for invalid packet 
                    }
                    break;
                case PacketType.S02PacketInstanceCreate:
                    Dictionary<string, string> instanceCreateMap = JSONParser.DecodeString(data);
                    string instanceName = instanceCreateMap.GetValueOrDefault("name");
                    if (!Objects.GetObjects().Contains(instanceName))
                    {
                        Logger.Warn("Illegal object name found in packet {0} ", packet.GetType().ToString());
                        Logger.Warn("Packet: " + packet.GetData().First().Value);
                        Data.RemovePlayer(Data.GetPlayer(packet.GetIPEndPoint().Address.ToString())); // kick player for invalid packet 
                    }
                    if (instanceName != "obj_projectile")
                    {
                        if (instanceCreateMap.Count < 4)
                        {
                            Logger.Warn("Unable to validate packet {0} ", packet.GetType().ToString(), " less than 4 components?");
                            Data.RemovePlayer(Data.GetPlayer(packet.GetIPEndPoint().Address.ToString())); // kick player for invalid packet 
                        }
                    }
                    else
                    {
                        if (instanceCreateMap.Count < 7)
                        {
                            Logger.Warn("Unable to validate packet {0} ", packet.GetType().ToString(), " less than 7 components? (type: bullet)");
                            Data.RemovePlayer(Data.GetPlayer(packet.GetIPEndPoint().Address.ToString())); // kick player for invalid packet 
                        }
                    }
                    break;
                case PacketType.S03PacketInstanceDestroy:
                    Dictionary<string, string> instanceDestroyMap = JSONParser.DecodeString(data);
                    string destroyName = instanceDestroyMap.GetValueOrDefault("name");
                    if (!Objects.GetObjects().Contains(destroyName))
                    {
                        Logger.Warn("Illegal object name found in packet {0} ", packet.GetType().ToString());
                        Logger.Warn("Packet: " + packet.GetData());
                        Data.RemovePlayer(Data.GetPlayer(packet.GetIPEndPoint().Address.ToString())); // kick player for invalid packet 
                    }
                    if (instanceDestroyMap.Count < 3)
                    {
                        Logger.Warn("Unable to validate packet {0} ", packet.GetType().ToString(), " less than 3 components?");
                        Data.RemovePlayer(Data.GetPlayer(packet.GetIPEndPoint().Address.ToString())); // kick player for invalid packet 
                    }
                    break;
            }
        }
    }
}
