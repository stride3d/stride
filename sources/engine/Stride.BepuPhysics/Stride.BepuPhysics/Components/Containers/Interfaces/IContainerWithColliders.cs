using Stride.BepuPhysics.Definitions;

namespace Stride.BepuPhysics.Components.Containers.Interfaces
{
    public interface IContainerWithColliders : IContainer
    {
        public ListOfColliders Colliders { get; set; }

    }
}