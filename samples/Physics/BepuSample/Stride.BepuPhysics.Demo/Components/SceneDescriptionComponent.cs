// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Engine;

namespace Stride.BepuPhysics.Demo.Components
{
    [ComponentCategory("BepuDemo")]
    public class SceneDescriptionComponent : SyncScript
    {

        public string Description { get; set; } = "";

        public override void Start()
        {
        }
        public override void Update()
        {
            DebugText.Print($"{Description}", new(Game.Window.PreferredWindowedSize.X - 900, 10));
        }
    }
}
