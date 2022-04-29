// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
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
        public Keys KeyToPress;

        public override void Update()
        {
            DebugText.Print($"{Info}: {KeyToPress}", new Int2(20, DrawY));

            if (Input.IsKeyPressed(KeyToPress))
            {
                Content.Unload(SceneSystem.SceneInstance.RootScene);
                SceneSystem.SceneInstance.RootScene = Content.Load(SceneToLoad);
            }
        }
    }
}
