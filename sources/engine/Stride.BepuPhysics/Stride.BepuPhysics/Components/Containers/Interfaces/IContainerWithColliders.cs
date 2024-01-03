using Stride.BepuPhysics.Definitions;
using Stride.Engine;
using Stride.Rendering;

namespace Stride.BepuPhysics.Components.Containers.Interfaces
{
    public interface IContainerWithColliders
    {
        public ListOfColliders Colliders { get; set; }

    }
}