// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xenko.Rendering.Lights;

namespace Xenko.Rendering.Shadows
{
    /// <summary>
    /// Interface to render shadows
    /// </summary>
    public interface ILightShadowRenderer
    {
        /// <summary>
        /// Reset the state of this instance before calling Render method multiple times for different shadow map textures. See remarks.
        /// </summary>
        /// <remarks>
        /// This method allows the implementation to prepare some internal states before being rendered.
        /// </remarks>
        void Reset(RenderContext context);

        /// <summary>
        /// Test if this renderer can render this kind of light
        /// </summary>
        bool CanRenderLight(IDirectLight light);
    }
}
