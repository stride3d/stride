using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;
using Stride.BepuPhysics.Components.Character;
using Stride.BepuPhysics.Components.Containers;
using Stride.BepuPhysics.Configurations;
using Stride.BepuPhysics.Extensions;
using Stride.Core.Extensions;

namespace Stride.BepuPhysics.Definitions.Contacts
{
    public class CharacterContactEventHandler : IContactEventHandler
    {
        private readonly BepuCharacterComponent _characterComponent;

        /// <summary> Order is not guaranteed and may change at any moment </summary>
        public List<(IContainer Source, Contact Contact)> Contacts { get; } = new();

        public CharacterContactEventHandler(BepuCharacterComponent characterComponent)
        {
            _characterComponent = characterComponent;
        }

        void IContactEventHandler.OnStoppedTouching<TManifold>(CollidableReference eventSource, CollidablePair pair, ref TManifold contactManifold, int contactIndex)
        {
            var sim = _characterComponent.BepuSimulation;
            if (sim == null)
            {
                Contacts.Clear();
                return;
            }

            var containerA = pair.A.GetContainerFromCollidable(sim);
            var containerB = pair.B.GetContainerFromCollidable(sim);
            if (containerA == null || containerB == null)
            {
                return;
            }
            var otherContainer = _characterComponent.CharacterBody == containerA ? containerB : containerA;
            for (int i = Contacts.Count - 1; i >= 0; i--)
            {
                if (Contacts[i].Source == otherContainer)
                    Contacts.SwapRemoveAt(i);
            }
        }

        void IContactEventHandler.OnStartedTouching<TManifold>(CollidableReference eventSource, CollidablePair pair, ref TManifold contactManifold, int contactIndex)
        {
            var sim = _characterComponent.BepuSimulation;
            if (sim == null)
                throw new InvalidOperationException("Received new contacts in a container that's not part of the simulation");

            var containerA = pair.A.GetContainerFromCollidable(sim);
            var containerB = pair.B.GetContainerFromCollidable(sim);
            if (containerA == null || containerB == null)
            {
                return;
            }
            var otherContainer = _characterComponent.CharacterBody == containerA ? containerB : containerA;

            contactManifold.GetContact(contactIndex, out var contact);
            contact.Offset = contact.Offset + containerA.Entity.Transform.GetWorldPos().ToNumericVector() + containerA.CenterOfMass.ToNumericVector();
            Contacts.Add((otherContainer, contact));
        }
    }
}