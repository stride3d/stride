using BepuPhysics;
using BepuPhysics.Collidables;
using Stride.BepuPhysics.Components.Containers.Interfaces;
using Stride.BepuPhysics.Definitions.Contacts;
using Stride.BepuPhysics.Extensions;
using Stride.BepuPhysics.Processors;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Rendering;

namespace Stride.BepuPhysics.Components.Containers
{
    public class BodyMeshContainerComponent : ContainerComponent, IBodyContainer, IContainerWithMesh
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

        private BodyReference? GetPhysicBodyRef() => ContainerData?.BodyReference;

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
            get => GetPhysicBodyRef()?.Collidable.Continuity ?? default;
            set
            {
                if (GetPhysicBodyRef() is {} bodyRef)
                    bodyRef.Collidable.Continuity = value;
            }
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

        #region WithMesh

        private float _mass = 1f;
        private bool _closed = true;
        private Model? _model;

        public float Mass
        {
            get => _mass;
            set
            {
                if (_mass != value)
                {
                    _mass = value;
                    ContainerData?.TryUpdateContainer();
                }
            }
        }
        public bool Closed
        {
            get => _closed;
            set
            {
                if (_closed != value)
                {
                    _closed = value;
                    ContainerData?.TryUpdateContainer();
                }
            }
        }
        public Model? Model
        {
            get => _model;
            set
            {
                _model = value;
                ContainerData?.TryUpdateContainer();
            }
        }

        #endregion

    }
}
