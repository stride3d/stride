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
    public class BodyContainerComponent : ContainerComponent, IBodyContainer, IContainerWithColliders
    {
        [DataMemberIgnore]
        public new IContactEventHandler? ContactEventHandler
        {
            get => base.ContactEventHandler;
            set => base.ContactEventHandler = value;
        }

        #region Body

        private bool _kinematic = false;
        private bool _ignoreGlobalGravity = false;
        private ContinuousDetection _continuous = ContinuousDetection.Discrete;
        private float _sleepThreshold = 0.01f;
        private byte _minimumTimestepCountUnderThreshold = 32;
        private Interpolation _interpolation = Interpolation.None;

        internal RigidPose PreviousPose, CurrentPose; //Sets by AfterSimulationUpdate()

        public bool Kinematic
        {
            get => _kinematic;
            set
            {
                _kinematic = value;
                ContainerData?.TryUpdateContainer();
            }
        }

        /// <summary> Whether to ignore the simulation's <see cref="Configurations.BepuSimulation.PoseGravity"/> </summary>
        /// <remarks> Gravity is always active if <see cref="Configurations.BepuSimulation.UsePerBodyAttributes"/> is false </remarks>
        public bool IgnoreGlobalGravity
        {
            get => _ignoreGlobalGravity;
            set
            {
                if (_ignoreGlobalGravity == value)
                    return;

                _ignoreGlobalGravity = value;
                ContainerData?.UpdateMaterialProperties();
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

        public Interpolation Interpolation
        {
            get => _interpolation;
            set
            {
                if (_interpolation == Interpolation.None && value != Interpolation.None)
                    ContainerData?.BepuSimulation.RegisterInterpolated(this);
                if (_interpolation != Interpolation.None && value == Interpolation.None)
                    ContainerData?.BepuSimulation.UnregisterInterpolated(this);
                _interpolation = value;
            }
        }

        /// <summary>
        /// Shortcut to <see cref="ContinuousDetection"/>.<see cref="ContinuousDetection.Mode"/>
        /// </summary>
        public ContinuousDetectionMode ContinuousDetectionMode
        {
            get => _continuous.Mode;
            set
            {
                if (_continuous.Mode == value)
                    return;

                _continuous = value switch
                {
                    ContinuousDetectionMode.Discrete => ContinuousDetection.Discrete,
                    ContinuousDetectionMode.Passive => ContinuousDetection.Passive,
                    ContinuousDetectionMode.Continuous => ContinuousDetection.Continuous(),
                    _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
                };
            }
        }

        [DataMemberIgnore]
        public bool Awake
        {
            get => GetPhysicBodyRef()?.Awake ?? false;
            set
            {
                if (GetPhysicBodyRef() is {} bodyRef)
                    bodyRef.Awake = value;
            }
        }
        [DataMemberIgnore]
        public Vector3 LinearVelocity
        {
            get => GetPhysicBodyRef()?.Velocity.Linear.ToStrideVector() ?? default;
            set
            {
                if (GetPhysicBodyRef() is {} bodyRef)
                    bodyRef.Velocity.Linear = value.ToNumericVector();
            }
        }
        [DataMemberIgnore]
        public Vector3 AngularVelocity
        {
            get => GetPhysicBodyRef()?.Velocity.Angular.ToStrideVector() ?? default;
            set
            {
                if (GetPhysicBodyRef() is {} bodyRef)
                    bodyRef.Velocity.Angular = value.ToNumericVector();
            }
        }
        [DataMemberIgnore]
        public Vector3 Position
        {
            get => GetPhysicBodyRef()?.Pose.Position.ToStrideVector() ?? default;
            set
            {
                if (GetPhysicBodyRef() is {} bodyRef)
                    bodyRef.Pose.Position = value.ToNumericVector();
            }
        }
        [DataMemberIgnore]
        public Quaternion Orientation
        {
            get => GetPhysicBodyRef()?.Pose.Orientation.ToStrideQuaternion() ?? Quaternion.Identity;
            set
            {
                if (GetPhysicBodyRef() is {} bodyRef)
                    bodyRef.Pose.Orientation = value.ToNumericQuaternion();
            }
        }
        [DataMemberIgnore]
        public BodyInertia BodyInertia
        {
            get => GetPhysicBodyRef()?.LocalInertia ?? default;
            set
            {
                if (GetPhysicBodyRef() is {} bodyRef)
                    bodyRef.LocalInertia = value;
            }
        }
        [DataMemberIgnore]
        public float SpeculativeMargin
        {
            get => GetPhysicBodyRef()?.Collidable.SpeculativeMargin ?? default;
            set
            {
                if (GetPhysicBodyRef() is {} bodyRef)
                    bodyRef.Collidable.SpeculativeMargin = value;
            }
        }

        [DataMemberIgnore]
        public ContinuousDetection ContinuousDetection
        {
            get => _continuous;
            set
            {
                _continuous = value;
                if (GetPhysicBodyRef() is {} bodyRef)
                    bodyRef.Collidable.Continuity = _continuous;
            }
        }

        internal BodyReference? GetPhysicBodyRef()
        {
            return ContainerData?.BodyReference;
        }

        public void ApplyImpulse(Vector3 impulse, Vector3 impulseOffset)
        {
            GetPhysicBodyRef()?.ApplyImpulse(impulse.ToNumericVector(), impulseOffset.ToNumericVector());
        }
        public void ApplyAngularImpulse(Vector3 impulse)
        {
            GetPhysicBodyRef()?.ApplyAngularImpulse(impulse.ToNumericVector());
        }
        public void ApplyLinearImpulse(Vector3 impulse)
        {
            GetPhysicBodyRef()?.ApplyLinearImpulse(impulse.ToNumericVector());
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
