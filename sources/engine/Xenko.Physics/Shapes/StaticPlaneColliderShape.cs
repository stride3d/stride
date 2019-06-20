// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Xenko.Core.Mathematics;
using Xenko.Extensions;
using Xenko.Graphics;
using Xenko.Graphics.GeometricPrimitives;
using Xenko.Rendering;

namespace Xenko.Physics
{
    public class StaticPlaneColliderShape : ColliderShape
    {
        public readonly Vector3 Normal;
        public readonly float Offset;

        /// <summary>
        /// Initializes a new instance of the <see cref="StaticPlaneColliderShape"/> class.
        /// A static plane that is solid to infinity on one side.
        /// Several of these can be used to confine a convex space in a manner that completely prevents tunneling to the outside.
        /// The plane itself is specified with a normal and distance as is standard in mathematics.
        /// </summary>
        /// <param name="normal">The normal.</param>
        /// <param name="offset">The offset.</param>
        public StaticPlaneColliderShape(Vector3 normalParam, float offsetParam)
        {
            Type = ColliderShapeTypes.StaticPlane;
            Is2D = false;
            Normal = normalParam;
            Offset = offsetParam;

            cachedScaling = Vector3.One;

            InternalShape = new BulletSharp.StaticPlaneShape(Normal, Offset)
            {
                LocalScaling = cachedScaling,
            };

            Matrix rotationMatrix;
            var oY = Vector3.Normalize(Normal);
            var oZ = Vector3.Cross(Vector3.UnitX, oY);
            if (oZ.Length() > MathUtil.ZeroTolerance)
            {
                oZ.Normalize();
                var oX = Vector3.Cross(oY, oZ);
                rotationMatrix = new Matrix(
                    oX.X, oX.Y, oX.Z, 0,
                    oY.X, oY.Y, oY.Z, 0,
                    oZ.X, oZ.Y, oZ.Z, 0,
                    0,       0,    0, 1);
            }
            else
            {
                var s = Math.Sign(oY.X);
                rotationMatrix = new Matrix(
                    0, s, 0, 0,
                    s, 0, 0, 0,
                    0, 0, 1, 0,
                    0, 0, 0, 1);
            }

            DebugPrimitiveMatrix = Matrix.Translation(Offset * Vector3.UnitY) * rotationMatrix;
        }

        public override MeshDraw CreateDebugPrimitive(GraphicsDevice device)
        {
            return GeometricPrimitive.Plane.New(device, 1000, 1000, 100, 100, normalDirection: NormalDirection.UpY).ToMeshDraw();
        }
    }
}
