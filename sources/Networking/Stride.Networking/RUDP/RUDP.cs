using System.Net;
using SharpRUDP;
namespace Stride.Networking.RUDP
{
    public class RUDPClient : SharpRUDP.RUDPConnection
    {
        public RUDPClient(string ip, int port)
        {
            this.Connect(ip, port);
            this.OnPacketReceived += new dlgEventUserData(RecieveMessage);
        }
        public void RecieveMessage(RUDPPacket p)
        {
            
        }
    }
    public class RUDPServer : SharpRUDP.RUDPConnection
    {
        public RUDPServer(string ip, int port)
        {
            this.Listen(ip, port);
            this.OnPacketReceived += new dlgEventUserData(RecieveMessage);
            this.OnClientConnect += new dlgEventConnection(clientConnect);
            this.OnClientDisconnect += new dlgEventConnection(clientDisconnect);
            
        }
        public void RecieveMessage(RUDPPacket p)
        {
            
        }
        public void clientConnect(IPEndPoint ep)
        {

        }
        public void clientDisconnect(IPEndPoint ep)
        {

        }
    }
}
