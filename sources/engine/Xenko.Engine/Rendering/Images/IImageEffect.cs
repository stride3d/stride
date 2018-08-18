// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xenko.Graphics;

namespace Xenko.Rendering.Images
{
    public interface IImageEffect : IGraphicsRenderer
    {
        /// <summary>
        /// Sets an input texture
        /// </summary>
        /// <param name="slot">The slot.</param>
        /// <param name="texture">The texture.</param>
        void SetInput(int slot, Texture texture);

        /// <summary>
        /// Sets the viewport.
        /// </summary>
        /// <param name="viewport">The viewport.</param>
        void SetViewport(Viewport? viewport);

        /// <summary>
        /// Sets the render target output.
        /// </summary>
        /// <param name="view">The render target output view.</param>
        /// <exception cref="System.ArgumentNullException">view</exception>
        void SetOutput(Texture view);

        /// <summary>
        /// Sets the render target outputs.
        /// </summary>
        /// <param name="views">The render target output views.</param>
        void SetOutput(params Texture[] views);
    }
}
