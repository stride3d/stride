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
    public class ConeColliderShape : ColliderShape
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConeColliderShape"/> class.
        /// </summary>
        /// <param name="orientation">Up axis.</param>
        /// <param name="radius">The radius of the cone</param>
        /// <param name="height">The height of the cone</param>
        public ConeColliderShape(float height, float radius, ShapeOrientation orientation)
        {
            Type = ColliderShapeTypes.Cone;
            Is2D = false; //always false for cone

            Matrix rotation;

            cachedScaling = Vector3.One;

            switch (orientation)
            {
                case ShapeOrientation.UpX:
                    InternalShape = new BulletSharp.ConeShapeX(radius, height)
                    {
                        LocalScaling = cachedScaling,
                    };
                    rotation = Matrix.RotationZ((float)Math.PI / 2.0f);
                    break;
                case ShapeOrientation.UpY:
                    InternalShape = new BulletSharp.ConeShape(radius, height)
                    {
                        LocalScaling = cachedScaling,
                    };
                    rotation = Matrix.Identity;
                    break;
                case ShapeOrientation.UpZ:
                    InternalShape = new BulletSharp.ConeShapeZ(radius, height)
                    {
                        LocalScaling = cachedScaling,
                    };
                    rotation = Matrix.RotationX((float)Math.PI / 2.0f);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(orientation));
            }

            DebugPrimitiveMatrix = Matrix.Scaling(new Vector3(radius * 2, height, radius * 2) * DebugScaling) * rotation;
        }

        public override MeshDraw CreateDebugPrimitive(GraphicsDevice device)
        {
            return GeometricPrimitive.Cone.New(device).ToMeshDraw();
        }
    }
}
