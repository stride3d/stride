using Stride.BepuPhysics.Components.Constraints;
using Stride.BepuPhysics.Extensions;
using Stride.Engine;
using Stride.Input;

#warning This should not be part of the base API, move it to demo/sample

namespace Stride.BepuPhysics.Demo.Components.Utils
{
    //[DataContract("SpawnerComponent", Inherited = true)]
    [ComponentCategory("BepuDemo - Utils")]
    public class ConstraintEditorComponent : SyncScript
    {
        public BaseConstraintComponent? Component { get; set; }


        public override void Start()
        {
        }

        public override void Update()
        {
            if (Component == null || !(Component is BallSocketConstraintComponent))
                return;

            if (Input.IsKeyPressed(Keys.I))
            {
                ((BallSocketConstraintComponent)Component).LocalOffsetB += new Core.Mathematics.Vector3(0, 1, 0);
            }
            if (Input.IsKeyPressed(Keys.K))
            {
                ((BallSocketConstraintComponent)Component).LocalOffsetB -= new Core.Mathematics.Vector3(0, 1, 0);
            }

            DebugText.Print($"LocalOffsetB : {((BallSocketConstraintComponent)Component).LocalOffsetB} (numpad i & k)", new(Game.Window.PreferredWindowedSize.X - 500, 300));
        }
    }
}
