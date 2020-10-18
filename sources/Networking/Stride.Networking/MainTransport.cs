
using System.Text;
using System.Threading;
using LiteNetLib;
using LiteNetLib.Utils;
using Stride.Core.Mathematics;
using Telepathy;
namespace Stride.Networking
{
    public enum TransportType
    {
        TCP,
        RUDP,
    };
    /// <summary>
    /// Wrapper class for MainTransportTCP and MainTransportRUDP as a server
    /// </summary>
    public class MainTransportServer
    {
        /// <summary>
        /// TCP or RUDP
        /// </summary>
        public TransportType type { get; private set; }
        /// <summary>
        /// Server for TCP
        /// </summary>
        public Server serverTCP { get; private set; }
        /// <summary>
        /// Server for RUDP
        /// </summary>
        public NetManager serverRUDP { get; private set; }
        /// <summary>
        /// Creates a new MainTransportClient Object.
        /// </summary>
        /// <param name="type">TCP or RUDP</param>
        public MainTransportServer(TransportType type)
        {
            this.type = type;
        }
        /// <summary>
        /// Create a server
        /// </summary>
        /// <param name="port">the port to start on</param>
        /// <param name="action">Recieve event</param>
        /// <param name="act2">connection request event. Checking for the key is done here.</param>
        public void CreateServer( int port, EventBasedNetListener.OnNetworkReceive action = null, EventBasedNetListener.OnConnectionRequest act2 = null)
        { 
            switch (type)
            {
                case TransportType.TCP:
                    serverTCP = new Server();
                    serverTCP.Start(port);
                    break;
                case TransportType.RUDP:
                    serverRUDP = MaintransportRUDP.CreateServer(port, action, act2);
                    break;
            }
        }
        /// <summary>
        /// Destroy the server
        /// </summary>
        public void DestroyServer()
        {
            serverRUDP.Stop();
            serverTCP.Stop();
        }
        /// <summary>
        /// Send a message.
        /// </summary>
        /// <param name="message">the string to be sent</param>
        /// <param name="vector">the vector 3 to be sent in this format X:Y:Z</param>
        /// <param name="cID">connection ID for TCP</param>
        public void SendMessage(int cID,string message = null, Vector3 vector = new Vector3())
        {
            if (message == null)
            {
                switch (type)
                {
                    case TransportType.TCP:
                        MainTransportTCP.SendMessageServer(cID, serverTCP, vector.ToStringNetworking());
                        break;
                    case TransportType.RUDP:
                        MaintransportRUDP.SendMessageServer(serverRUDP, vector.ToStringNetworking());
                        break;
                }
            }
            else
            {
                switch (type)
                {
                    case TransportType.TCP:
                        MainTransportTCP.SendMessageServer(cID,serverTCP, message);
                        break;
                    case TransportType.RUDP:
                        MaintransportRUDP.SendMessageClient(serverRUDP, message);
                        break;
                }
            }
        }
    }
    /// <summary>
    /// Wrapper for MainTransportTCP and MainTransportRUDP as a client
    /// </summary>
    public class MainTransportClient
    {
        /// <summary>
        /// TCP or RUDP
        /// </summary>
        public TransportType type { get; private set; }
        /// <summary>
        /// client for TCP
        /// </summary>
        public Client clientTCP { get; private set; }
        /// <summary>
        /// client for RUDP
        /// </summary>
        public NetManager clientRUDP { get; private set; }
        /// <summary>
        /// Creates a new MainTransportClient Object.
        /// </summary>
        /// <param name="type">TCP or RUDP</param>
        public MainTransportClient(TransportType type)
        {
            this.type = type;
        }
        /// <summary>
        /// Create and connect a client
        /// </summary>
        /// <param name="ip">the ip address to connect to</param>
        /// <param name="port">the port to listen on</param>
        /// <param name="Key">ONLY RUDP, lets the networking know what the client is supposed to connect to.</param>
        /// <param name="action">ONLY RUDP, NetworkRecieve action, for when the client recieves a packet.</param>
        public void CreateAndConnectClient(string ip, int port, string Key = null, EventBasedNetListener.OnNetworkReceive action = null)
        {
            switch (type)
            {
                case TransportType.TCP:
                    clientTCP = new Client();
                    clientTCP.Connect(ip, port);
                    break;
                case TransportType.RUDP:
                    clientRUDP = MaintransportRUDP.CreateAndConnectClient(Key, port, ip, action);
                    break;
            }
        }
        /// <summary>
        /// Destroy the client
        /// </summary>
        public void DestroyClient()
        {
            clientRUDP.Stop();
            clientTCP.Disconnect();
        }
        /// <summary>
        /// Send a message.
        /// </summary>
        /// <param name="message">the string to be sent</param>
        /// <param name="vector">the vector 3 to be sent in this format X:Y:Z</param>
        public void SendMessage(string message = null, Vector3 vector = new Vector3())
        {
            if (message == null)
            {
                switch (type)
                {
                    case TransportType.TCP:
                        MainTransportTCP.SendMessageClient(clientTCP, null, vector);
                        break;
                    case TransportType.RUDP:
                        MaintransportRUDP.SendMessageClient(clientRUDP, vector.ToStringNetworking());
                        break;
                }
            }
            else
            {
                switch (type)
                {
                    case TransportType.TCP:
                        MainTransportTCP.SendMessageClient(clientTCP, message);
                        break;
                    case TransportType.RUDP:
                        MaintransportRUDP.SendMessageClient(clientRUDP, message);
                        break;
                }
            }
        }
    }
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
        public static string ToStringNetworking(this Vector3 v)
        {
            return $"{v.X}:{v.Y}:{v.Z}";
        }
        /// <summary>
        /// send a message to the server
        /// </summary>
        /// <param name="client">the client that should send the message</param>
        /// <param name="s">the string to be sent</param>
        /// <param name="v">the vector 3 to be sent in this format X:Y:Z</param>
        public static void SendMessageClient(Client client, string s = null, Vector3 v = new Vector3())
        {

            if (s == null)
            {
                client.Send(new UTF8Encoding(true).GetBytes(v.ToStringNetworking()));
            }
            else
            {
                client.Send(new UTF8Encoding(true).GetBytes(s));
            }
        }
        /// <summary>
        /// send a message to the client
        /// </summary>
        /// <param name="connectionID">the ID of the client</param>
        /// <param name="server">the serverr to send from</param>
        /// <param name="s">the string to send</param>
        /// <param name="v">the vector 3 to be sent in this format X:Y:Z</param>
        public static void SendMessageServer(int connectionID,Server server, string s = null, Vector3 v = new Vector3())
        {

            if (s == null)
            {
                server.Send(connectionID,new UTF8Encoding(true).GetBytes(v.ToStringNetworking()));
            }
            else
            {
                server.Send(connectionID,new UTF8Encoding(true).GetBytes(s));
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
    /// <summary>
    /// Wrapper for LiteNetLib
    /// </summary>
    public class MaintransportRUDP
    {
        /// <summary>
        /// Create and connect a RUDP client
        /// </summary>
        /// <param name="connectionKey">the string to validate that this client is part of the right program.</param>
        /// <param name="port">the port to listen on</param>
        /// <param name="ip">the ip address to connect to</param>
        /// <param name="action">the NetworkRecieve event</param>
        /// <returns>RUDP Client</returns>
        public static NetManager CreateAndConnectClient(string connectionKey, int port, string ip, EventBasedNetListener.OnNetworkReceive action)
        {
            EventBasedNetListener listener = new EventBasedNetListener();
            NetManager client = new NetManager(listener);
            client.Start();
            client.Connect(ip, port, connectionKey);
            listener.NetworkReceiveEvent += action;
            PollEvents(client);
            return client;
        }

        /// <summary>
        /// Create a server
        /// </summary>
        /// <param name="port">port to listen on</param>
        /// <param name="action">NetworkRecive Event</param>
        /// <param name="act2">Request for when a client is trying to join</param>
        /// <param name="act3">Client join request</param>
        /// <param name="act4">Client Disconnect Event</param>
        /// <returns>RUDP Server</returns>
        public static NetManager CreateServer(int port, EventBasedNetListener.OnNetworkReceive action, EventBasedNetListener.OnConnectionRequest act2)
        {
            EventBasedNetListener listener = new EventBasedNetListener();
            NetManager server = new NetManager(listener);
            server.Start(port);
            listener.NetworkReceiveEvent += action;
            listener.ConnectionRequestEvent += act2;
            PollEvents(server);
            return server;
        }
        static void PollEvents(NetManager server)
        {
            while (server.IsRunning)
            {
                server.PollEvents();
                Thread.Sleep(10);
            }
        }
        /// <summary>
        /// Server Send Message
        /// </summary>
        /// <param name="server">the server to send with</param>
        /// <param name="message">the message to send. NOTE: to sens a vector3, use the ToStringNetworking to parse correctly.</param>
        public static void SendMessageServer(NetManager server,string message)
        {
            NetDataWriter writer = new NetDataWriter();
            writer.Put(message);
            server.SendBroadcast(writer, server.LocalPort);
        }
        /// <summary>
        /// Client send message
        /// </summary>
        /// <param name="client">the client to send with</param>
        /// <param name="message">the message to send. NOTE: to send a vector3, use the ToStringNetworking to parse correctly.</param>
        public static void SendMessageClient(NetManager client,string message)
        {
            NetDataWriter writer = new NetDataWriter();
            writer.Put(message);
            client.SendBroadcast(writer, client.LocalPort);
        }
    }
    
}
