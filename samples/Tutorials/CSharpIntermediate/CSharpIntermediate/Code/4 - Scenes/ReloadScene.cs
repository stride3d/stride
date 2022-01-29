using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Input;

namespace CSharpIntermediate
{
    public class ReloadScene : SyncScript
    {
        public override void Update()
        {
            DebugText.Print("Press R to reload the current scene", new Int2(20, 40));
            
            if (Input.IsKeyPressed(Keys.R))
            {
                var currentScene = SceneSystem.SceneInstance.RootScene;
                Content.Unload(SceneSystem.SceneInstance.RootScene);
                // Replace "MainScene" with the location and name of the scene you want to load on restart
                //SceneSystem.SceneInstance.RootScene = Content.Load<Scene>(currentScene.);
            }
        }
    }
}
