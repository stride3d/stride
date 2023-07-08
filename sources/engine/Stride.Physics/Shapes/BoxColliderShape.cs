// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Mathematics;
using Stride.Extensions;
using Stride.Graphics;
using Stride.Graphics.GeometricPrimitives;
using Stride.Rendering;

namespace Stride.Physics
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

            // Note: Creating Convex 2D Shape from (3D) BoxShape, causes weird behaviour, 
            // better to instantiate Box2DShape directly (see issue #1707)
            if (Is2D)
            {
                InternalShape = new BulletSharp.Box2DShape(size / 2) { LocalScaling = cachedScaling };
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
