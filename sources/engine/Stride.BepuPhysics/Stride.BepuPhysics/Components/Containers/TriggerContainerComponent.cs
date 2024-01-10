using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;
using Stride.BepuPhysics.Definitions.Contacts;
using Stride.BepuPhysics.Extensions;

namespace Stride.BepuPhysics.Components.Containers
{
    public class TriggerContainerComponent : StaticContainerComponent, IContactEventHandler
    {
        public event EventHandler<IContainer?>? ContainerEnter;
        public event EventHandler<IContainer?>? ContainerLeave;

        public TriggerContainerComponent()
        {
            ContactEventHandler = this;
        }

        void IContactEventHandler.OnStoppedTouching<TManifold>(CollidableReference eventSource, CollidablePair pair, ref TManifold contactManifold, int contactIndex)
        {
            var containerA = pair.A.GetContainerFromCollidable(Simulation);
            var containerB = pair.B.GetContainerFromCollidable(Simulation);
            ContainerLeave?.Invoke(this, containerA is TriggerContainerComponent ? containerB : containerA);
        }
        void IContactEventHandler.OnStartedTouching<TManifold>(CollidableReference eventSource, CollidablePair pair, ref TManifold contactManifold, int contactIndex)
        {
            var containerA = pair.A.GetContainerFromCollidable(Simulation);
            var containerB = pair.B.GetContainerFromCollidable(Simulation);
            ContainerEnter?.Invoke(this, containerA is TriggerContainerComponent ? containerB : containerA);
        }
    }
}
