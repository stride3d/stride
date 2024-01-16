using Stride.Rendering;

namespace Stride.BepuPhysics.Components.Containers.Interfaces
{
    public interface IContainerWithMesh : IContainer
    {
        public float Mass { get; }
        public bool Closed { get; }
        #warning shouldn't we enforce this to be non-null ?
        public Model? Model { get; }
        int IContainer.GetAmountOfShapes => Model == null ? 0 : 1;
    }
}