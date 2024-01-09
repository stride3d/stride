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
                var inverseInertiaTensor = GetPhysicBodyRef().LocalInertia.InverseInertiaTensor;
                return new Vector3(inverseInertiaTensor.ZX, inverseInertiaTensor.ZY, inverseInertiaTensor.ZZ);
            }
            set
            {
                var inverseInertiaTensor = GetPhysicBodyRef().LocalInertia;
                inverseInertiaTensor.InverseInertiaTensor.ZX = value.X;
                inverseInertiaTensor.InverseInertiaTensor.ZY = value.Y;
                inverseInertiaTensor.InverseInertiaTensor.ZZ = value.Z;
                GetPhysicBodyRef().SetLocalInertia(inverseInertiaTensor);
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
