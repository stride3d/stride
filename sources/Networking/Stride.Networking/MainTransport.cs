using Telepathy;
using Stride.Core.Mathematics;
using System.Text;
namespace Stride.Networking
{

    /// <summary>
    /// Use This class As a wrapper for TCP
    /// </summary>
    public static class MainTransportTCP
    {
        /// <summary>
        /// Create and connect a TCP Client
        /// </summary>
        /// <param name="ip">the ip address to connect the client to</param>
        /// <param name="port">the port to connect the client to</param>
        /// <returns></returns>
        public static Client ConnectAndCreateClient(string ip, int port)
        {

            Client client = new Client();
            client.Connect(ip, port);
            return client;


        }
        /// <summary>
        /// Disconnect the client
        /// </summary>
        /// <param name="client">the client to disconnect</param>
        public static void DestroyClient(Client client)
        {
            client.Disconnect();
        }
        /// <summary>
        /// Create a server on this machine
        /// </summary>
        /// <param name="port">the port the server should listen through</param>
        /// <returns></returns>
        public static Server CreateServer(int port)
        {
            Server server = new Server();
            server.Start(port);
            return server;
        }
        /// <summary>
        /// send a message to the server
        /// </summary>
        /// <param name="client">the client that should send the message</param>
        /// <param name="s">the string to be sent</param>
        /// <param name="v">the vector 3 to be sent in this format X.Y.Z</param>
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
        /// <summary>
        /// Call this in your update loop
        /// </summary>
        /// <param name="client">The client to recieve the message</param>
        /// <returns>a Message containing the data</returns>
        public static Message RecieveMessageClient(Client client)
        {
            Message msg;
            while (client.GetNextMessage(out msg))
            {
                switch (msg.eventType)
                {
                    case Telepathy.EventType.Connected:
                        return msg;

                    case Telepathy.EventType.Data:
                        return msg;

                    case Telepathy.EventType.Disconnected:
                        return msg;

                }
            }
            return msg;
        }
        /// <summary>
        /// Server recieve message
        /// </summary>
        /// <param name="server">the server to recieve the message</param>
        /// <returns>a Message containing the data</returns>
        public static Message RecieveMessageServer(Server server)
        {
            Message msg;
            while (server.GetNextMessage(out msg))
            {
                switch (msg.eventType)
                {
                    case Telepathy.EventType.Connected:
                        return msg;

                    case Telepathy.EventType.Data:
                        return msg;

                    case Telepathy.EventType.Disconnected:
                        return msg;

                }
            }
            return msg;
        }
    }
}
