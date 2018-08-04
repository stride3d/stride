// Copyright (c) Xenko contributors (https://xenko.com)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Xenko.Rendering.Lights
{
    /// <summary>
    /// Interface to project a texture onto geometry.
    /// </summary>
    internal interface ITextureProjectionRenderer
    {
        ITextureProjectionShaderGroupData CreateShaderGroupData();
    }
}
