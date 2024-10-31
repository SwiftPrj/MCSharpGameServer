using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace LegacyServer.Packets
{
    public class Packet
    {
        private PacketType type;
        private UdpClient c;
        private IPEndPoint endpoint;
        private Dictionary<PacketType, string> data;

        public Packet(PacketType type, Dictionary<PacketType, string> data, UdpClient c, IPEndPoint endpoint)
        {
            this.c = c;
            this.endpoint = endpoint;
            this.type = type;
            this.data = data;
        }

        public PacketType GetType() { return type; }
        public UdpClient GetUdpClient() { return c; }
        public IPEndPoint GetIPEndPoint() { return endpoint; }
        public Dictionary<PacketType, string> GetData() { return data; }    
        public void SetType(PacketType type) { this.type = type; }

        public void SetData(Dictionary<PacketType, string> v)
        {
            this.data = v;
        }

        public void SetPosition()
        {

        }

        public void SetValue(string value)
        {
            data.Clear();

            data.Add(GetType(), value);
        }
    }
}
