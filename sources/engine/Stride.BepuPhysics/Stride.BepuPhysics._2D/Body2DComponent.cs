// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using BepuPhysics;
using BepuPhysics.Collidables;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;

namespace Stride.BepuPhysics
{
    [ComponentCategory("Bepu")]
    public class Body2DComponent : BodyComponent
    {
        Vector3 _rotationLock = new Vector3(0, 0, 0);

        [DataMemberIgnore]
        internal Vector3 RotationLock
        {
            get
            {
                return _rotationLock;
            }
            set
            {
                _rotationLock = value;
                if (BodyReference is { } bRef)
                {
                    bRef.LocalInertia.InverseInertiaTensor.XX *= value.X;
                    bRef.LocalInertia.InverseInertiaTensor.YX *= value.X * value.Y;
                    bRef.LocalInertia.InverseInertiaTensor.ZX *= value.Z * value.X;
                    bRef.LocalInertia.InverseInertiaTensor.YY *= value.Y;
                    bRef.LocalInertia.InverseInertiaTensor.ZY *= value.Z * value.Y;
                    bRef.LocalInertia.InverseInertiaTensor.ZZ *= value.Z;
                }
            }
        }

        protected override void AttachInner(RigidPose pose, BodyInertia shapeInertia, TypedIndex shapeIndex)
        {
            base.AttachInner(pose, shapeInertia, shapeIndex);
#warning what about a body that become kinematic after some time ?
            if (!Kinematic)
                RotationLock = new Vector3(0, 0, 1);
        }
    }
}
