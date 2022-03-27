using Stride.Core.Mathematics;
using Stride.Core.Serialization;
using Stride.Engine;
using Stride.Input;

namespace CSharpIntermediate
{
    public class LoadScene : SyncScript
    {
        public UrlReference<Scene> SceneToLoad;
        public int DrawY = 20;
        public string Info = "Info text";
        public Keys KeyTopress;

        public override void Update()
        {
            DebugText.Print($"{Info}: {KeyTopress}", new Int2(20, DrawY));

            if (Input.IsKeyPressed(KeyTopress))
            {
                Content.Unload(SceneSystem.SceneInstance.RootScene);
                SceneSystem.SceneInstance.RootScene = Content.Load(SceneToLoad);
            }
        }
    }
}
