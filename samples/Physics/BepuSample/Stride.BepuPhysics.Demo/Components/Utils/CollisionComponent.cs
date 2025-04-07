// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

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

        public BodyComponent? Collidable1 { get; set; }
        public BodyComponent? Collidable2 { get; set; }
        public BodyComponent? Collidable3 { get; set; }
        public BodyComponent? Collidable4 { get; set; }

        public override void Start()
        {
            if (Collidable1 != null)
                Collidable1.ContactEventHandler = MyCustomContactEventHandler1;
            if (Collidable2 != null)
                Collidable2.ContactEventHandler = MyCustomContactEventHandler2;
            if (Collidable3 != null)
                Collidable3.ContactEventHandler = MyCustomContactEventHandler3;
            if (Collidable4 != null)
                Collidable4.ContactEventHandler = MyCustomContactEventHandler4;
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

        void IContactEventHandler.OnStartedTouching<TManifold>(CollidableComponent eventSource, CollidableComponent other, ref TManifold contactManifold, bool flippedManifold, int workerIndex, BepuSimulation bepuSimulation)
        {
            Contact = true;
        }

        void IContactEventHandler.OnStoppedTouching<TManifold>(CollidableComponent eventSource, CollidableComponent other, ref TManifold contactManifold, bool flippedManifold, int workerIndex, BepuSimulation bepuSimulation)
        {
            Contact = false;
        }
    }

}
