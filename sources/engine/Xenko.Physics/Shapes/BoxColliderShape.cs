// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xenko.Core.Mathematics;
using Xenko.Extensions;
using Xenko.Graphics;
using Xenko.Graphics.GeometricPrimitives;
using Xenko.Rendering;

namespace Xenko.Physics
{
    public class BoxColliderShape : ColliderShape
    {
        public readonly Vector3 BoxSize;

        /// <summary>
        /// Initializes a new instance of the <see cref="BoxColliderShape"/> class.
        /// </summary>
        /// <param name="is2D">If this cube is a 2D quad</param>
        /// <param name="size">The size of the cube</param>
        public BoxColliderShape(bool is2D, Vector3 size)
        {
            Type = ColliderShapeTypes.Box;
            Is2D = is2D;
            BoxSize = size;

            //Box is not working properly when in a convex2dshape, Z cannot be 0

            cachedScaling = Is2D ? new Vector3(1, 1, 0.001f) : Vector3.One;

            if (is2D) size.Z = 0.001f;

            var shape = new BulletSharp.BoxShape(size / 2)
            {
                LocalScaling = cachedScaling,
            };

            if (Is2D)
            {
                InternalShape = new BulletSharp.Convex2DShape(shape) { LocalScaling = cachedScaling };
            }
            else
            {
                InternalShape = shape;
            }

            DebugPrimitiveMatrix = Matrix.Scaling(size * DebugScaling);
        }

        public override MeshDraw CreateDebugPrimitive(GraphicsDevice device)
        {
            return GeometricPrimitive.Cube.New(device).ToMeshDraw();
        }
    }
}
