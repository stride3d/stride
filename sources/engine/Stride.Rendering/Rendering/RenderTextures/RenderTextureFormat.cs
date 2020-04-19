// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core;
using Stride.Graphics;

namespace Stride.Rendering.RenderTextures
{
    /// <summary>
    /// Describes the format of a <see cref="RenderTextureDescriptor"/>.
    /// </summary>
    public enum RenderTextureFormat
    {
        /// <summary>
        /// The rendering target is a 32bits bits targets (4 x 16 bits half floats per RGBA component).
        /// </summary>
        /// <userdoc>The rendering target is a 32bits bits targets (4 x 16 bits half floats per RGBA component).</userdoc>
        [Display("Low Dynamic Range (RGBA8)")]
        LDR = PixelFormat.R8G8B8A8_UNorm,

        /// <summary>
        /// The rendering target is a floating point 64 bits targets (4 x 16 bits half floats per RGBA component).
        /// </summary>
        /// <userdoc>The rendering target is a floating point 64 bits targets (4 x 16 bits half floats per RGBA component).</userdoc>
        [Display("High Dynamic Range (RGBA16 float)")]
        HDR = PixelFormat.R16G16B16A16_Float,
    }
}
