using System.Collections.Generic;

namespace Stride.Input
{
    /// <summary>
    /// A <see cref="IGamePadDevice"/> from a <see cref="IGameControllerDevice"/> using a <see cref="GamePadLayout"/> to create a mapping between the two
    /// </summary>
    public abstract class GamePadFromLayout : GamePadDeviceBase
    {
        private readonly List<InputEvent> events = new List<InputEvent>();

        private GamePadState state = new GamePadState();

        protected InputManager InputManager { get; }

        protected GamePadLayout Layout { get; }

        protected IGameControllerDevice GameControllerDevice { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GamePadFromLayout"/> class.
        /// </summary>
        protected GamePadFromLayout(InputManager inputManager, IGameControllerDevice controller, GamePadLayout layout)
        {
            InputManager = inputManager;
            Layout = layout;
            GameControllerDevice = controller;
        }

        public override GamePadState State => state;

        public override void Update(List<InputEvent> inputEvents)
        {
            // Wrap the controller device and turn it's events into gamepad events
            GameControllerDevice.Update(events);

            int eventStart = inputEvents.Count;

            foreach (var e in events)
            {
                Layout.MapInputEvent(this, GameControllerDevice, e, inputEvents);
                InputManager.PoolInputEvent(e); // Put event back into event pool
            }

            ClearButtonStates();

            // Apply events to gamepad state
            for (int index = eventStart; index < inputEvents.Count;)
            {
                if (!state.Update(inputEvents[index]))
                {
                    // Discard event, since it didn't affect the state
                    InputManager.PoolInputEvent(inputEvents[index]); // Put event back into event pool
                    inputEvents.RemoveAt(index);
                }
                else
                {
                    var buttonEvent = inputEvents[index] as GamePadButtonEvent;
                    if (buttonEvent != null)
                        UpdateButtonState(buttonEvent);

                    index++;
                }
            }

            events.Clear();
        }
    }
}