using Stride.Engine;
using Stride.Rendering;

namespace Stride.BepuPhysics.Components.Containers
{
#warning We need to make a new type to remove the colliderList for mesh
    public interface IMeshContainerComponent
    {
        public Entity Entity { get; }
        public float Mass { get; }
        public bool Closed { get; }
        public Model? Model { get; }
    }
}