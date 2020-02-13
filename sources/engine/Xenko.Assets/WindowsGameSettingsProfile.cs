// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;

using Xenko.Core;
using Xenko.Graphics;

namespace Xenko.Assets
{
    /// <summary>
    /// Base settings for Windows profile.
    /// </summary>
    [DataContract("WindowsGameSettingsProfile")]
    public class WindowsGameSettingsProfile : GameSettingsProfileBase
    {
        public WindowsGameSettingsProfile()
        {
            GraphicsPlatform = GraphicsPlatform.Direct3D11;
        }

        public override IEnumerable<GraphicsPlatform> GetSupportedGraphicsPlatforms()
        {
            return new[] { GraphicsPlatform.Vulkan, GraphicsPlatform.Direct3D11, GraphicsPlatform.OpenGL, GraphicsPlatform.OpenGLES, };
        }
    }
}
