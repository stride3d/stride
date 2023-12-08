using Stride.BepuPhysics.Components.Colliders;
using Stride.BepuPhysics.Components.Containers;
using Stride.BepuPhysics.Extensions;
using Stride.Engine;
using Stride.Input;

namespace Stride.BepuPhysics.Components.Utils
{
    //[DataContract("SpawnerComponent", Inherited = true)]
    [ComponentCategory("Bepu - Utils")]
    public class ColliderEditorComponent : SyncScript
    {
        public ColliderComponent? Component { get; set; }


        public override void Start()
        {
        }

        public override void Update()
        {
            if (Component == null || !(Component is BoxColliderComponent))
                return;

            if (Input.IsKeyPressed(Keys.U))
            {
                ((BoxColliderComponent)Component).Size += new Core.Mathematics.Vector3(1, 1, 1);
                ((BoxColliderComponent)Component).Entity.Transform.Scale += new Core.Mathematics.Vector3(1, 1, 1);
            }
            if (Input.IsKeyPressed(Keys.J))
            {
                ((BoxColliderComponent)Component).Size -= new Core.Mathematics.Vector3(1, 1, 1);
                ((BoxColliderComponent)Component).Entity.Transform.Scale -= new Core.Mathematics.Vector3(1, 1, 1);
            }
            if (Input.IsKeyPressed(Keys.N))
            {
                var rr = (BodyContainerComponent?)Component.Container;
                if (rr != null)
                    rr.Kinematic = !rr.Kinematic;
            }
            DebugText.Print($"Size : {((BoxColliderComponent)Component).Size} (numpad u & j) + n for toggle kinematic", new(BepuAndStrideExtensions.X_DEBUG_TEXT_POS, 350));
        }
    }
}
