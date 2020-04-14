// Copyright (c) Stride contributors (https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Graphics;

namespace Stride.Rendering.Lights
{
    public class RenderLight
    {
        public ILight Type;
        public Matrix WorldMatrix;
        public float Intensity;

        /// <summary>
        /// Gets the light position in World-Space (computed by the <see cref="LightProcessor"/>) (readonly field). See remarks.
        /// </summary>
        /// <value>The position.</value>
        /// <remarks>This property should only be used inside a renderer and not from a script as it is updated after scripts</remarks>
        public Vector3 Position;

        /// <summary>
        /// Gets the light direction in World-Space (computed by the <see cref="LightProcessor"/>) (readonly field).
        /// </summary>
        /// <value>The direction.</value>
        /// <remarks>This property should only be used inside a renderer and not from a script as it is updated after scripts</remarks>
        public Vector3 Direction;

        public Color3 Color;

        /// <summary>
        /// The bounding box of this light in WS after the <see cref="LightProcessor"/> has been applied (readonly field).
        /// </summary>
        internal BoundingBox BoundingBox;

        /// <summary>
        /// The bounding box extents of this light in WS after the <see cref="LightProcessor"/> has been applied (readonly field).
        /// </summary>
        internal BoundingBoxExt BoundingBoxExt;

        /// <summary>
        /// The determines whether this instance has a valid bounding box (readonly field).
        /// </summary>
        internal bool HasBoundingBox;

        /// <summary>
        /// Updates this instance( <see cref="Position"/>, <see cref="Direction"/>, <see cref="HasBoundingBox"/>, <see cref="BoundingBox"/>, <see cref="BoundingBoxExt"/>
        /// </summary>
        /// <param name="colorSpace"></param>
        internal void UpdateBoundingBox()
        {
            // Compute bounding boxes
            HasBoundingBox = false;
            BoundingBox = new BoundingBox();
            BoundingBoxExt = new BoundingBoxExt();

            var directLight = Type as IDirectLight;
            if (directLight != null && directLight.HasBoundingBox)
            {
                // Computes the bounding boxes
                BoundingBox = directLight.ComputeBounds(Position, Direction);
                BoundingBoxExt = new BoundingBoxExt(BoundingBox);
            }
        }
    }
}
