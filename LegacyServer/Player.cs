using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace LegacyServer
{
    public class Player
    {
        Dictionary<Items, int> inventory;
        List<Object> info; // info of the player, like IP, health, position, etc 
        IPEndPoint endpoint;
        UdpClient client;
        bool isConnected;
        float[] position = { 0, 0 }; // first value is x, second is y
        public Player(UdpClient client, IPEndPoint endpoint,List<Object> info, Dictionary<Items, int> inventory)
        {
            this.info = info;
            this.inventory = inventory;
            this.endpoint = endpoint;
            this.client = client;
        }

        public bool GetConnected() { return isConnected; }
        public void SetConnected(bool val) { isConnected = val; }
        public IPEndPoint GetEndPoint() { return endpoint; }
        public UdpClient GetClient() { return client; }  
        public List<Object> GetInfo()
        {
            return info;
        }

        public float[] GetPosition()
        {
            return position;
        }
        
        public void SetPosition(float[] val)
        {
            position = val;
        }

        public int GetHealth()
        {
            return (int)GetInfo()[0];
        }

        public int GetID()
        {
            return (int)GetInfo()[1];
        }

        public Dictionary<Items, int> GetInventory() 
        {
            return inventory;
        }
    }
}
