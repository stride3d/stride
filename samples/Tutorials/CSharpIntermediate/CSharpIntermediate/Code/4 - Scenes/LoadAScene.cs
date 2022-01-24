using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stride.Core.Mathematics;
using Stride.Engine;

namespace CSharpIntermediate
{
    public class LoadAScene : SyncScript
    {
        public string SceneUrl;
        private Scene childScene;


        public override void Start()
        {
            childScene = Content.Load<Scene>(SceneUrl);
            childScene.Parent = Entity.Scene;
        }

        public override void Update()
        {
        }
    }
}
