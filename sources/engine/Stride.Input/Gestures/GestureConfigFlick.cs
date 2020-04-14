// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

using Xenko.Core.Mathematics;

namespace Xenko.Input
{
    /// <summary> 
    /// Configuration class for the Flick gesture.
    /// </summary>
    /// <remarks>A Flick gesture can be composed of 1 or more fingers.</remarks>
    public sealed class GestureConfigFlick : GestureConfig
    {
        /// <summary>
        /// The (x,y) error margins allowed during directional dragging.
        /// </summary>
        /// <exception cref="InvalidOperationException">Tried to modify the configuration after it has been frozen by the system.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The provided x or y value was not positive.</exception>
        public Vector2 AllowedErrorMargins
        {
            get { return allowedErrorMargins; }
            set
            {
                CheckNotFrozen();

                if (value.X < 0 || value.Y < 0)
                    throw new ArgumentOutOfRangeException("value");

                allowedErrorMargins = value;
            }
        }
        private Vector2 allowedErrorMargins;

        /// <summary>
        /// The shape of the flick gesture.
        /// </summary>        
        /// <exception cref="InvalidOperationException">Tried to modify the configuration after it has been frozen by the system.</exception>
        public GestureShape FlickShape
        {
            get { return flickShape; }
            set
            {
                CheckNotFrozen();

                flickShape = value;
            }
        }
        private GestureShape flickShape;

        /// <summary>
        /// The minimum average speed of the gesture to be detected as a flick.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">The value must be positive</exception>
        /// <exception cref="InvalidOperationException">Tried to modify the configuration after it has been frozen by the system.</exception>
        public float MinimumAverageSpeed
        {
            get { return minimumAverageSpeed; }

            set
            {
                CheckNotFrozen();

                if (value < 0)
                    throw new ArgumentOutOfRangeException("value");

                minimumAverageSpeed = value;
            }
        }

        private float minimumAverageSpeed;

        /// <summary>
        /// The minimum distance that the flick gesture has to cross from its origin to be detected has Flick.
        /// </summary>
        /// <exception cref="InvalidOperationException">Tried to modify the configuration after it has been frozen by the system.</exception>
        public float MinimumFlickLength
        {
            get { return minimumFlickLength; }
            set
            {
                CheckNotFrozen();

                if (value <= 0)
                    throw new ArgumentOutOfRangeException("value");

                minimumFlickLength = value;
            }
        }
        private float minimumFlickLength;

        /// <summary>
        /// Create a default Flick gesture configuration for one finger free flicking.
        /// </summary>
        public GestureConfigFlick()
            : this(GestureShape.Free)
        {
        }

        /// <summary>
        /// Create a default gesture configuration for one finger flicking.
        /// </summary>
        /// <param name="flickShape">The shape of the flicking.</param>
        public GestureConfigFlick(GestureShape flickShape)
        {
            AssociatedGestureType = GestureType.Flick;

            FlickShape = flickShape;
            RequiredNumberOfFingers = 1;
            MinimumAverageSpeed = 0.4f;
            MinimumFlickLength = 0.04f;
            AllowedErrorMargins = 0.02f * Vector2.One;
        }

        internal override GestureRecognizer CreateRecognizerImpl(float screenRatio)
        {
            return new GestureRecognizerFlick(this, screenRatio);
        }
    }
}
