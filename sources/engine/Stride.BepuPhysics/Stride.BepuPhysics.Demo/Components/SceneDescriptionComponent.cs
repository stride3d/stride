using Stride.Engine;

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
            DebugText.Print($"{Description}", new(Game.Window.PreferredWindowedSize.X - 900, 10));
        }
    }
}
