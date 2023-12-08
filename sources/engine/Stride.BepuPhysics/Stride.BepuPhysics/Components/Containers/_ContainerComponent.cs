using Stride.BepuPhysics.Definitions.Collisions;
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

    public abstract class ContainerComponent : EntityComponent
    {
        private int? _simulationIndex = 0;

        private float _springFrequency = 30;
        private float _springDampingRatio = 3;
        private float _frictionCoefficient = 1f;
        private float _maximumRecoveryVelocity = 1000;
        private byte _colliderGroupMask = byte.MaxValue; //1111 1111 => collide with everything

        private IContactEventHandler? _contactEventHandler = null;

        public int SimulationIndex
        {
            get => _simulationIndex ?? 0;
            set
            {
                ContainerData?.DestroyContainer();
                _simulationIndex = value;
                ContainerData?.BuildOrUpdateContainer();
            }
        }

        public float SpringFrequency
        {
            get
            {
                return _springFrequency;
            }
            set
            {
                _springFrequency = value;
                if (ContainerData?.Exist == true)
                    ContainerData.BuildOrUpdateContainer();
            }
        }
        public float SpringDampingRatio
        {
            get
            {
                return _springDampingRatio;
            }
            set
            {
                _springDampingRatio = value;
                if (ContainerData?.Exist == true)
                    ContainerData.BuildOrUpdateContainer();
            }
        }
        public float FrictionCoefficient
        {
            get => _frictionCoefficient;
            set
            {
                _frictionCoefficient = value;
                if (ContainerData?.Exist == true)
                    ContainerData.BuildOrUpdateContainer();
            }
        }
        public float MaximumRecoveryVelocity
        {
            get => _maximumRecoveryVelocity;
            set
            {
                _maximumRecoveryVelocity = value;
                if (ContainerData?.Exist == true)
                    ContainerData.BuildOrUpdateContainer();
            }
        }
        public byte ColliderGroupMask
        {
            get => _colliderGroupMask;
            set
            {
                _colliderGroupMask = value;
                if (ContainerData?.Exist == true)
                    ContainerData.BuildOrUpdateContainer();
            }
        }

        internal bool RegisterContact()
        {
            if (ContainerData?.Exist != true || ContactEventHandler == null)
                return false;

            if (ContainerData.isStatic)
                ContainerData.BepuSimulation.ContactEvents.Register(ContainerData.SHandle, ContactEventHandler);
            else
                ContainerData.BepuSimulation.ContactEvents.Register(ContainerData.BHandle, ContactEventHandler);

            return true;
        }
        internal bool UnregisterContact()
        {
            if (ContainerData?.Exist != true)
                return false;

            if (ContainerData.isStatic)
                ContainerData.BepuSimulation.ContactEvents.Unregister(ContainerData.SHandle);
            else
                ContainerData.BepuSimulation.ContactEvents.Unregister(ContainerData.BHandle);
            return true;
        }
        public bool IsRegistered()
        {
            if (ContainerData?.Exist != true)
                return false;

            if (ContainerData.isStatic)
                return ContainerData.BepuSimulation.ContactEvents.IsListener(ContainerData.SHandle);
            else
                return ContainerData.BepuSimulation.ContactEvents.IsListener(ContainerData.BHandle);
        }

        public Vector3 CenterOfMass { get; internal set; } = new Vector3();

        /// <summary>
        /// ContainerData is the bridge to Bepu.
        /// Automatically set by processor.
        /// </summary>
        [DataMemberIgnore]
        internal ContainerData? ContainerData { get; set; }

        public IContactEventHandler? ContactEventHandler
        {
            get => _contactEventHandler;
            set
            {
                if (IsRegistered())
                    UnregisterContact();

                _contactEventHandler = value;
                RegisterContact();
            }
        }
    }
}
