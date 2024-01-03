using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;
using Stride.BepuPhysics.Components.Containers;
using Stride.BepuPhysics.Configurations;
using Stride.BepuPhysics.Extensions;

namespace Stride.BepuPhysics.Definitions.Contacts
{
    public class TriggerContactEventHandler : IContactEventHandler
    {
        private readonly Func<BepuSimulation?> _bepuSimulation;
        private readonly Action<IContainer?> _enterCallback;
        private readonly Action<IContainer?> _leaveCallback;

        public TriggerContactEventHandler(Func<BepuSimulation?> bepuSimulation, Action<IContainer?> enterCallback, Action<IContainer?> leaveCallback)
        {
            _bepuSimulation = bepuSimulation;
            _enterCallback = enterCallback;
            _leaveCallback = leaveCallback;
        }

        void IContactEventHandler.OnStoppedTouching<TManifold>(CollidableReference eventSource, CollidablePair pair, ref TManifold contactManifold, int contactIndex)
        {
            var sim = _bepuSimulation();
            var containerA = pair.A.GetContainerFromCollidable(sim);
            var containerB = pair.B.GetContainerFromCollidable(sim);
            _leaveCallback?.Invoke(containerA is TriggerContainerComponent ? containerB : containerA);
        }
        void IContactEventHandler.OnStartedTouching<TManifold>(CollidableReference eventSource, CollidablePair pair, ref TManifold contactManifold, int contactIndex)
        {
            var sim = _bepuSimulation();
            var containerA = pair.A.GetContainerFromCollidable(sim);
            var containerB = pair.B.GetContainerFromCollidable(sim);
            _enterCallback?.Invoke(containerA is TriggerContainerComponent ? containerB : containerA);
        }

    }

}
