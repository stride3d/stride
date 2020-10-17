using System.Text;
using Telepathy;
using NUnit.Framework;
using LiteNetLib;

namespace Stride.Networking.Tests
{
    public class Tests
    {
        public int id = 0;
        Server server;
        Client client;
        [SetUp]
        public void Setup()
        {
            server = MainTransportTCP.CreateServer(80);
            client = MainTransportTCP.ConnectAndCreateClient("localhost", 80);
        }

        [Test]
        public void Test1()
        {
            var s = "T";


            client.Send(new UTF8Encoding(true).GetBytes(s));
            Message message;
            while (true)
            {
                client.Send(new UTF8Encoding(true).GetBytes(s));
                while (server.GetNextMessage(out message))
                {
                    switch (message.eventType)
                    {
                        case EventType.Connected:
                            break;
                        case EventType.Data:
                            id = message.connectionId;
                            Assert.That(new UTF8Encoding(true).GetString(message.data), Is.EqualTo(s));
                            return;
                        case EventType.Disconnected:
                            break;
                    }
                }
            }
        }
        [Test]
        public void Test2()
        {
            MainTransportServer server = new MainTransportServer(TransportType.RUDP);
            server.CreateServer(80, NetRec, new EventBasedNetListener.OnConnectionRequest(RequestJoin));
        }
        public void NetRec(NetPeer peer, NetPacketReader netPacketReader, DeliveryMethod method)
        {

        }
        public void RequestJoin(ConnectionRequest request)
        {
            request.AcceptIfKey("KEY");
        }
        [OneTimeTearDown]
        void End()
        {
            client.Disconnect();
            server.Stop();
        }

    }
}
