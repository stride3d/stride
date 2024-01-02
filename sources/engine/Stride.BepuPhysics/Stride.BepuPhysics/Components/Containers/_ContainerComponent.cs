using System.Diagnostics;
using BepuPhysics.Collidables;
using Stride.BepuPhysics.Configurations;
using Stride.BepuPhysics.Definitions;
using Stride.BepuPhysics.Definitions.Colliders;
using Stride.BepuPhysics.Definitions.Contacts;
using Stride.BepuPhysics.Extensions;
using Stride.BepuPhysics.Processors;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Engine.Design;
using Stride.Games;
using Stride.Graphics;
using Stride.Graphics.GeometricPrimitives;
using Stride.Physics;
using Stride.Rendering;
using static BepuPhysics.Collidables.CompoundBuilder;
using Mesh = BepuPhysics.Collidables.Mesh;

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
        private ushort _colliderFilterByDistanceId = 0; //0 => Feature not enabled
        private ushort _colliderFilterByDistanceX = 0; //collision occur if deltaX > 1
        private ushort _colliderFilterByDistanceY = 0; //collision occur if deltaY > 1
        private ushort _colliderFilterByDistanceZ = 0; //collision occur if deltaZ > 1

        private bool _ignoreGlobalGravity = false;

        private IContactEventHandler? _contactEventHandler = null;


        internal List<ContainerComponent> ChildsContainerComponent { get; } = new();
        internal ContainerData? ContainerData { get; set; }


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

        public byte ColliderGroupMask
        {
            get => _colliderGroupMask;
            set
            {
                _colliderGroupMask = value;
                ContainerData?.UpdateMaterialProperties();
            }
        }
        public ushort ColliderFilterByDistanceId
        {
            get => _colliderFilterByDistanceId;
            set
            {
                _colliderFilterByDistanceId = value;
                ContainerData?.UpdateMaterialProperties();
            }
        }
        public ushort ColliderFilterByDistanceX
        {
            get => _colliderFilterByDistanceX;
            set
            {
                _colliderFilterByDistanceX = value;
                ContainerData?.UpdateMaterialProperties();
            }
        }
        public ushort ColliderFilterByDistanceY
        {
            get => _colliderFilterByDistanceY;
            set
            {
                _colliderFilterByDistanceY = value;
                ContainerData?.UpdateMaterialProperties();
            }
        }
        public ushort ColliderFilterByDistanceZ
        {
            get => _colliderFilterByDistanceZ;
            set
            {
                _colliderFilterByDistanceX = value;
                ContainerData?.UpdateMaterialProperties();
            }
        }

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

        public Vector3 CenterOfMass { get; internal set; } = new Vector3();

        public ListOfColliders Colliders { get; set; } = new();
        //public ListWithOnEditCallback<ColliderBase> CollidersGen { get; set; } = new();

        public ContainerComponent()
        {
            Colliders.OnEditCallBack =
                () =>
                {
                    ContainerData?.TryUpdateContainer();
                };
        }
    }
}
