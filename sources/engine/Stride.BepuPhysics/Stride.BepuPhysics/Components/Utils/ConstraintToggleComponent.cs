using Stride.BepuPhysics.Components.Constraints;
using Stride.BepuPhysics.Extensions;
using Stride.Engine;
using Stride.Input;

#warning This should not be part of the base API, move it to demo/sample

namespace Stride.BepuPhysics.Components.Utils
{
    //[DataContract("SpawnerComponent", Inherited = true)]
    [ComponentCategory("Bepu - Utils")]
    public class ConstraintToggleComponent : SyncScript
    {
        public BaseConstraintComponent? Component { get; set; }


        public override void Start()
        {
        }

        public override void Update()
        {
            if (Component == null )
                return;

            if (Input.IsKeyPressed(Keys.G))
            {
                Component.Enabled = !Component.Enabled;
            }

            DebugText.Print($"G forr toggle constraint", new(BepuAndStrideExtensions.X_DEBUG_TEXT_POS, 300));
        }
    }
}
