using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Input;

namespace CSharpIntermediate
{
    public class LoadChildScene : SyncScript
    {
        public string SceneUrl;
        private Scene childScene;
        private int loaded = 1;

        public override void Start()
        {

        }

        public override void Update()
        {
            if (childScene == null)
                DebugText.Print("Press L to load child scene", new Int2(20, 20));
            else
                DebugText.Print("Press U to unload child scene", new Int2(20, 20));

            if (Input.IsKeyPressed(Keys.L))
            {
                childScene = Content.Load<Scene>(SceneUrl);
                childScene.Offset = new Vector3(0, 0.5f * loaded, 0);
                childScene.Parent = Entity.Scene;
                loaded++;
            }

            if (Input.IsKeyPressed(Keys.U))
            {
                childScene.Parent = null;
                childScene.Dispose();
                childScene = null;
            }
        }
    }
}
