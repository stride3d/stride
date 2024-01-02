using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;
using Stride.BepuPhysics.Components.Containers;
using Stride.BepuPhysics.Configurations;

namespace Stride.BepuPhysics.Definitions.Contacts
{
    public class TriggerContactEventHandler : IContactEventHandler
    {
        private readonly Func<BepuSimulation?> _bepuSimulation;
        private readonly Action<ContainerComponent?> _enterCallback;
        private readonly Action<ContainerComponent?> _leaveCallback;

        public TriggerContactEventHandler(Func<BepuSimulation?> bepuSimulation, Action<ContainerComponent?> enterCallback, Action<ContainerComponent?> leaveCallback)
        {
            _bepuSimulation = bepuSimulation;
            _enterCallback = enterCallback;
            _leaveCallback = leaveCallback;
        }

        void IContactEventHandler.OnStoppedTouching<TManifold>(CollidableReference eventSource, CollidablePair pair, ref TManifold contactManifold, int contactIndex)
        {
            var containerA = GetContainerFromCollidable(pair.A, _bepuSimulation());
            var containerB = GetContainerFromCollidable(pair.B, _bepuSimulation());
            _leaveCallback?.Invoke(containerA is TriggerContainerComponent ? containerB : containerA);
        }
        void IContactEventHandler.OnStartedTouching<TManifold>(CollidableReference eventSource, CollidablePair pair, ref TManifold contactManifold, int contactIndex)
        {
            var containerA = GetContainerFromCollidable(pair.A, _bepuSimulation());
            var containerB = GetContainerFromCollidable(pair.B, _bepuSimulation());
            _enterCallback?.Invoke(containerA is TriggerContainerComponent ? containerB : containerA);
        }

        private ContainerComponent? GetContainerFromCollidable(CollidableReference collidable, BepuSimulation? sim)
        {
            if (sim != null)
                if (collidable.Mobility == CollidableMobility.Static && sim.StaticsContainers.TryGetValue(collidable.StaticHandle, out StaticContainerComponent? staticsContainer))
                {
                    return staticsContainer;
                }
                else if (collidable.Mobility != CollidableMobility.Static && sim.BodiesContainers.TryGetValue(collidable.BodyHandle, out BodyContainerComponent? bodiesContainer))
                {
                    return bodiesContainer;
                }
            throw new InvalidOperationException("Received new contacts with a container that's not part of the simulation");
        }

    }

}
