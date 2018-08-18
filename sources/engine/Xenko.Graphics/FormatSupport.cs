// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

namespace Xenko.Graphics
{
    /// <summary>
    /// <p>Which resources are supported for a given format and given device (see <strong><see cref="SharpDX.Direct3D11.Device.CheckFormatSupport"/></strong> and <strong><see cref="SharpDX.Direct3D11.Device.CheckFeatureSupport"/></strong>).</p>
    /// </summary>
    [Flags]
    public enum FormatSupport : int
    {
        /// <summary>
        /// No documentation.
        /// </summary>
        Buffer = unchecked((int)1),

        /// <summary>
        /// No documentation.
        /// </summary>
        InputAssemblyVertexBuffer = unchecked((int)2),

        /// <summary>
        /// No documentation.
        /// </summary>
        InputAssemblyIndexBuffer = unchecked((int)4),

        /// <summary>
        /// No documentation.
        /// </summary>
        StreamOutputBuffer = unchecked((int)8),

        /// <summary>
        /// No documentation.
        /// </summary>
        Texture1D = unchecked((int)16),

        /// <summary>
        /// No documentation.
        /// </summary>
        Texture2D = unchecked((int)32),

        /// <summary>
        /// No documentation.
        /// </summary>
        Texture3D = unchecked((int)64),

        /// <summary>
        /// No documentation.
        /// </summary>
        TextureCube = unchecked((int)128),

        /// <summary>
        /// No documentation.
        /// </summary>
        ShaderLoad = unchecked((int)256),

        /// <summary>
        /// No documentation.
        /// </summary>
        ShaderSample = unchecked((int)512),

        /// <summary>
        /// No documentation.
        /// </summary>
        ShaderSampleComparison = unchecked((int)1024),

        /// <summary>
        /// No documentation.
        /// </summary>
        ShaderSampleMonoText = unchecked((int)2048),

        /// <summary>
        /// No documentation.
        /// </summary>
        Mip = unchecked((int)4096),

        /// <summary>
        /// No documentation.
        /// </summary>
        MipAutogen = unchecked((int)8192),

        /// <summary>
        /// No documentation.
        /// </summary>
        RenderTarget = unchecked((int)16384),

        /// <summary>
        /// No documentation.
        /// </summary>
        Blendable = unchecked((int)32768),

        /// <summary>
        /// No documentation.
        /// </summary>
        DepthStencil = unchecked((int)65536),

        /// <summary>
        /// No documentation.
        /// </summary>
        CpuLockable = unchecked((int)131072),

        /// <summary>
        /// No documentation.
        /// </summary>
        MultisampleResolve = unchecked((int)262144),

        /// <summary>
        /// No documentation.
        /// </summary>
        Display = unchecked((int)524288),

        /// <summary>
        /// No documentation.
        /// </summary>
        CastWithinBitLayout = unchecked((int)1048576),

        /// <summary>
        /// No documentation.
        /// </summary>
        MultisampleRendertarget = unchecked((int)2097152),

        /// <summary>
        /// No documentation.
        /// </summary>
        MultisampleLoad = unchecked((int)4194304),

        /// <summary>
        /// No documentation.
        /// </summary>
        ShaderGather = unchecked((int)8388608),

        /// <summary>
        /// No documentation.
        /// </summary>
        BackBufferCast = unchecked((int)16777216),

        /// <summary>
        /// No documentation.
        /// </summary>
        TypedUnorderedAccessView = unchecked((int)33554432),

        /// <summary>
        /// No documentation.
        /// </summary>
        ShaderGatherComparison = unchecked((int)67108864),

        DecoderOutput = 134217728,

        VideoProcessorOutput = 268435456,

        VideoProcessorInput = 536870912,

        VideoEncoder = 1073741824,

        /// <summary>
        /// None.
        /// </summary>
        None = unchecked((int)0),
    }
}
