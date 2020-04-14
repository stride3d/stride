// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using BulletSharp;
using Stride.Core.Mathematics;
using Stride.Extensions;
using Stride.Graphics;
using Stride.Graphics.GeometricPrimitives;
using Stride.Rendering;

namespace Stride.Physics
{
    public class CapsuleColliderShape : ColliderShape
    {
        public readonly float Length;
        public readonly float Radius;
        public readonly ShapeOrientation Orientation;

        /// <summary>
        /// Initializes a new instance of the <see cref="CapsuleColliderShape"/> class.
        /// </summary>
        /// <param name="is2D">if set to <c>true</c> [is2 d].</param>
        /// <param name="radius">The radius.</param>
        /// <param name="length">The length of the capsule.</param>
        /// <param name="orientation">Up axis.</param>
        public CapsuleColliderShape(bool is2D, float radius, float length, ShapeOrientation orientation)
        {
            Type = ColliderShapeTypes.Capsule;
            Is2D = is2D;

            Length = length;
            Radius = radius;
            Orientation = orientation;

            Matrix rotation;
            CapsuleShape shape;

            cachedScaling = Is2D ? new Vector3(1, 1, 0) : Vector3.One; 

            switch (orientation)
            {
                case ShapeOrientation.UpZ:
                    shape = new CapsuleShapeZ(radius, length)
                    {
                        LocalScaling = cachedScaling,
                    };
                    rotation = Matrix.RotationX((float)Math.PI / 2.0f);
                    break;

                case ShapeOrientation.UpY:
                    shape = new CapsuleShape(radius, length)
                    {
                        LocalScaling = cachedScaling,
                    };
                    rotation = Matrix.Identity;
                    break;

                case ShapeOrientation.UpX:
                    shape = new CapsuleShapeX(radius, length)
                    {
                        LocalScaling = cachedScaling,
                    };
                    rotation = Matrix.RotationZ((float)Math.PI / 2.0f);
                    break;

                default:
                    throw new ArgumentOutOfRangeException("orientation");
            }

            InternalShape = Is2D ? (CollisionShape)new Convex2DShape(shape) { LocalScaling = cachedScaling } : shape;

            DebugPrimitiveMatrix = Matrix.Scaling(new Vector3(DebugScaling)) * rotation;
        }

        public override MeshDraw CreateDebugPrimitive(GraphicsDevice device)
        {
            return GeometricPrimitive.Capsule.New(device, Length, Radius).ToMeshDraw();
        }

        public override Vector3 Scaling
        {
            get { return base.Scaling; }
            set
            {
                Vector3 newScaling;
                switch (Orientation)
                {
                    case ShapeOrientation.UpX:
                        {
                            newScaling = new Vector3(value.X, value.Z, value.Z);
                            break;
                        }
                    case ShapeOrientation.UpY:
                        {
                            newScaling = new Vector3(value.X, value.Y, value.X);
                            break;
                        }
                    case ShapeOrientation.UpZ:
                        {
                            newScaling = new Vector3(value.Y, value.Y, value.Z);
                            break;
                        }
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                base.Scaling = newScaling;
            }
        }
    }
}
