using BepuPhysics;
using BepuPhysics.Collidables;
using Stride.BepuPhysics.Components.Containers;
using Stride.BepuPhysics.Components.Containers.Interfaces;
using Stride.BepuPhysics.Definitions;
using Stride.BepuPhysics.Extensions;
using Stride.BepuPhysics.Processors;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;

namespace Stride.BepuPhysics._2D.Components.Containers
{
    [ComponentCategory("Bepu - Containers 2D")]
    public class _2DBodyContainerComponent : BodyContainerComponent
    {

        [DataMemberIgnore]
        public Vector3 RotationLock
        {
            get
            {
                if (GetPhysicBodyRef() is { } bRef)
                {
                    var inverseInertiaTensor = bRef.LocalInertia.InverseInertiaTensor;
                    return new Vector3(inverseInertiaTensor.ZX, inverseInertiaTensor.ZY, inverseInertiaTensor.ZZ);
                }

                return default;
            }
            set
            {
                if (GetPhysicBodyRef() is { } bRef)
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

        public override void Start()
        {
            if (!Kinematic)
                RotationLock = new Vector3(0, 0, 1);
            base.Start();
        }

    }
}
