using Stride.Engine;
using Stride.Rendering;

namespace Stride.BepuPhysics.Components.Containers
{
    public interface IMeshContainerComponent
    {
        public Entity Entity { get; }
        public Model? Model { get; }
        public float Mass { get; }
        public bool Closed { get; }
    }
}