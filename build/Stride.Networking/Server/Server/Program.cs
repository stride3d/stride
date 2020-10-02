using Telepathy;
using System;
using Stride.Networking;
using Stride.Core.Mathematics;
using System.Text;

namespace Stride.Networking.Server
{
    class Program
    {
        Telepathy.Server server;

        public Vector3 RecievedVec3;
        int[] connectIds;
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
            while (true)
            {
                UpdateLoop();
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
                        HandleData(msg);
                        break;
                    case EventType.Disconnected:
                        break;
                }
            }
        }
        public void HandleData(Message m)
        {
            String s = new UTF8Encoding(true).GetString(m.data);
            try
            {
                string[] StV = s.Split('.');
                char[] charsToOmmit = new char[2];
                charsToOmmit.SetValue('.', 0);
                charsToOmmit.SetValue(' ', 1);
                foreach (string st in StV)
                {
                    StV.GetValue(Array.IndexOf(StV, st)).ToString().Trim(charsToOmmit);
                }
                RecievedVec3.X = float.Parse(StV.GetValue(0).ToString());
                RecievedVec3.Y = float.Parse(StV.GetValue(1).ToString());
                RecievedVec3.Z = float.Parse(StV.GetValue(2).ToString());
                HandleVector3(m);
            }
            catch (Exception)
            {
                goto End;
            }
End:
            try
            {
                Messages.MessageCodes mc = (Messages.MessageCodes)Enum.Parse(typeof(Messages.MessageCodes), s, true);
                switch (mc)
                {
                    case Messages.MessageCodes.HitByRaycast:
                        HitByRaycast(m);
                        break;
                    case Messages.MessageCodes.CollidedWithSender:
                        CollidedWithSender(m);
                        break;
                    case Messages.MessageCodes.LevelLoad:
                        LevelLoad(m);
                        break;
                    case Messages.MessageCodes.close:
                        close();
                        break;
                }
            }
            catch (Exception)
            {
                Else(m);
            }

        }

        public void Else(Message m)
        {
            int[] cIds = new int[8];
            foreach (int c in cIds)
            {
                server.Send(c, m.data);
            }
        }

        public void close()
        {
            server.Stop();
        }

        public void LevelLoad(Message m)
        {
            int[] cIds = new int[8];
            foreach (int c in cIds)
            {
                server.Send(c, m.data);
            }
        }

        public void CollidedWithSender(Message m)
        {
            int[] cIds = new int[8];
            foreach (int c in cIds)
            {
                server.Send(c, m.data);
            }
        }

        public void HitByRaycast(Message m)
        {
            int[] cIds = new int[8];
            foreach (int c in cIds)
            {
                server.Send(c, m.data);
            }
        }
        void HandleVector3(Message m)
        {
            int[] cIds = new int[8];
            foreach(int c in cIds)
            {
                server.Send(c, m.data);
            }
        }
    }
}
