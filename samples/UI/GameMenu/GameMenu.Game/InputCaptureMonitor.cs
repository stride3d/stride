// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Input;

namespace GameMenu
{
    /// <summary>
    /// Shows what game code sees of the input while the menu UI is used. Input the UI captures, such as clicks on buttons
    /// or typing in a text box, is hidden from the game. Press F9 to turn this masking off and compare.
    /// </summary>
    public class InputCaptureMonitor : SyncScript
    {
        private int leftClicks;
        private float wheelTotal;

        public override void Update()
        {
            if (Input.IsKeyPressed(Keys.F9))
                Input.MaskCapturedInput = !Input.MaskCapturedInput;

            if (Input.IsMouseButtonPressed(MouseButton.Left))
                leftClicks++;
            wheelTotal += Input.MouseWheelDelta;

            var position = new Int2(10, 10);
            DebugText.Print($"What the game sees (F9: masking {(Input.MaskCapturedInput ? "on" : "off")})", position);
            DebugText.Print($"Keys down: {string.Join(", ", Input.DownKeys)}", position + new Int2(0, 20));
            DebugText.Print($"Left clicks: {leftClicks}   Wheel: {wheelTotal:0.##}", position + new Int2(0, 40));
            DebugText.Print($"Mouse held by: {OwnerName(Input.Mouse)}   Keyboard held by: {OwnerName(Input.Keyboard)}", position + new Int2(0, 60));
        }

        private static string OwnerName(IInputDevice device) => device?.CaptureState.Owner?.GetType().Name ?? "nobody";
    }
}
