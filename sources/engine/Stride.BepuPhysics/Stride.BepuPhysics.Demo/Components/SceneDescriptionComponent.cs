using Stride.BepuPhysics.Extensions;
using Stride.Core.Serialization;
using Stride.Engine;
using Stride.Input;

namespace Stride.BepuPhysics.Demo.Components
{
    [ComponentCategory("BepuDemo")]
    public class SceneDescriptionComponent : SyncScript
    {

        public string Description { get; set; } = "";

        public override void Start()
        {
        }
        public override void Update()
        {
            DebugText.Print($"{Description}", new(Game.Window.PreferredWindowedSize.X - 500, 10));
        }
    }
}
