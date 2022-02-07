using Stride.Core.Mathematics;
using Stride.Core.Serialization;
using Stride.Engine;
using Stride.Input;

namespace CSharpIntermediate
{
    public class LoadScene : SyncScript
    {
        public UrlReference<Scene> SceneToLoad;

        public override void Update()
        {
            DebugText.Print("Press L to load a scene", new Int2(20, 20));

            if (Input.IsKeyPressed(Keys.L))
            {
                Content.Unload(SceneSystem.SceneInstance.RootScene);
                SceneSystem.SceneInstance.RootScene = Content.Load(SceneToLoad);
            }
        }
    }
}
