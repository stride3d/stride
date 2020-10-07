using Stride.Networking.RUDP;
using Telepathy;
using Stride.Core.Mathematics;
using System.Text;
using SharpRUDP;
namespace Stride.Networking
{
    public enum TransportType
    {
        RUDP,
        TCP,
    };
    /// <summary>
    /// Use This class As a wrapper for TCP and RUDP
    /// </summary>
    public static class MainTransportTCP
    {
        public static Client ConnectAndCreateClientTCP(string ip, int port)
        {

                Client client = new Client();
                client.Connect(ip, port);
                return client;
            

        }
        public static void DestroyClient(Client client)
        {
            client.Disconnect();
        }
        public static Server CreateServer(int port)
        {
            Server server = new Server();
            server.Start(port);
            return server;
        }
        public static void SendMessage(Client client, string s = null, Vector3 v = new Vector3())
        {
            
            if(s == null)
            {
                client.Send(new UTF8Encoding(true).GetBytes(v.ToString()));
            }
            else
            {
                client.Send(new UTF8Encoding(true).GetBytes(s));
            }
        }
        public static byte[] RecieveMessageClient(Client client)
        {
            Message msg;
            while(client.GetNextMessage(out msg))
            {
                switch (msg.eventType)
                {
                    case Telepathy.EventType.Connected:
                        return null;
                        
                    case Telepathy.EventType.Data:
                        return msg.data;
                        
                    case Telepathy.EventType.Disconnected:
                        return null;
                        
                }
            }
            return null;
        }
        public static byte[] RecieveMessageServer(Server client)
        {
            Message msg;
            while (client.GetNextMessage(out msg))
            {
                switch (msg.eventType)
                {
                    case Telepathy.EventType.Connected:
                        return null;

                    case Telepathy.EventType.Data:
                        return msg.data;

                    case Telepathy.EventType.Disconnected:
                        return null;

                }
            }
            return null;
        }
    }
    public static class MainTransportRUDP
    {
        public static RUDPClient CreateAndConnectClientRUDP(string ip, int port)
        {
            var connection = new RUDPClient(ip, port);
            return connection;
        }
        public static void DestroyClient(RUDPClient client)
        {
            client.Disconnect();
        }
        public static RUDPServer CreateServer(string ip, int port)
        {
            var connection = new RUDPServer(ip, port);
            return connection;
        }

    }
}
