using BepuPhysics;
using BepuPhysics.Collidables;
using Stride.BepuPhysics.Components.Containers.Interfaces;
using Stride.BepuPhysics.Definitions;
using Stride.BepuPhysics.Definitions.Contacts;
using Stride.BepuPhysics.Extensions;
using Stride.BepuPhysics.Processors;
using Stride.Core;
using Stride.Core.Mathematics;

namespace Stride.BepuPhysics.Components.Containers
{
    public class StaticContainerComponent : ContainerComponent, IStaticContainer, IContainerWithColliders
    {

        [DataMemberIgnore]
        public new IContactEventHandler? ContactEventHandler
        {
            get => base.ContactEventHandler;
            set => base.ContactEventHandler = value;
        }

        #region Static

        private StaticReference GetPhysicStaticRef()
        {
            if (ContainerData == null)
                throw new Exception("Container data is null");

            return ContainerData.BepuSimulation.Simulation.Statics[ContainerData.SHandle];
        }

        /// <summary>
        /// Get the bepu StaticReference /!\
        /// </summary>
        /// <returns>A volatil ref to the bepu static associed with this bodyContainer</returns>
        [DataMemberIgnore]
        public StaticReference? GetPhysicStatic => ContainerData?.BepuSimulation.Simulation.Statics[ContainerData.SHandle];
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

        #endregion

        #region WithCollider

        public ListOfColliders Colliders { get; set; } = new();

        public StaticContainerComponent()
        {
            Colliders.OnEditCallBack = () => ContainerData?.TryUpdateContainer();
        }

        #endregion

    }
}
