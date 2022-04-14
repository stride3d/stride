using Stride.Core.Mathematics;
using Stride.Core.Serialization;
using Stride.Engine;
using Stride.Input;

namespace CSharpIntermediate
{
    public class LoadChildScene : SyncScript
    {
        // We can load a scene by name, however if the scene would be renamed, this property would not update
        //public string childSceneToLoad;

        public UrlReference<Scene> childSceneToLoad;
        private int loaded = 0;
        private Scene loadedChildScene;

        public override void Update()
        {
            DebugText.Print("Press C to load/unload child scene", new Int2(20, 60));
            if (Input.IsKeyPressed(Keys.C))
            {
                if (loadedChildScene == null)
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
                else
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
