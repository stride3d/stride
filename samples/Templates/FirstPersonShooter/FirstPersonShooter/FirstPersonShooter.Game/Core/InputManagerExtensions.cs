// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Mathematics;
using Stride.Input;

namespace FirstPersonShooter.Core
{
    public static class InputManagerExtensions
    {
        public static bool IsGamePadButtonDown(this InputManager input, GamePadButton button, int index)
        {
            var gamepad = input.GetGamePadByIndex(index);
            if (gamepad == null)
                return false;

            return (gamepad.State.Buttons & button) == button;
        }

        public static bool IsGamePadButtonDownAny(this InputManager input, GamePadButton button)
        {
            foreach(var gamepad in input.GamePads)
            { 
                if ((gamepad.State.Buttons & button) == button)
                    return true;
            }
            return false;
        }

        public static Vector2 GetLeftThumb(this InputManager input, int index)
        {
            var gamepad = input.GetGamePadByIndex(index);
            if (gamepad == null)
                return Vector2.Zero;

            return gamepad.State.LeftThumb;
        }

        public static Vector2 GetLeftThumbAny(this InputManager input, float deadZone)
        {
            int totalCount = 0;
            Vector2 totalMovement = Vector2.Zero;
            foreach (var gamepad in input.GamePads)
            {
                var leftVector = gamepad.State.LeftThumb;
                if (leftVector.Length() >= deadZone)
                {
                    totalCount++;
                    totalMovement += leftVector;
                }
            }

            return (totalCount > 1) ? (totalMovement / totalCount) : totalMovement;
        }

        public static Vector2 GetRightThumb(this InputManager input, int index)
        {
            var gamepad = input.GetGamePadByIndex(index);
            if (gamepad == null)
                return Vector2.Zero;

            return gamepad.State.RightThumb;
        }

        public static Vector2 GetRightThumbAny(this InputManager input, float deadZone)
        {
            int totalCount = 0;
            Vector2 totalMovement = Vector2.Zero;
            foreach (var gamepad in input.GamePads)
            {
                var rightVector = gamepad.State.RightThumb;
                if (rightVector.Length() >= deadZone)
                {
                    totalCount++;
                    totalMovement += rightVector;
                }
            }

            return (totalCount > 1) ? (totalMovement / totalCount) : totalMovement;
        }

        public static float GetLeftTrigger(this InputManager input, int index)
        {
            var gamepad = input.GetGamePadByIndex(index);
            if (gamepad == null)
                return 0.0f;

            return gamepad.State.LeftTrigger;
        }

        public static float GetLeftTriggerAny(this InputManager input, float deadZone)
        {
            int totalCount = 0;
            float totalInput = 0;
            foreach (var gamepad in input.GamePads)
            {
                float triggerValue = gamepad.State.LeftTrigger;
                if (triggerValue >= deadZone)
                {
                    totalCount++;
                    totalInput += triggerValue;
                }
            }

            return (totalCount > 1) ? (totalInput / totalCount) : totalInput;
        }

        public static float GetRightTrigger(this InputManager input, int index)
        {
            var gamepad = input.GetGamePadByIndex(index);
            if (gamepad == null)
                return 0.0f;

            return gamepad.State.RightTrigger;
        }

        public static float GetRightTriggerAny(this InputManager input, float deadZone)
        {
            int totalCount = 0;
            float totalInput = 0;
            foreach (var gamepad in input.GamePads)
            {
                float triggerValue = gamepad.State.RightTrigger;
                if (triggerValue >= deadZone)
                {
                    totalCount++;
                    totalInput += triggerValue;
                }
            }

            return (totalCount > 1) ? (totalInput / totalCount) : totalInput;
        }
    }
}
