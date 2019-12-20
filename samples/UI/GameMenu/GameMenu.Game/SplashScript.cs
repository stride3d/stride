// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Linq;
using Xenko.Core.Serialization;
using Xenko.Engine;
using Xenko.Input;

namespace GameMenu
{
    public class SplashScript : UISceneBase
    {
        public UrlReference<Scene> NextSceneUrl { get; set; }

        protected override void LoadScene()
        {
            // Allow user to resize the window with the mouse.
            Game.Window.AllowUserResizing = true;
        }

        protected override void UpdateScene()
        {
            if (Input.PointerEvents.Any(e => e.EventType == PointerEventType.Pressed))
            {
                // Next scene
                SceneSystem.SceneInstance.RootScene = Content.Load(NextSceneUrl);
                Cancel();
            }
        }
    }
}
