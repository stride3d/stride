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

        /// <summary>
        /// Get the bepu StaticReference /!\
        /// </summary>
        /// <returns>A volatil ref to the bepu static associed with this bodyContainer</returns>
        public StaticReference? GetPhysicStatic()
        {
            return ContainerData?.BepuSimulation.Simulation.Statics[ContainerData.SHandle];
        }

        private StaticReference GetPhysicStaticRef()
        {
            if (ContainerData == null)
                throw new Exception("Container data is null");

            return ContainerData.BepuSimulation.Simulation.Statics[ContainerData.SHandle];
        }

        [DataMemberIgnore]
        public Vector3 Position
        {
            get => GetPhysicStaticRef().Pose.Position.ToStrideVector();
            set
            {
                var bodyRef = GetPhysicStaticRef();
                bodyRef.Pose.Position = value.ToNumericVector();
            }
        }
        [DataMemberIgnore]
        public Quaternion Orientation
        {
            get => GetPhysicStaticRef().Pose.Orientation.ToStrideQuaternion();
            set
            {
                var bodyRef = GetPhysicStaticRef();
                bodyRef.Pose.Orientation = value.ToNumericQuaternion();
            }
        }
        [DataMemberIgnore]
        public ContinuousDetection ContinuousDetection
        {
            get => GetPhysicStaticRef().Continuity;
            set
            {
                var bodyRef = GetPhysicStaticRef();
                bodyRef.Continuity = value;
            }
        }
    }
}
