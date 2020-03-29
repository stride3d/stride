using Xenko.Core.Mathematics;
using Xenko.Engine;
using Xenko.Input;

namespace CSharpBeginner.Code
{
    /// <summary>
    /// This script demonstrates how to check for keyboard input.
    /// </summary>
    public class KeyboardInputDemo : SyncScript
    {
        public Entity BlueTeapot;
        public Entity YellowTeapot;
        public Entity GreenTeapot;

        public override void Start() { }

        public override void Update()
        {
            // First lets check if we have a keyboard.
            if (Input.HasKeyboard)
            {
                // Key down is used for when a key is being held down.
                DebugText.Print("Hold the 1 key down to rotate the blue theapot", new Int2(340, 500));
                if (Input.IsKeyDown(Keys.D1))
                {
                    var deltaTime = (float)Game.UpdateTime.Elapsed.TotalSeconds;
                    BlueTeapot.Transform.RotationEulerXYZ += new Vector3(0, 0.3f * deltaTime, 0);
                }

                // Use 'IsKeyPressed' for a single key press event. 
                DebugText.Print("Press F to rotate the yellow theapot (and to pay respects)", new Int2(340, 520));
                if (Input.IsKeyPressed(Keys.F))
                {
                    YellowTeapot.Transform.Rotation *= Quaternion.RotationY(-0.4f);
                }

                // 'IsKeyReleased' is used for when you want to know when a key is released after being either held down or pressed. 
                DebugText.Print("Press and release the Space bar to rotate the green theapot", new Int2(340, 540));
                if (Input.IsKeyReleased(Keys.Space))
                {
                    GreenTeapot.Transform.Rotation *= Quaternion.RotationY(0.6f);
                }
            }
        }
    }
}
