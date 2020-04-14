using Stride.Core;
using Stride.Input;
using Stride.Engine;
using Stride.Engine.Events;

namespace ##Namespace##
{
    /// <summary>
    /// Script which dispatches an event when a key is pressed or at regular time intervals.
    /// </summary>
    public class ##Scriptname## : SyncScript
    {
        // Hint. Change the event key type if you want to broadcast different type of data
        public static EventKey<string> EventKey = new EventKey<string>();

        /// <summary>
        /// When conditions are met this dispatcher will fire an event with the specified name.
        /// </summary>
        [DataMember(10)]
        [Display("Event Name")]
        public string EventName = "";

        /// <summary>
        /// Determines which key triggers the event. The default is a space key press.
        /// </summary>
        [DataMember(110)]
        [Display("Key")]
        public Keys Key = Keys.Space;

        /// <summary>
        /// The event will be triggered at regular time intervals. Set it to 0 to disable this feature.
        /// </summary>
        [DataMember(120)]
        [Display("Interval")]
        public float TimeInterval { get; set; } = 0f;

        private float timeIntervalCountdown = 0f;

        public override void Update()
        {
            // Condition 1 - the trigger can be a key press
            var isTriggered = Input.IsKeyPressed(Key);

            // Condition 2 - the trigger can be a time interval
            if (TimeInterval > 0)
            {
                timeIntervalCountdown -= (float)Game.UpdateTime.Elapsed.TotalSeconds;
                if (timeIntervalCountdown <= 0f)
                {
                    timeIntervalCountdown = TimeInterval;
                    isTriggered = true;
                }
            }

            if (!isTriggered)
                return;

            EventKey.Broadcast(EventName);
        }
    }
}
