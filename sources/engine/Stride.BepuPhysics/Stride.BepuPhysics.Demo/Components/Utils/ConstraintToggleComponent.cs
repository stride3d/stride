using Stride.BepuPhysics.Components.Constraints;
using Stride.BepuPhysics.Extensions;
using Stride.Engine;
using Stride.Input;

namespace Stride.BepuPhysics.Demo.Components.Utils
{
    //[DataContract("SpawnerComponent", Inherited = true)]
    [ComponentCategory("BepuDemo - Utils")]
    public class ConstraintToggleComponent : SyncScript
    {
        public ConstraintComponentBase? Component { get; set; }


        public override void Start()
        {
        }

        public override void Update()
        {
            if (Component == null)
                return;

            if (Input.IsKeyPressed(Keys.G))
            {
                Component.Enabled = !Component.Enabled;
            }

            DebugText.Print($"G forr toggle constraint", new(Game.Window.PreferredWindowedSize.X - 500, 300));
        }
    }
}
