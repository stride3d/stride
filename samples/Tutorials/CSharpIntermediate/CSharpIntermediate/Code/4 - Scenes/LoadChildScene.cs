using Stride.Core.Mathematics;
using Stride.Core.Serialization;
using Stride.Engine;
using Stride.Input;

namespace CSharpIntermediate
{
    public class LoadChildScene : SyncScript
    {
        //public string childSceneToLoad;
        public UrlReference<Scene> childSceneToLoad;
        private int loaded = 0;
        private Scene loadedChildScene;

        public override void Update()
        {
            DebugText.Print("Press C to load/unload child scene", new Int2(20, 60));
            if (loadedChildScene == null)
            {
                if (Input.IsKeyPressed(Keys.C))
                {
                    // loadedChildScene = Content.Load<Scene>(childSceneToLoad);
                    // Or
                    loadedChildScene = Content.Load(childSceneToLoad);
                    loadedChildScene.Offset = new Vector3(0, 0.5f * loaded, 0);
                    loaded++;

                    // Entity.Scene.Children.Add(loadedChildScene);
                    // Or 
                    loadedChildScene.Parent = Entity.Scene;
                }
            }
            else
            {
                if (Input.IsKeyPressed(Keys.C))
                {
                    // Entity.Scene.Children.Remove(loadedChildScene);
                    // Or
                    loadedChildScene.Parent = null;

                    Content.Unload(loadedChildScene);
                    loadedChildScene = null;
                }
            }
        }
    }
}
