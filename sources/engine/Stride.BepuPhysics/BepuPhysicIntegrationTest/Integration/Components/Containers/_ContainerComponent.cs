using BepuPhysicIntegrationTest.Integration.Processors;
using BepuPhysics.Constraints;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Engine.Design;

namespace BepuPhysicIntegrationTest.Integration.Components.Containers
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


        public Vector3 CenterOfMass { get; internal set; } = new Vector3();

        /// <summary>
        /// ContainerData is the bridge to Bepu.
        /// Automatically set by processor.
        /// </summary>
        [DataMemberIgnore]
        internal ContainerData? ContainerData { get; set; }
    }
}
