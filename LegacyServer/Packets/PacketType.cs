using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LegacyServer.Packets
{
    public enum PacketType
    {
        C00PacketPlayerHeartbeat, // client respons using this to S01PacketHeartbeat
        C01PacketPlayerConnected, // sent when client connects to the server
        C02PacketPlayerDisconnected, // sent when client disconnects from the server
        C03PacketPlayerUpdate, // sent when the client updates their position
        C04PacketInstanceGet, // sent when the client wants to get all instances stored on the server, excluding players
        S01PacketHeartbeat, // server sends this to all clients making sure they are still connected
        S02PacketInstanceCreate, // server sends this to all clients notifying them that a new instance has spawned
        S03PacketInstanceDestroy, // server sends this to all clients notifying them that an instance has been destroyed
        S04PacketSpawnPlayer, // server sends this to all clients notifying them that a new player has connected
        S05PacketHealthUpdate, // server sends this to a specific client, then updates their health
        S06PacketStatusUpdate // usually reserved for updating variables (eg player counter)

        // TODO: implement client-side instance create/destroy packets
    }
}
