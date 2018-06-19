// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xenko.Rendering.Compositing;
using Xenko.Rendering.Images;

namespace Xenko.Rendering
{
    /// <summary>
    /// Renderer interface for a end-user <see cref="ImageEffect"/> accessible from <see cref="SceneEffectRenderer"/>. See remarks.
    /// </summary>
    /// <remarks>
    /// An <see cref="IImageEffectRenderer"/> expect an input texture on slot 0, possibly a depth texture on slot 1 and a single
    /// output.
    /// </remarks>
    public interface IImageEffectRenderer : IImageEffect
    {
    }
}
