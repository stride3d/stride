// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

using Stride.Core;

namespace Stride.Input
{
    /// <summary>
    /// This represents the base class for all gesture configuration.
    /// </summary>
    /// <remarks>
    /// <para>Gesture configurations cannot be modified after being added to the input system for gesture recognition. Doing so will throw an <see cref="InvalidOperationException"/>.</para>
    /// <para>Gesture Recognizers work with normalized coordinates belonging to [0,1]x[0,1/screenRatio] 
    /// so distances, speeds and margin errors need to be expressed relatively to this coordinates system.</para>
    /// </remarks>
    public abstract class GestureConfig
    {
        /// <summary>
        /// Specify the <see cref="GestureType"/> corresponding to this configuration.
        /// </summary>
        public GestureType AssociatedGestureType { get; protected set; }

        private readonly int restrictedNumberOfFinger;

        internal GestureConfig() : this(0)
        {
        }

        internal GestureConfig(int numberOfFinger)
        {
            AssociatedGestureType = (GestureType)(-1);

            restrictedNumberOfFinger = numberOfFinger;
        }

        /// <summary>
        /// This value represents the required number of simultaneous finger to tap to trigger the gesture. For example: 1 for single finger, and so on...
        /// </summary>
        /// <remarks>This value is strictly positive.</remarks>
        /// <exception cref="ArgumentOutOfRangeException">The given value is not in the allowed range.</exception>
        /// <exception cref="InvalidOperationException">Tried to modify the configuration after it has been frozen by the system.</exception>
        public int RequiredNumberOfFingers
        {
            get { return requiredNumberOfFingers; }
            set
            {
                CheckNotFrozen();

                if (value < 1)
                    throw new ArgumentOutOfRangeException("value");

                if (restrictedNumberOfFinger != 0 && value != restrictedNumberOfFinger)
                    throw new ArgumentOutOfRangeException("value");

                requiredNumberOfFingers = value;
            }
        }
        private int requiredNumberOfFingers;

        /// <summary>
        /// Indicate that the configuration is frozen and cannot be modified anymore by the user.
        /// </summary>
        private bool frozen;

        /// <summary>
        /// Freeze the configuration so that the user cannot modify it anymore.
        /// </summary>
        private void Freeze()
        {
            frozen = true;
        }

        internal void CheckNotFrozen()
        {
            if (frozen)
                throw new InvalidOperationException("Tried to modify the configuration after is has been frozen.");
        }

        /// <summary>
        /// Create a recognizer for the current configuration.
        /// </summary>
        /// <returns></returns>
        internal GestureRecognizer CreateRecognizer(float screenRatio)
        {
            // freeze the configuration so that the user cannot modify it from outside.
            Freeze();

            return CreateRecognizerImpl(screenRatio);
        }

        internal abstract GestureRecognizer CreateRecognizerImpl(float screenRatio);
    }
}
