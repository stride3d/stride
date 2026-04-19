// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Stride.Input
{
    /// <summary>
    /// Partial support of Game Controller DB for SDL in 2.0.9 format.
    /// DPad remapping is ignored, partial axis (+aN, -aN) are ignored.
    /// List of devices and mappings: https://github.com/gabomdq/SDL_GameControllerDB
    /// </summary>
    public class GamePadLayoutGenericSDL : GamePadLayout
    {
        public string DisplayName { get; }

        public Guid ProductId { get; set; }

        public GamePadLayoutGenericSDL(Guid productId, string displayName, Dictionary<string, string> properties)
        {
            ProductId = productId;
            DisplayName = displayName;
            foreach (var (key, value) in properties)
            {
                var gamePadInputType = GetGamePadInputType(key);
                var deviceInputType = GetDeviceInputType(value);

                if (gamePadInputType == InputType.Button && deviceInputType == InputType.Button)
                {
                    AddButtonToButton(GetIndex(value), GetGamePadButton(key));
                }
                else if (gamePadInputType == InputType.Button && deviceInputType == InputType.Axis)
                {
                    AddAxisToButton(GetIndex(value), GetGamePadButton(key));
                }
                else if (gamePadInputType == InputType.Axis && deviceInputType == InputType.Axis)
                {
                    AddAxisToAxis(GetIndex(value), GetGamePadAxis(key), GetInverted(key, value), GetRemap(key));
                }
                else if (gamePadInputType == InputType.Axis && deviceInputType == InputType.Button)
                {
                    AddButtonToAxis(GetIndex(value), GetGamePadAxis(key), GetInverted(key, value));
                }
            }
        }

        public override bool MatchDevice(IInputSource source, IGameControllerDevice device)
        {
            return CompareProductId(device.ProductId, ProductId);
        }

        private static int GetIndex(string input)
        {
            int result = 0;
            foreach (char c in input)
            {
                if (char.IsDigit(c))
                {
                    result *= 10;
                    result += c - '0';
                }
            }

            return result;
        }

        private static bool GetInverted(string key, string value)
        {
            // invert the Y axis input (SDL uses up left, we use up right?)
            var byAxis = key == "lefty" || key == "righty";
            return value.EndsWith('~') ? !byAxis : byAxis; // trailing ~ means invert axis
        }

        private static bool GetRemap(string input)
        {
            // Clamp trigger axis to (0, 1) instead of (-1, 1)
            return input == "lefttrigger" || input == "righttrigger";
        }

        private static GamePadButton GetGamePadButton(string input)
        {
            switch (input)
            {
                case "a": return GamePadButton.A;
                case "b": return GamePadButton.B;
                case "x": return GamePadButton.X;
                case "y": return GamePadButton.Y;
                case "back": return GamePadButton.Back;
                case "start": return GamePadButton.Start;
                case "leftstick": return GamePadButton.LeftThumb;
                case "rightstick": return GamePadButton.RightThumb;
                case "leftshoulder": return GamePadButton.LeftShoulder;
                case "rightshoulder": return GamePadButton.RightShoulder;
                default: throw new InvalidOperationException("Unsupported button value (likely a bug).");
            }
        }

        private static GamePadAxis GetGamePadAxis(string input)
        {
            switch (input)
            {
                case "leftx": return GamePadAxis.LeftThumbX;
                case "lefty": return GamePadAxis.LeftThumbY;
                case "rightx": return GamePadAxis.RightThumbX;
                case "righty": return GamePadAxis.RightThumbY;
                case "lefttrigger": return GamePadAxis.LeftTrigger;
                case "righttrigger": return GamePadAxis.RightTrigger;
                default: throw new InvalidOperationException("Unsupported axis value (likely a bug).");
            }
        }

        private static InputType GetGamePadInputType(string input)
        {
            switch (input)
            {
                case "a":
                case "b":
                case "x":
                case "y":
                case "back":
                case "start":
                case "leftstick":
                case "rightstick":
                case "leftshoulder":
                case "rightshoulder":
                    return InputType.Button;
                case "leftx":
                case "lefty":
                case "rightx":
                case "righty":
                case "lefttrigger":
                case "righttrigger":
                    return InputType.Axis;
                default: return InputType.None;
                // ignoring platform and dup,ddown,dleft,dright
            }
        }

        private static InputType GetDeviceInputType(string input)
        {
            if (input[0] == 'b') return InputType.Button;
            if (input[0] == 'a') return InputType.Axis;
            // We ignore half axis here (-a0/+a0) and dpad (h0.1/h0.2/h0.4/h0.8)
            return InputType.None;
        }

        private enum InputType { None, Button, Axis }
    }
}
