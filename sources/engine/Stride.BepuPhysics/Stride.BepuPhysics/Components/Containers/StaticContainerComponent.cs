using BepuPhysics;
using BepuPhysics.Collidables;
using Stride.BepuPhysics.Extensions;
using Stride.BepuPhysics.Processors;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Engine.Design;

namespace Stride.BepuPhysics.Components.Containers
{
    [DataContract]
    [DefaultEntityComponentProcessor(typeof(ContainerProcessor), ExecutionMode = ExecutionMode.Runtime)]
    [ComponentCategory("Bepu - Containers")]
    public class StaticContainerComponent : ContainerComponent
    {
#warning This will be deleted !!!
        StaticReference? GetPhysicStatic()
        {
            return ContainerData?.BepuSimulation.Simulation.Statics[ContainerData.SHandle];
        }

        private StaticReference GetRef()
        {
            if (ContainerData == null)
                throw new Exception("Container data is null");

            return ContainerData.BepuSimulation.Simulation.Statics[ContainerData.SHandle];
        }

        [DataMemberIgnore]
        public Vector3 Position
        {
            get => GetRef().Pose.Position.ToStrideVector();
            set
            {
                var bodyRef = GetRef();
                bodyRef.Pose.Position = value.ToNumericVector();
            }
        }
        [DataMemberIgnore]
        public Quaternion Orientation
        {
            get => GetRef().Pose.Orientation.ToStrideQuaternion();
            set
            {
                var bodyRef = GetRef();
                bodyRef.Pose.Orientation = value.ToNumericQuaternion();
            }
        }
        [DataMemberIgnore]
        public ContinuousDetection ContinuousDetection
        {
            get => GetRef().Continuity;
            set
            {
                var bodyRef = GetRef();
                bodyRef.Continuity = value;
            }
        }
    }
}
