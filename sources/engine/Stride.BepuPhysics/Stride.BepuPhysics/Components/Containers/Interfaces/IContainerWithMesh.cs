using Stride.Rendering;

namespace Stride.BepuPhysics.Components.Containers.Interfaces
{
    public interface IContainerWithMesh : IContainer
    {
        public float Mass { get; }
        public bool Closed { get; }
        public Model Model { get; }
        int IContainer.GetAmountOfShapes => 1;
    }
}