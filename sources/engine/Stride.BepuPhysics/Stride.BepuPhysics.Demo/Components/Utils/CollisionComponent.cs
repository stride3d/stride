using BepuPhysics.Collidables;
using Stride.BepuPhysics.Definitions.Contacts;
using Stride.Engine;


namespace Stride.BepuPhysics.Demo.Components.Utils
{
    //[DataContract("SpawnerComponent", Inherited = true)]
    [ComponentCategory("BepuDemo - Utils")]
    public class CollisionComponent : SyncScript
    {

        private MyCustomContactEventHandler MyCustomContactEventHandler1 = new();
        private MyCustomContactEventHandler MyCustomContactEventHandler2 = new();
        private MyCustomContactEventHandler MyCustomContactEventHandler3 = new();
        private MyCustomContactEventHandler MyCustomContactEventHandler4 = new();

        public BodyComponent? Container1 { get; set; }
        public BodyComponent? Container2 { get; set; }
        public BodyComponent? Container3 { get; set; }
        public BodyComponent? Container4 { get; set; }

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
            DebugText.Print($"1 : {MyCustomContactEventHandler1.Contact}  |  2 : {MyCustomContactEventHandler2.Contact}  |  3 : {MyCustomContactEventHandler3.Contact}  |  4 : {MyCustomContactEventHandler4.Contact}", new(Game.Window.PreferredWindowedSize.X - 500, 800));
        }
    }

    public class MyCustomContactEventHandler : IContactEventHandler
    {
        public bool Contact { get; private set; } = false;
        public bool NoContactResponse => false;

        void IContactEventHandler.OnStartedTouching<TManifold>(CollidableReference eventSource, CollidableReference other, ref TManifold contactManifold, int contactIndex, BepuSimulation bepuSimulation)
        {
            Contact = true;
        }

        void IContactEventHandler.OnStoppedTouching<TManifold>(CollidableReference eventSource, CollidableReference other, ref TManifold contactManifold, int contactIndex, BepuSimulation bepuSimulation)
        {
            Contact = false;
        }

    }

}
