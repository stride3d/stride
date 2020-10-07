using Telepathy;
using Stride.Core.Mathematics;
using System.Text;
namespace Stride.Networking
{

    /// <summary>
    /// Use This class As a wrapper for TCP and RUDP
    /// </summary>
    public static class MainTransportTCP
    {
        public static Client ConnectAndCreateClient(string ip, int port)
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

            if (s == null)
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
}
