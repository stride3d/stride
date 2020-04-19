// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

namespace Stride.Input
{
    /// <summary>
    /// Configuration class for the Tap gesture.
    /// </summary>
    /// <remarks>A tap gesture can be composed of 1 or more fingers.</remarks>
    public sealed class GestureConfigTap : GestureConfig
    {
        /// <summary>
        /// This value represents the required number of successive user touches to trigger the gesture. For example: 1 for single touch, 2 for double touch, and so on...
        /// </summary>
        /// <remarks>This value is strictly positive.</remarks>
        /// <exception cref="ArgumentOutOfRangeException">The given value is not greater or equal to 1.</exception>
        /// <exception cref="InvalidOperationException">Tried to modify the configuration after it has been frozen by the system.</exception>
        public int RequiredNumberOfTaps
        {
            get { return requiredNumberOfTaps; }
            set
            {
                CheckNotFrozen();

                if (value < 1)
                    throw new ArgumentOutOfRangeException("value");

                requiredNumberOfTaps = value;
            }
        }
        private int requiredNumberOfTaps;

        /// <summary>
        /// This value represents the maximum interval of time that can separate two touches of a same gesture. 
        /// By reducing this value, the system will tend to detect multi-touch gesture has several single touch gesture.
        /// By increasing this value, the system will tend to regroup distant (in time) single touch gestures into a multi-touch gesture.
        /// </summary>
        public TimeSpan MaximumTimeBetweenTaps 
        {
            get { return maximumTimeBetweenTaps; }
            set
            {
                CheckNotFrozen();

                maximumTimeBetweenTaps = value;
            }
        }
        private TimeSpan maximumTimeBetweenTaps;

        /// <summary>
        /// This value represents the maximum amount of time that the user can stay touching the screen before taking off its finger. 
        /// </summary>
        public TimeSpan MaximumPressTime
        {
            get { return maximumPressTime; }
            set
            {
                CheckNotFrozen();

                maximumPressTime = value;
            }
        }
        private TimeSpan maximumPressTime;

        /// <summary>
        /// The value represents the maximum distance that can separate two touches of the same finger during the gesture.
        /// By reducing this value, the system will tend to detect multi-touch gesture has several single touch gesture.
        /// By increasing this value, the system will tend to regroup distant single touch gestures into a multi-touch gesture.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">The value has to be positive.</exception>
        /// <exception cref="InvalidOperationException">Tried to modify the configuration after it has been frozen by the system.</exception>
        public float MaximumDistanceTaps
        {
            get { return maximumDistanceTaps; }
            set
            {
                CheckNotFrozen();

                if (value < 0)
                    throw new ArgumentOutOfRangeException("value");

                maximumDistanceTaps = value;
            }
        }
        private float maximumDistanceTaps;

        /// <summary>
        /// Create a default Tap gesture configuration for single touch and single finger detection.
        /// </summary>
        public GestureConfigTap()
            : this(1, 1)
        {
        }

        /// <summary>
        /// Create a default Tap gesture configuration for the given numbers of touches and fingers.
        /// </summary>
        /// <param name="numberOfTap">The number of taps required</param>
        /// <param name="numberOfFingers">The number of fingers required</param>
        public GestureConfigTap(int numberOfTap, int numberOfFingers)
        {
            AssociatedGestureType = GestureType.Tap;

            RequiredNumberOfTaps = numberOfTap;
            RequiredNumberOfFingers = numberOfFingers;

            MaximumTimeBetweenTaps = TimeSpan.FromMilliseconds(400);
            MaximumDistanceTaps = 0.04f;
            MaximumPressTime = TimeSpan.FromMilliseconds(100);
        }

        internal override GestureRecognizer CreateRecognizerImpl(float screenRatio)
        {
            return new GestureRecognizerTap(this, screenRatio);
        }
    }
}
