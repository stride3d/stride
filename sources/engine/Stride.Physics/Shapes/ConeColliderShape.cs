// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core.Mathematics;
using Stride.Extensions;
using Stride.Graphics;
using Stride.Graphics.GeometricPrimitives;
using Stride.Rendering;

namespace Stride.Physics
{
    public class ConeColliderShape : ColliderShape
    {
        public readonly float Height;
        public readonly float Radius;
        public readonly ShapeOrientation Orientation;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConeColliderShape"/> class.
        /// </summary>
        /// <param name="orientation">Up axis.</param>
        /// <param name="radius">The radius of the cone</param>
        /// <param name="height">The height of the cone</param>
        public ConeColliderShape(float heightParam, float radiusParam, ShapeOrientation orientationParam)
        {
            Type = ColliderShapeTypes.Cone;
            Is2D = false; //always false for cone
            Height = heightParam;
            Radius = radiusParam;
            Orientation = orientationParam;

            Matrix rotation;

            cachedScaling = Vector3.One;

            switch (Orientation)
            {
                case ShapeOrientation.UpX:
                    InternalShape = new BulletSharp.ConeShapeX(Radius, Height)
                    {
                        LocalScaling = cachedScaling,
                    };
                    rotation = Matrix.RotationZ((float)Math.PI / 2.0f);
                    break;
                case ShapeOrientation.UpY:
                    InternalShape = new BulletSharp.ConeShape(Radius, Height)
                    {
                        LocalScaling = cachedScaling,
                    };
                    rotation = Matrix.Identity;
                    break;
                case ShapeOrientation.UpZ:
                    InternalShape = new BulletSharp.ConeShapeZ(Radius, Height)
                    {
                        LocalScaling = cachedScaling,
                    };
                    rotation = Matrix.RotationX((float)Math.PI / 2.0f);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(Orientation));
            }

            DebugPrimitiveMatrix = Matrix.Scaling(new Vector3(Radius * 2, Height, Radius * 2) * DebugScaling) * rotation;
        }

        public override MeshDraw CreateDebugPrimitive(GraphicsDevice device)
        {
            return GeometricPrimitive.Cone.New(device).ToMeshDraw();
        }
    }
}
