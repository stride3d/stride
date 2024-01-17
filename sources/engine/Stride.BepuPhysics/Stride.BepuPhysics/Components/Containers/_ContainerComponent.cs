using Stride.BepuPhysics.Components.Containers.Interfaces;
using Stride.BepuPhysics.Configurations;
using Stride.BepuPhysics.Definitions.Colliders;
using Stride.BepuPhysics.Definitions.Contacts;
using Stride.BepuPhysics.Processors;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Engine.Design;

namespace Stride.BepuPhysics.Components.Containers
{
    [DataContract(Inherited = true)]
    [DefaultEntityComponentProcessor(typeof(ContainerProcessor), ExecutionMode = ExecutionMode.Runtime)] //ExecutionMode.Editor | ExecutionMode.Runtime //Lol, don't do that
    [ComponentCategory("Bepu - Containers")]
    public abstract class ContainerComponent : StartupScript, IContainer
    {
        ContainerData? IContainer.ContainerData => ContainerData;

        private int _simulationIndex = 0;
        private float _springFrequency = 30;
        private float _springDampingRatio = 3;
        private float _frictionCoefficient = 1f;
        private float _maximumRecoveryVelocity = 1000;

        private CollisionMask _collisionMask = CollisionMask.Everything;
        private FilterByDistance _filterByDistance;

        private IContactEventHandler? _contactEventHandler = null;


        internal ContainerData? ContainerData { get; set; }


        [DataMemberIgnore]
        public IContactEventHandler? ContactEventHandler
        {
            get => _contactEventHandler;
            protected set
            {
                if (ContainerData?.IsRegistered() == true)
                    ContainerData?.UnregisterContact();

                _contactEventHandler = value;
                ContainerData?.RegisterContact();
            }
        }

        [DataMemberIgnore]
        public BepuSimulation? Simulation => ContainerData?.BepuSimulation;

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

        public CollisionMask CollisionMask
        {
            get => _collisionMask;
            set
            {
                _collisionMask = value;
                ContainerData?.UpdateMaterialProperties();
            }
        }

        public FilterByDistance FilterByDistance
        {
            get => _filterByDistance;
            set
            {
                _filterByDistance = value;
                ContainerData?.UpdateMaterialProperties();
            }
        }

        public Vector3 CenterOfMass { get; internal set; } = new Vector3();

    }
}
