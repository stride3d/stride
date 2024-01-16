using Stride.BepuPhysics.Definitions;

namespace Stride.BepuPhysics.Components.Containers.Interfaces
{
    public interface IContainerWithColliders : IContainer
    {
        #warning I wonder if there's a way to enfore at least one collider with Manio's required attribute
        public ListOfColliders Colliders { get; set; }
        int IContainer.GetAmountOfShapes => Colliders.Count;
    }
}