using System.Diagnostics;
using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;
using Stride.BepuPhysics.Components.Containers.Interfaces;
using Stride.BepuPhysics.Definitions.Contacts;
using Stride.BepuPhysics.Extensions;
using Stride.Core;

namespace Stride.BepuPhysics.Components.Containers
{
    public class TriggerContainerComponent : StaticContainerComponent, IContactEventHandler
    {
        public event EventHandler<IContainer>? ContainerEnter;
        public event EventHandler<IContainer>? ContainerLeave;

        [DataMemberIgnore]
        public new IContactEventHandler? ContactEventHandler
        {
            get => base.ContactEventHandler;
            private set => base.ContactEventHandler = value;
        }

        public TriggerContainerComponent()
        {
            ContactEventHandler = this;
        }

        void IContactEventHandler.OnStartedTouching<TManifold>(CollidableReference eventSource, CollidablePair pair, ref TManifold contactManifold, int contactIndex)
        {
            Debug.Assert(Simulation is not null);
            var containerA = pair.A.GetContainerFromCollidable(Simulation);
            var containerB = pair.B.GetContainerFromCollidable(Simulation);
            var other = containerA is TriggerContainerComponent ? containerB : containerA;
            ContainerEnter?.Invoke(this, other);
        }
        void IContactEventHandler.OnStoppedTouching<TManifold>(CollidableReference eventSource, CollidablePair pair, ref TManifold contactManifold, int contactIndex)
        {
            Debug.Assert(Simulation is not null);
            var containerA = pair.A.GetContainerFromCollidable(Simulation);
            var containerB = pair.B.GetContainerFromCollidable(Simulation);
            var other = containerA is TriggerContainerComponent ? containerB : containerA;
            ContainerLeave?.Invoke(this, other);
        }
    }
}
