// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Rendering.Compositing;
using Stride.Rendering.Images;

namespace Stride.Rendering
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
