// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;

using Stride.Core;
using Stride.Graphics;

namespace Stride.Assets
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
            return new[] { GraphicsPlatform.Direct3D11, GraphicsPlatform.OpenGL, GraphicsPlatform.OpenGLES, };
        }
    }
}
