// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Input
{
    /// <summary>
    /// Decides which game controller changes are applied while the game window does not have focus, the same way SDL does.
    /// </summary>
    /// <remarks>
    /// Without focus, a controller only moves back towards rest: buttons can be released, directions can return to neutral and axes
    /// can move towards their rest value. Presses and movement away from rest are ignored, so nothing new is applied while the
    /// player uses another window, and nothing stays held after they let go.
    /// </remarks>
    internal static class BackgroundInputFilter
    {
        /// <summary>
        /// Gets whether a button change is applied while the game window has no focus.
        /// </summary>
        /// <param name="isDown">The new state of the button.</param>
        public static bool AcceptsButton(bool isDown) => !isDown;

        /// <summary>
        /// Gets whether a direction change is applied while the game window has no focus.
        /// </summary>
        /// <param name="direction">The new direction.</param>
        public static bool AcceptsDirection(Direction direction) => direction.IsNeutral;

        /// <summary>
        /// Gets whether an axis change is applied while the game window has no focus.
        /// </summary>
        /// <param name="rest">The rest value of the axis.</param>
        /// <param name="current">The value the game currently has for the axis.</param>
        /// <param name="value">The new value of the axis.</param>
        public static bool AcceptsAxis(float rest, float current, float value)
        {
            return !((value > rest && value >= current) || (value < rest && value <= current));
        }
    }

    /// <summary>
    /// Tracks the rest value of a controller axis: the first value it reports, as in SDL.
    /// </summary>
    /// <remarks>
    /// Some devices report an axis at one end of its range until the first real reading. If the first value is at an end and the
    /// next one is near the centre, the rest value is taken again from the new value.
    /// </remarks>
    internal struct AxisRest
    {
        private const float ExtremeValue = 0.9999f;
        private const float CentreRange = 0.25f;

        private bool hasInitialValue;
        private bool hasSecondValue;
        private float initialValue;
        private float lastValue;

        /// <summary>
        /// Gets the rest value of the axis.
        /// </summary>
        public float Value { get; private set; }

        /// <summary>
        /// Records a value read from the device.
        /// </summary>
        /// <param name="value">The value of the axis.</param>
        public void Observe(float value)
        {
            if (!hasInitialValue || (!hasSecondValue && System.Math.Abs(initialValue) >= ExtremeValue && System.Math.Abs(value) < CentreRange))
            {
                initialValue = value;
                Value = value;
                hasInitialValue = true;
            }
            else if (value != lastValue)
            {
                hasSecondValue = true;
            }

            lastValue = value;
        }
    }
}
