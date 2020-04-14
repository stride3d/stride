// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;

using Xenko.Core;
using Xenko.Graphics;

namespace Xenko.Assets
{
    /// <summary>
    /// Default game settings profile. This is currently used internally only.
    /// </summary>
    [DataContract()]
    public abstract class GameSettingsProfileBase : IGameSettingsProfile
    {
        [DataMember(10)]
        public GraphicsPlatform GraphicsPlatform { get; set; }

        public abstract IEnumerable<GraphicsPlatform> GetSupportedGraphicsPlatforms();
    }
}
