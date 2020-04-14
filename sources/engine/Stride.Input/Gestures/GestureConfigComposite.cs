// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

namespace Stride.Input
{
    /// <summary>
    /// Configuration class for the Composite gesture.
    /// </summary>
    /// <remarks>
    /// <para>A composite gesture is a transformation which is a composition of a translation, a rotation and a scale.
    /// It is performed by using two fingers and performing translation, scale and rotation motions.</para>
    /// <para>A composite gesture can only be composed of 2 fingers. 
    /// Trying to modify the <see cref="GestureConfig.RequiredNumberOfFingers"/> field will throw an exception.</para></remarks>
    public sealed class GestureConfigComposite : GestureConfig
    {
        /// <summary>
        /// The scale value above which the gesture is started.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">The value has to be greater or equal to 1.</exception>
        /// <exception cref="InvalidOperationException">Tried to modify the configuration after it has been frozen by the system.</exception>
        /// <remarks>The user can increase this value if he has small or no interest in the scale component of the transformation. 
        /// By doing so, he avoids triggering the Composite Gesture when only small scale changes happen. 
        /// On the contrary, the user can decrease this value if he wants to be immediately warned about the smallest change in scale.</remarks>
        public float MinimumScaleValue
        {
            get { return minimumScaleValue; }
            set
            {
                CheckNotFrozen();

                if (value <= 1)
                    throw new ArgumentOutOfRangeException("value");

                minimumScaleValue = value;
                MinimumScaleValueInv = 1 / minimumScaleValue;
            }
        }
        private float minimumScaleValue;

        internal float MinimumScaleValueInv { get; set; }

        /// <summary>
        /// The translation distance above which the gesture is started.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">The value has to be positive.</exception>
        /// <exception cref="InvalidOperationException">Tried to modify the configuration after it has been frozen by the system.</exception>
        /// <remarks>The user can increase this value if he has small or no interest in the translation component of the transformation. 
        /// By doing so, he avoids triggering the Composite Gesture when only small translation changes happen. 
        /// On the contrary, the user can decrease this value if he wants to be immediately warned about the smallest change in translation.</remarks>
        public float MinimumTranslationDistance
        {
            get { return mminimumTranslationDistance; }
            set
            {
                CheckNotFrozen();

                if (value < 0)
                    throw new ArgumentOutOfRangeException("value");

                mminimumTranslationDistance = value;
            }
        }
        private float mminimumTranslationDistance;

        /// <summary>
        /// The rotation angle (in radian) above which the gesture is started.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">The angle has to be strictly positive.</exception>
        /// <exception cref="InvalidOperationException">Tried to modify the configuration after it has been frozen by the system.</exception>
        /// <remarks>The user can increase this value if he has small or no interest in the rotation component of the transformation. 
        /// By doing so, he avoids triggering the Composite Gesture when only small rotation changes happen. 
        /// On the contrary, the user can decrease this value if he wants to be immediately warned about the smallest change in rotation.</remarks>
        public float MinimumRotationAngle
        {
            get { return minimumRotationAngle; }
            set
            {
                CheckNotFrozen();

                if (value <= 0)
                    throw new ArgumentOutOfRangeException("value");

                minimumRotationAngle = value;
            }
        }

        private float minimumRotationAngle;

        /// <summary>
        /// Create a default Rotation gesture configuration.
        /// </summary>
        public GestureConfigComposite()
            : base(2)
        {
            AssociatedGestureType = GestureType.Composite;

            RequiredNumberOfFingers = 2;
            MinimumRotationAngle = 0.1f;
            MinimumScaleValue = 1.075f;
            MinimumTranslationDistance = 0.016f;
        }

        internal override GestureRecognizer CreateRecognizerImpl(float screenRatio)
        {
            return new GestureRecognizerComposite(this, screenRatio);
        }
    }
}
