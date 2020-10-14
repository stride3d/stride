using System.Text;
using Telepathy;
using NUnit.Framework;
using System.IO;

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
            var s = "T";
            
            server.Send(id, new UTF8Encoding(true).GetBytes(s));
            Message message;

            while (true)
            {
                server.Send(id, new UTF8Encoding(true).GetBytes(s));
                while (client.GetNextMessage(out message))
                {
                    switch (message.eventType)
                    {
                        case EventType.Connected:
                            break;
                        case EventType.Data:
                            Assert.That(new UTF8Encoding(true).GetString(message.data), Is.EqualTo(s));
                            return;
                        case EventType.Disconnected:
                            Assert.Fail();
                            break;
                    }
                }
            }
        }
        [OneTimeTearDown]
        void End()
        {
            client.Disconnect();
            server.Stop();
        }

    }
}
