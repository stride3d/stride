using Telepathy;
using System;
namespace Stride.Networking.Server
{
    class Program
    {
        Telepathy.Server server;
        static void Main(string[] args)
        {
            new Program().Begin(args);
        }
        void Begin(string[] args)
        {
            try {
                server = new Telepathy.Server();
                server.Start(int.Parse(args.GetValue(0).ToString()));
            } 
            catch
            {
                switch (args.Length)
                {
                    case 0:
                        Console.WriteLine("There needs to be 1 argument that is a number");
                        break;
                    case 1:
                        Console.WriteLine("Please pass the first argument as a number without decimals.");
                        break;
                }
                
                server.Stop();
            }
            
        }
        void UpdateLoop()
        {
            Message msg;
            while (server.GetNextMessage(out msg))
            {
                switch (msg.eventType)
                {
                    case EventType.Connected:
                        break;
                    case EventType.Data:
                        break;
                    case EventType.Disconnected:
                        break;
                }
            }
        }
    }
}
