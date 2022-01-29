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
    public class SwitchScene : SyncScript
    {
        public Scene SceneToLoad;

        public override void Start()
        {

        }

        public override void Update()
        {
            DebugText.Print("Press S to Switch child scene", new Int2(20, 60));
            

            if (Input.IsKeyPressed(Keys.L))
            {
                SceneSystem.SceneInstance.RootScene = SceneToLoad;
            }
        }
    }
}
