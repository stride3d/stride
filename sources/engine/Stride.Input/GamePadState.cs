// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Runtime.InteropServices;

using Stride.Core.Mathematics;

namespace Stride.Input
{
    /// <summary>
    /// Describes the state of a typical gamepad.
    /// </summary>
    /// <seealso cref="IGamePadDevice.State"/>
    [StructLayout(LayoutKind.Sequential)]
    public struct GamePadState : IEquatable<GamePadState>
    {
        /// <summary>
        /// Bitmask of the gamepad buttons.
        /// </summary>
        public GamePadButton Buttons;

        /// <summary>
        /// Left thumbstick x-axis/y-axis value. The value is in the range [-1.0f, 1.0f] for both axis.
        /// </summary>
        public Vector2 LeftThumb;

        /// <summary>
        /// Right thumbstick x-axis/y-axis value. The value is in the range [-1.0f, 1.0f] for both axis.
        /// </summary>
        public Vector2 RightThumb;

        /// <summary>
        /// The left trigger analog control in the range [0, 1.0f]. See remarks.
        /// </summary>
        /// <remarks>
        /// Some controllers are not supporting the range of value and may act as a simple button returning only 0 or 1.
        /// </remarks>
        public float LeftTrigger;

        /// <summary>
        /// The right trigger analog control in the range [0, 1.0f]. See remarks.
        /// </summary>
        /// <remarks>
        /// Some controllers are not supporting the range of value and may act as a simple button returning only 0 or 1.
        /// </remarks>
        public float RightTrigger;

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>true if the current object is equal to the <paramref name="other" /> parameter; otherwise, false.</returns>
        public bool Equals(GamePadState other)
        {
            return Buttons.Equals(other.Buttons) && LeftThumb.Equals(other.LeftThumb) && RightThumb.Equals(other.RightThumb) && LeftTrigger.Equals(other.LeftTrigger) && RightTrigger.Equals(other.RightTrigger);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is GamePadState && Equals((GamePadState)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = Buttons.GetHashCode();
                hashCode = (hashCode * 397) ^ LeftThumb.GetHashCode();
                hashCode = (hashCode * 397) ^ RightThumb.GetHashCode();
                hashCode = (hashCode * 397) ^ LeftTrigger.GetHashCode();
                hashCode = (hashCode * 397) ^ RightTrigger.GetHashCode();
                return hashCode;
            }
        }

        /// <summary>
        /// Implements the == operator.
        /// </summary>
        /// <param name="left">The left gamepad value.</param>
        /// <param name="right">The right gamepad value.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator ==(GamePadState left, GamePadState right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Implements the != operator.
        /// </summary>
        /// <param name="left">The left gamepad value.</param>
        /// <param name="right">The right gamepad value.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator !=(GamePadState left, GamePadState right)
        {
            return !left.Equals(right);
        }

        public override string ToString()
        {
            return $"Buttons: {Buttons}, LeftThumb: {LeftThumb}, RightThumb: {RightThumb}, LeftTrigger: {LeftTrigger}, RightTrigger: {RightTrigger}";
        }

        /// <summary>
        /// Updates the state from any gamepad events received that have mapped buttons
        /// </summary>
        /// <param name="evt">The gamepad event to process</param>
        /// <returns><c>true</c> if the event made any changes</returns>
        public bool Update(InputEvent evt)
        {
            var buttonEvent = evt as GamePadButtonEvent;
            if (buttonEvent != null)
            {
                return Update(buttonEvent);
            }

            var axisEvent = evt as GamePadAxisEvent;
            if (axisEvent != null)
            {
                return Update(axisEvent);
            }

            return false;
        }

        /// <summary>
        /// Updates the state from any gamepad events received that have mapped buttons
        /// </summary>
        /// <param name="buttonEvent">The gamepad event to process</param>
        /// <returns><c>true</c> if the event made any changes</returns>
        public bool Update(GamePadButtonEvent buttonEvent)
        {
            if (buttonEvent.IsDown)
            {
                if ((Buttons & buttonEvent.Button) != 0)
                    return false;

                Buttons |= buttonEvent.Button; // Set bits
            }
            else
            {
                if ((Buttons & buttonEvent.Button) == 0)
                    return false;

                Buttons &= ~buttonEvent.Button; // Clear bits
            }

            return true;
        }

        /// <summary>
        /// Updates the state from any gamepad events received that have mapped buttons
        /// </summary>
        /// <param name="axisEvent">The gamepad event to process</param>
        /// <returns><c>true</c> if the event made any changes</returns>
        public bool Update(GamePadAxisEvent axisEvent)
        {
            switch (axisEvent.Axis)
            {
                case GamePadAxis.LeftThumbX:
                    return UpdateFloat(ref LeftThumb.X, axisEvent);
                case GamePadAxis.LeftThumbY:
                    return UpdateFloat(ref LeftThumb.Y, axisEvent);
                case GamePadAxis.RightThumbX:
                    return UpdateFloat(ref RightThumb.X, axisEvent);
                case GamePadAxis.RightThumbY:
                    return UpdateFloat(ref RightThumb.Y, axisEvent);
                case GamePadAxis.LeftTrigger:
                    return UpdateFloat(ref LeftTrigger, axisEvent);
                case GamePadAxis.RightTrigger:
                    return UpdateFloat(ref RightTrigger, axisEvent);
            }
            return false;
        }

        private bool UpdateFloat(ref float a, GamePadAxisEvent evt)
        {
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (a == evt.Value)
                return false;

            a = evt.Value;
            return true;
        }
    }
}