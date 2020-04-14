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
    public class CylinderColliderShape : ColliderShape
    {
        public readonly ShapeOrientation Orientation;
        public readonly float Height;
        public readonly float Radius;

        /// <summary>
        /// Initializes a new instance of the <see cref="CylinderColliderShape"/> class.
        /// </summary>
        /// <param name="orientation">Up axis.</param>
        /// <param name="radius">The radius of the cylinder</param>
        /// <param name="height">The height of the cylinder</param>
        public CylinderColliderShape(float heightParam, float radiusParam, ShapeOrientation orientationParam)
        {
            Type = ColliderShapeTypes.Cylinder;
            Is2D = false; //always false for cylinders
            Height = heightParam;
            Radius = radiusParam;

            Matrix rotation;

            cachedScaling = Vector3.One;
            Orientation = orientationParam;

            switch (Orientation)
            {
                case ShapeOrientation.UpX:
                    InternalShape = new BulletSharp.CylinderShapeX(new Vector3(Height / 2, Radius, Radius))
                    {
                        LocalScaling = cachedScaling,
                    };
                    rotation = Matrix.RotationZ((float)Math.PI / 2.0f);
                    break;
                case ShapeOrientation.UpY:
                    InternalShape = new BulletSharp.CylinderShape(new Vector3(Radius, Height / 2, Radius))
                    {
                        LocalScaling = cachedScaling,
                    };
                    rotation = Matrix.Identity;
                    break;
                case ShapeOrientation.UpZ:
                    InternalShape = new BulletSharp.CylinderShapeZ(new Vector3(Radius, Radius, Height / 2))
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
            return GeometricPrimitive.Cylinder.New(device).ToMeshDraw();
        }
    }
}
