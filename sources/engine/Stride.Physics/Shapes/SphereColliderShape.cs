// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Mathematics;
using Stride.Extensions;
using Stride.Graphics;
using Stride.Graphics.GeometricPrimitives;
using Stride.Rendering;

namespace Stride.Physics
{
    public class SphereColliderShape : ColliderShape
    {
        public readonly float Radius;

        /// <summary>
        /// Initializes a new instance of the <see cref="SphereColliderShape"/> class.
        /// </summary>
        /// <param name="is2D">if set to <c>true</c> [is2 d].</param>
        /// <param name="radius">The radius.</param>
        public SphereColliderShape(bool is2D, float radiusParam)
        {
            Type = ColliderShapeTypes.Sphere;
            Is2D = is2D;
            Radius = radiusParam;

            cachedScaling = Is2D ? new Vector3(1, 1, 0) : Vector3.One;

            var shape = new BulletSharp.SphereShape(Radius)
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

            DebugPrimitiveMatrix = Matrix.Scaling(2 * Radius * DebugScaling);
            if (Is2D)
            {
                DebugPrimitiveMatrix.M33 = 0f;
            }
        }

        public override MeshDraw CreateDebugPrimitive(GraphicsDevice device)
        {
            return GeometricPrimitive.Sphere.New(device).ToMeshDraw();
        }
    }
}
