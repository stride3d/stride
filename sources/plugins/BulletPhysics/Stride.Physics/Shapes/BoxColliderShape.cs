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

            if (Is2D)
            {
                // Note that encapsulating a 2D Box goes against bullet's 2D collision example.
                // This was found through trial and error as the most stable solution, see issue #1707 and #2019
                InternalShape = new BulletSharp.Convex2DShape(new BulletSharp.Box2DShape(size / 2) { LocalScaling = Vector3.One }) { LocalScaling = cachedScaling };
            }
            else
            {
                InternalShape = new BulletSharp.BoxShape(size / 2) { LocalScaling = cachedScaling };
            }

            DebugPrimitiveMatrix = Matrix.Scaling(size * DebugScaling);
        }

        public override MeshDraw CreateDebugPrimitive(GraphicsDevice device)
        {
            return GeometricPrimitive.Cube.New(device).ToMeshDraw();
        }
    }
}
