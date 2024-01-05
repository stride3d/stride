using Stride.Rendering;

namespace Stride.BepuPhysics.Components.Containers.Interfaces
{
    public interface IContainerWithMesh
    {
        public float Mass { get; }
        public bool Closed { get; }
        public Model? Model { get; }
    }
}