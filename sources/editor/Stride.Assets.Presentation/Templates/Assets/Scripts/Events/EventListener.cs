using System.Threading.Tasks;
using Stride.Core;
using Stride.Engine;
using Stride.Engine.Events;

namespace ##Namespace##
{
    /// <summary>
    /// Script which listens to an event, and then catches it and performs some game logic based on the it.
    /// </summary>
    public class ##Scriptname## : AsyncScript
    {
        // Hint. You also need the EventBroadcaster script. Change the namespace accordingly.
        private readonly EventReceiver<string> listener = new EventReceiver<string>(EventBroadcaster.EventKey);

        /// <summary>
        /// This name should match the event name which you expect to receive from a EventBroadcaster script.
        /// </summary>
        [DataMember(10)]
        [Display("Event Name")]
        public string EventName = "";

        public override async Task Execute()
        {
            while (Game.IsRunning)
            {
                var trigger = await listener.ReceiveAsync();
                if (trigger != EventName)
                    continue;

                EventReceived();
            }
        }

        private void EventReceived()
        {
            // TODO Add your game logic here

        }
    }
}
