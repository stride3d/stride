using System;
using System.Text;
using Telepathy;
using NUnit.Framework;
using Stride.Networking;
namespace Stride.Networking.Tests
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void Test1()
        {
            var s = "T";
            var server = MainTransportTCP.CreateServer(80);
            var client = MainTransportTCP.ConnectAndCreateClient("localhost", 80);
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
                            Assert.That(new UTF8Encoding(true).GetString(message.data), Is.EqualTo(s));
                            return;
                        case EventType.Disconnected:
                            break;
                    }
                }
            }
        }
    }
}
