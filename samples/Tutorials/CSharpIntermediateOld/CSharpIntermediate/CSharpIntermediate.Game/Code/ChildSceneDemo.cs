using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Input;

namespace CSharpIntermediate.Code
{
    public class ChildSceneDemo : SyncScript
    {
        private Scene rootScene;
        private Scene sceneA;

        public override void Start()
        {
            rootScene = SceneSystem.SceneInstance.RootScene;
        }

        public override void Update()
        {
            DebugText.Print("Press 1 to load or remove child scene A", new Int2(300, 180));
            if (Input.IsKeyPressed(Keys.D1))
            {
                if (sceneA == null)
                {
                    sceneA = Content.Load<Scene>("Scenes/ChildScenes/ChildSceneA");
                    sceneA.Parent = rootScene;
                }
                else
                {
                    rootScene.Children.Remove(sceneA);
                    sceneA = null;
                }
            }

            DebugText.Print("Press 2 to load ChildSceneB and set the previous scene as parent", new Int2(300, 200));
            if (Input.IsKeyPressed(Keys.D2))
            {
                if (Entity.Scene.Children.Count == 0)
                {
                    var sceneB = Content.Load<Scene>("Scenes/ChildScenes/ChildSceneB");
                    var sceneC = Content.Load<Scene>("Scenes/ChildScenes/ChildSceneC");
                    sceneB.Offset = new Vector3(-2, 0, 0);
                    sceneC.Offset = new Vector3(2, 0, 3);

                    sceneB.Parent = Entity.Scene;
                    sceneC.Parent = sceneB;
                }
                else
                {
                    var sceneB = Entity.Scene.Children[0];
                    Entity.Scene.Children.Remove(sceneB);
                    sceneB = null;
                }
            }
        }

        public override void Cancel()
        {
            Content.Unload(sceneA);
        }
    }
}
