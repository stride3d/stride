using System;
using BepuPhysicIntegrationTest.Integration.Components.Collisions;
using BepuPhysicIntegrationTest.Integration.Components.Containers;
using BepuPhysicIntegrationTest.Integration.Configurations;
using BepuPhysicIntegrationTest.Integration.Extensions;
using Stride.Engine;
using Stride.Input;

namespace BepuPhysicIntegrationTest.Integration.Components.Utils
{
    //[DataContract("SpawnerComponent", Inherited = true)]
    [ComponentCategory("Bepu - Utils")]
    public class CollisionComponent : SyncScript
    {

        private MyCustomCollision collisionStateHandler = new();

        public ContainerComponent? Container { get; set; }

        public override void Start()
        {
            if (Container != null && !Container.IsRegistered())
            {
                Container.RegisterContact(collisionStateHandler);
            }
        }

        public override void Update()
        {
            if (Container != null && !Container.IsRegistered())
            {
                Container.RegisterContact(collisionStateHandler);
            }

            DebugText.Print($"Contact : {collisionStateHandler.Contact}", new(BepuAndStrideExtensions.X_DEBUG_TEXT_POS, 800));
        }
    }

    public class MyCustomCollision : IContactEventHandler
    {
        public bool Contact { get; private set; } = false;

        void IContactEventHandler.OnPairCreated<TManifold>(BepuPhysics.Collidables.CollidableReference eventSource, BepuPhysics.CollisionDetection.CollidablePair pair, ref TManifold contactManifold, int workerIndex)
        {
            Console.WriteLine("pc");
        }

        void IContactEventHandler.OnPairEnded(BepuPhysics.Collidables.CollidableReference eventSource, BepuPhysics.CollisionDetection.CollidablePair pair)
        {
            Console.WriteLine("pe");
        }

        void IContactEventHandler.OnStartedTouching<TManifold>(BepuPhysics.Collidables.CollidableReference eventSource, BepuPhysics.CollisionDetection.CollidablePair pair, ref TManifold contactManifold, int workerIndex)
        {
            Contact = true;
            Console.WriteLine("stot");
        }

        void IContactEventHandler.OnStoppedTouching<TManifold>(BepuPhysics.Collidables.CollidableReference eventSource, BepuPhysics.CollisionDetection.CollidablePair pair, ref TManifold contactManifold, int workerIndex)
        {
            Contact = false;
            Console.WriteLine("stat");
        }

        void IContactEventHandler.OnContactAdded<TManifold>(BepuPhysics.Collidables.CollidableReference eventSource, BepuPhysics.CollisionDetection.CollidablePair pair, ref TManifold contactManifold, System.Numerics.Vector3 contactOffset, System.Numerics.Vector3 contactNormal, float depth, int featureId, int contactIndex, int workerIndex)
        {
            Console.WriteLine("ca");
        }

        void IContactEventHandler.OnContactRemoved<TManifold>(BepuPhysics.Collidables.CollidableReference eventSource, BepuPhysics.CollisionDetection.CollidablePair pair, ref TManifold contactManifold, int removedFeatureId, int workerIndex)
        {
            Console.WriteLine("cr");
        }

    }

}
