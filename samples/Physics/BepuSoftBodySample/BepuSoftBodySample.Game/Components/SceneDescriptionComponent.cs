// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Engine;

namespace BepuSoftBodySample.Game.Components
{
    /// <summary> What a scene shows, in a sentence: <see cref="SceneSelectorComponent"/> writes it under the name of the scene </summary>
    [ComponentCategory("BepuSoftBodyDemo")]
    public class SceneDescriptionComponent : SyncScript
    {
        public string Description { get; set; } = "";

        public override void Update()
        {
        }
    }
}