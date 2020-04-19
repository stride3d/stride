// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

namespace Stride.Graphics
{
    [Flags]
    public enum TextureFlags
    {
        /// <summary>
        /// No option.
        /// </summary>
        None = 0,

        /// <summary>
        /// A texture usable as a ShaderResourceView.
        /// </summary>
        ShaderResource = 1,

        /// <summary>
        /// A texture usable as render target.
        /// </summary>
        RenderTarget = 2,   // TODO: This naming is ambiguous. A render target can be a depth, stencil or color buffer. But we use "RenderTarget" synonymously with "ColorTarget".

        /// <summary>
        /// A texture usable as an unordered access buffer.
        /// </summary>
        UnorderedAccess = 4,

        /// <summary>
        /// A texture usable as a depth stencil buffer.
        /// </summary>
        DepthStencil = 8,

        /// <summary>
        /// A texture usable as a readonly depth stencil buffer.
        /// </summary>
        DepthStencilReadOnly = 8 + Texture.DepthStencilReadOnlyFlags,
    }
}
