using Stride.BepuPhysics.Configurations;
using Stride.BepuPhysics.Definitions.Collisions;
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

    public abstract class ContainerComponent : EntityComponent
    {
        private int _simulationIndex = 0;
        private float _springFrequency = 30;
        private float _springDampingRatio = 3;
        private float _frictionCoefficient = 1f;
        private float _maximumRecoveryVelocity = 1000;
        private byte _colliderGroupMask = byte.MaxValue; //1111 1111 => collide with everything
        private bool _ignoreGravity = false;

        private IContactEventHandler? _contactEventHandler = null;

        /// <summary>
        /// ContainerData is the bridge to Bepu.
        /// Automatically set by processor.
        /// </summary>
        [DataMemberIgnore]
        internal ContainerData? ContainerData { get; set; }


        public int SimulationIndex
        {
            get
            {
                return _simulationIndex;
            }
            set
            {
                ContainerData?.DestroyContainer();
                _simulationIndex = value;
                ContainerData?.RebuildContainer();
            }
        }

        [DataMemberIgnore]
        public BepuSimulation? Simulation
        {
            get => ContainerData?.BepuSimulation;
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
                ContainerData?.UpdateMaterialProperties();
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
                ContainerData?.UpdateMaterialProperties();
            }
        }
        public float FrictionCoefficient
        {
            get => _frictionCoefficient;
            set
            {
                _frictionCoefficient = value;
                ContainerData?.UpdateMaterialProperties();
            }
        }
        public float MaximumRecoveryVelocity
        {
            get => _maximumRecoveryVelocity;
            set
            {
                _maximumRecoveryVelocity = value;
                ContainerData?.UpdateMaterialProperties();
            }
        }
        public byte ColliderGroupMask
        {
            get => _colliderGroupMask;
            set
            {
                _colliderGroupMask = value;
                ContainerData?.UpdateMaterialProperties();
            }
        }
        public bool IgnoreGravity
        {
            get => _ignoreGravity;
            set
            {
                if (_ignoreGravity == value)
                    return;

                _ignoreGravity = value;
                ContainerData?.UpdateMaterialProperties();
            }
        }

        public Vector3 CenterOfMass { get; internal set; } = new Vector3();       

        [DataMemberIgnore]
        public IContactEventHandler? ContactEventHandler
        {
            get => _contactEventHandler;
            set
            {
                if (ContainerData?.IsRegistered() == true)
                    ContainerData?.UnregisterContact();

                _contactEventHandler = value;
                ContainerData?.RegisterContact();
            }
        }
        
    }
}
