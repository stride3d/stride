using Stride.BepuPhysics.Processors;
using Stride.Core.Mathematics;
using Stride.Engine;

namespace Stride.BepuPhysics.Components.Containers.Interfaces
{
    public interface IContainer
    {
        internal List<ContainerComponent> ChildsContainerComponent { get; }
        internal ContainerData? ContainerData { get; }

        public Entity Entity { get; }
        public Vector3 CenterOfMass { get; }
        public int SimulationIndex { get; set; }
        public float SpringFrequency { get; set; }
        public float SpringDampingRatio { get; set; }
        public float FrictionCoefficient { get; set; }
        public float MaximumRecoveryVelocity { get; set; }
        public byte ColliderGroupMask { get; set; }
        public ushort ColliderFilterByDistanceId { get; set; }
        public ushort ColliderFilterByDistanceX { get; set; }
        public ushort ColliderFilterByDistanceY { get; set; }
        public ushort ColliderFilterByDistanceZ { get; set; }
        public bool IgnoreGlobalGravity { get; set; }

    }
}