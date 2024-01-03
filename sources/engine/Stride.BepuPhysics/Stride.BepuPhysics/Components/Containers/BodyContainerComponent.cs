using BepuPhysics;
using BepuPhysics.Collidables;
using Stride.BepuPhysics.Components.Containers.Interfaces;
using Stride.BepuPhysics.Definitions;
using Stride.BepuPhysics.Extensions;
using Stride.BepuPhysics.Processors;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Engine.Design;

namespace Stride.BepuPhysics.Components.Containers
{
    [DataContract(Inherited = true)]
    [DefaultEntityComponentProcessor(typeof(ContainerProcessor), ExecutionMode = ExecutionMode.Runtime)]
    [ComponentCategory("Bepu - Containers")]
    public class BodyContainerComponent : ContainerComponent, IBodyContainer, IContainerWithColliders
    {

        #region Body

        private bool _kinematic = false;
        private float _sleepThreshold = 0.01f;
        private byte _minimumTimestepCountUnderThreshold = 32;

        public bool Kinematic
        {
            get => _kinematic;
            set
            {
                _kinematic = value;
                ContainerData?.TryUpdateContainer();
            }
        }
        public float SleepThreshold
        {
            get => _sleepThreshold;
            set
            {
                _sleepThreshold = value;
                ContainerData?.TryUpdateContainer();
            }
        }
        public byte MinimumTimestepCountUnderThreshold
        {
            get => _minimumTimestepCountUnderThreshold;
            set
            {
                _minimumTimestepCountUnderThreshold = value;
                ContainerData?.TryUpdateContainer();
            }
        }

        private BodyReference GetPhysicBodyRef()
        {
            if (ContainerData == null)
                throw new Exception("Container data is null");

            return ContainerData.BepuSimulation.Simulation.Bodies[ContainerData.BHandle];
        }
        /// <summary>
        /// Get the bepu BodyReference /!\
        /// </summary>
        /// <returns>A volatil ref to the bepu body associed with this bodyContainer</returns>
        [DataMemberIgnore]
        public BodyReference? GetPhysicBody => ContainerData?.BepuSimulation.Simulation.Bodies[ContainerData.BHandle];
        [DataMemberIgnore]
        public bool Awake
        {
            get => GetPhysicBodyRef().Awake;
            set
            {
                var bodyRef = GetPhysicBodyRef();
                bodyRef.Awake = value;
            }
        }
        [DataMemberIgnore]
        public Vector3 LinearVelocity
        {
            get => GetPhysicBodyRef().Velocity.Linear.ToStrideVector();
            set
            {
                var bodyRef = GetPhysicBodyRef();
                bodyRef.Velocity.Linear = value.ToNumericVector();
            }
        }
        [DataMemberIgnore]
        public Vector3 AngularVelocity
        {
            get => GetPhysicBodyRef().Velocity.Angular.ToStrideVector();
            set
            {
                var bodyRef = GetPhysicBodyRef();
                bodyRef.Velocity.Angular = value.ToNumericVector();
            }
        }
        [DataMemberIgnore]
        public Vector3 Position
        {
            get => GetPhysicBodyRef().Pose.Position.ToStrideVector();
            set
            {
                var bodyRef = GetPhysicBodyRef();
                bodyRef.Pose.Position = value.ToNumericVector();
            }
        }
        [DataMemberIgnore]
        public Quaternion Orientation
        {
            get => GetPhysicBodyRef().Pose.Orientation.ToStrideQuaternion();
            set
            {
                var bodyRef = GetPhysicBodyRef();
                bodyRef.Pose.Orientation = value.ToNumericQuaternion();
            }
        }
        [DataMemberIgnore]
        public BodyInertia BodyInertia
        {
            get => GetPhysicBodyRef().LocalInertia;
            set
            {
                var bodyRef = GetPhysicBodyRef();
                bodyRef.LocalInertia = value;
            }
        }
        [DataMemberIgnore]
        public float SpeculativeMargin
        {
            get => GetPhysicBodyRef().Collidable.SpeculativeMargin;
            set
            {
                var bodyRef = GetPhysicBodyRef();
                bodyRef.Collidable.SpeculativeMargin = value;
            }
        }
        [DataMemberIgnore]
        public ContinuousDetection ContinuousDetection
        {
            get => GetPhysicBodyRef().Collidable.Continuity;
            set
            {
                var bodyRef = GetPhysicBodyRef();
                bodyRef.Collidable.Continuity = value;
            }
        }

        public void ApplyImpulse(Vector3 impulse, Vector3 impulseOffset)
        {
            GetPhysicBodyRef().ApplyImpulse(impulse.ToNumericVector(), impulseOffset.ToNumericVector());
        }
        public void ApplyAngularImpulse(Vector3 impulse)
        {
            GetPhysicBodyRef().ApplyAngularImpulse(impulse.ToNumericVector());
        }
        public void ApplyLinearImpulse(Vector3 impulse)
        {
            GetPhysicBodyRef().ApplyLinearImpulse(impulse.ToNumericVector());
        }

        #endregion

        #region WithCollider

        public ListOfColliders Colliders { get; set; } = new();


        public BodyContainerComponent()
        {
            Colliders.OnEditCallBack = () => ContainerData?.TryUpdateContainer();
        }

        #endregion

    }
}
