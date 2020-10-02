using Telepathy;
using Stride.Engine;
using System;
using System.Net;
using System.Text;
using System.Numerics;
namespace Stride.Networking
{
    class GameClient : SyncScript
    {
        Client client;
        Vector3 RecievedVec3;
        public GameClient(IPAddress address, int port)
        {
            client = new Telepathy.Client();
#pragma warning disable CS0618 // Type or member is obsolete
            client.Connect($"{address.Address}", port);
#pragma warning restore CS0618 // Type or member is obsolete
        }
        public override void Start()
        {
            base.Start();
        }
        public override void Update()
        {
            Message msg;
            while (client.GetNextMessage(out msg))
            {
                switch (msg.eventType)
                {
                    case Telepathy.EventType.Connected:
                        Connected();
                        break;
                    case Telepathy.EventType.Data:
                        HandleData(msg.data);
                        break;
                    case Telepathy.EventType.Disconnected:
                        close();
                        break;
                }
            }
        }
        public void HandleData(byte[] data)
        {
            String s = new UTF8Encoding(true).GetString(data);
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
            }
            catch (Exception)
            {
                goto End;
            }
        End:
            try
            {
                Messages.MessageCodes m = (Messages.MessageCodes)Enum.Parse(typeof(Messages.MessageCodes), s, true);
                switch (m)
                {
                    case Messages.MessageCodes.HitByRaycast:
                        HitByRaycast();
                        break;
                    case Messages.MessageCodes.CollidedWithSender:
                        CollidedWithSender();
                        break;
                    case Messages.MessageCodes.LevelLoad:
                        LevelLoad();
                        break;
                    case Messages.MessageCodes.close:
                        close();
                        break;
                }
            }
            catch (Exception)
            {
                Else();
            }
            
        }
        /// <summary>
        /// Sends a string to the server
        /// </summary>
        /// <param name="s">the string to be sent</param>
        public void SendMessage(string s)
        {
            client.Send(new UTF8Encoding(true).GetBytes(s));
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="s">the byte[] to be sent</param>
        public void SendMessage(byte[] s)
        {
            client.Send(s);
        }
        /// <summary>
        /// Sends a string to the server
        /// </summary>
        /// <param name="s">the vector3 to be sent</param>
        public void SendMessage(Vector3 s)
        {
            client.Send(new UTF8Encoding(true).GetBytes(s.ToString()));
        }
        /// <summary>
        /// this.Entity was hit by a raycast. Override to add custom code.
        /// </summary>
        public void HitByRaycast()
        {

        }
        /// <summary>
        /// this.Entity collided with something. Override to add custom code.
        /// </summary>
        public void CollidedWithSender()
        {

        }
        /// <summary>
        /// Level Loaded. Override to add custom code.
        /// </summary>
        public void LevelLoad()
        {

        }
        /// <summary>
        /// Everything else. Override to add custom code.
        /// </summary>
        public void Else()
        {

        }
        /// <summary>
        /// closes all connections.
        /// </summary>
        public void close()
        {
            client.Disconnect();
        }
        /// <summary>
        /// put code for connecting here
        /// </summary>
        public void Connected()
        {

        }
    }
}
