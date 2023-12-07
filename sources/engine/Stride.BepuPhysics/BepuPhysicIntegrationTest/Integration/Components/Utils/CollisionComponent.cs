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

        private MyCustomContactEventHandler MyCustomContactEventHandler1 = new();
        private MyCustomContactEventHandler MyCustomContactEventHandler2 = new();
        private MyCustomContactEventHandler MyCustomContactEventHandler3 = new();
        private MyCustomContactEventHandler MyCustomContactEventHandler4 = new();

        public ContainerComponent? Container1 { get; set; }
        public ContainerComponent? Container2 { get; set; }
        public ContainerComponent? Container3 { get; set; }
        public ContainerComponent? Container4 { get; set; }

        public override void Start()
        {
            if (Container1 != null)
                Container1.ContactEventHandler = MyCustomContactEventHandler1;
            if (Container2 != null)
                Container2.ContactEventHandler = MyCustomContactEventHandler2;
            if (Container3 != null)
                Container3.ContactEventHandler = MyCustomContactEventHandler3;
            if (Container4 != null)
                Container4.ContactEventHandler = MyCustomContactEventHandler4;
        }

        public override void Update()
        {
            DebugText.Print($"1 : {MyCustomContactEventHandler1.Contact}  |  2 : {MyCustomContactEventHandler2.Contact}  |  3 : {MyCustomContactEventHandler3.Contact}  |  4 : {MyCustomContactEventHandler4.Contact}", new(BepuAndStrideExtensions.X_DEBUG_TEXT_POS, 800));
        }
    }

    public class MyCustomContactEventHandler : IContactEventHandler
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
