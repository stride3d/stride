// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

using Xenko.Core.Mathematics;

namespace Xenko.Input
{
    /// <summary>
    /// Configuration class for the Drag gesture.
    /// </summary>
    /// <remarks>A drag gesture can be composed of 1 or more fingers.</remarks>
    public sealed class GestureConfigDrag : GestureConfig
    {
        /// <summary>
        /// Specify the minimum translation distance required  before that the gesture can be recognized as a Drag.
        /// </summary>
        /// <exception cref="InvalidOperationException">Tried to modify the configuration after it has been frozen by the system.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The provided value was negative.</exception>
        /// <remarks>The user can reduce this value if he needs the drag gesture to be triggered even for very small drags.
        /// On the contrary, he can increase this value if he wants to avoid to deals with too small drags.</remarks>
        public float MinimumDragDistance
        {
            get { return minimumDragDistance; }
            set
            {
                CheckNotFrozen();

                if (value < 0)
                    throw new ArgumentOutOfRangeException("value");

                minimumDragDistance = value;
            }
        }

        private float minimumDragDistance;

        /// <summary>
        /// The (x,y) error margins allowed during directional dragging.
        /// </summary>
        /// <exception cref="InvalidOperationException">Tried to modify the configuration after it has been frozen by the system.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The provided x or y value was not positive.</exception>
        /// <remarks>Those values are used only for directional (vertical or horizontal) dragging. 
        /// Decrease those values to trigger the gesture only when the dragging is perfectly in the desired direction.
        /// Increase those values to allow directional gestures to be more approximative.</remarks>
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
        /// The shape (direction) of the drag gesture.
        /// </summary>
        /// <exception cref="InvalidOperationException">Tried to modify the configuration after it has been frozen by the system.</exception>
        public GestureShape DragShape
        {
            get { return dragShape; }
            set
            {
                CheckNotFrozen();

                dragShape = value;
            }
        }
        private GestureShape dragShape;
        
        /// <summary>
        /// Create a default drag gesture configuration for one finger free dragging.
        /// </summary>
        public GestureConfigDrag()
            : this(GestureShape.Free)
        {
        }

        /// <summary> 
        /// Create a default drag gesture configuration for one finger dragging.
        /// </summary>
        /// <param name="dragShape">The dragging shape</param>
        public GestureConfigDrag(GestureShape dragShape)
        {
            AssociatedGestureType = GestureType.Drag;

            DragShape = dragShape;
            RequiredNumberOfFingers = 1;
            AllowedErrorMargins = 0.02f * Vector2.One;
            MinimumDragDistance = 0.02f;
        }

        internal override GestureRecognizer CreateRecognizerImpl(float screenRatio)
        {
            return new GestureRecognizerDrag(this, screenRatio);
        }
    }
}
