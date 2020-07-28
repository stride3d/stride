using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Input;

namespace CSharpBeginner.Code
{
    /// <summary>
    /// This script demonstrates how to create virtual buttons and how to use them.
    /// </summary>
    public class VirtualButtonsDemo : SyncScript
    {
        public Entity BlueTeapot;

        public override void Start()
        {
            // Create a new VirtualButtonConfigSet if none exists. 
            Input.VirtualButtonConfigSet = Input.VirtualButtonConfigSet ?? new VirtualButtonConfigSet();

            // Bind the "W" key and "Up arrow" to a virtual button called "Forward".
            var forwardW = new VirtualButtonBinding("Forward", VirtualButton.Keyboard.W);
            var forwardUpArrow = new VirtualButtonBinding("Forward", VirtualButton.Keyboard.Up);
            var forwardLeftMouse = new VirtualButtonBinding("Forward", VirtualButton.Mouse.Left);
            var forwardLeftTrigger = new VirtualButtonBinding("Forward", VirtualButton.GamePad.LeftTrigger);

            // Create a new virtual button configuration and add the virtual button bindings
            var virtualButtonForward = new VirtualButtonConfig
            {
                forwardW,
                forwardUpArrow,
                forwardLeftMouse,
                forwardLeftTrigger
            };

            // Add the virtual button binding to the virtual button configuration
            Input.VirtualButtonConfigSet.Add(virtualButtonForward);
        }

        public override void Update()
        {
            // We retrieve a float value from the virtual button. 
            // When the value is higher than 0, we know that we have at least one of the keys or mouse pressed
            // Keyboard and mouse return a value of 1 if they are being pressed.
            // Gamepads can have a more accurate value between 0 and 1 depending on how far a trigger is being pressed
            var forward = Input.GetVirtualButton(0, "Forward");

            // Note: Gamepad sticks can be a negative value. For this example we only check if the value is higher than 0
            if (forward > 0)
            {
                var deltaTime = (float)Game.UpdateTime.Elapsed.TotalSeconds;
                BlueTeapot.Transform.Rotation *= Quaternion.RotationY(0.6f * forward * deltaTime);
            }
            
            DebugText.Print("Hold down W, the Up arrow the left mouse button or the Left trigger on a gamepad", new Int2(600, 200));
            DebugText.Print("Virtual button 'Forward': " + forward, new Int2(600, 220));
        }
    }
}
