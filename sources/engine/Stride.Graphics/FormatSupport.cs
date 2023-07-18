// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

namespace Stride.Graphics
{
    /// <summary>
    ///   Flags specifying which resources and features are supported for a given pixel format for a graphics device.
    /// </summary>
    /// <remarks>
    ///   For more information, see <see cref="GraphicsDevice.Features"/>.
    /// </remarks>
    [Flags]
    public enum FormatSupport : int
    {
        None = 0,

        Buffer = unchecked(1),
        InputAssemblyVertexBuffer = unchecked(2),
        InputAssemblyIndexBuffer = unchecked(4),
        StreamOutputBuffer = unchecked(8),
        Texture1D = unchecked(16),
        Texture2D = unchecked(32),
        Texture3D = unchecked(64),
        TextureCube = unchecked(128),
        ShaderLoad = unchecked(256),
        ShaderSample = unchecked(512),
        ShaderSampleComparison = unchecked(1024),
        ShaderSampleMonoText = unchecked(2048),
        Mip = unchecked(4096),
        MipAutogen = unchecked(8192),
        RenderTarget = unchecked(16384),
        Blendable = unchecked(32768),
        DepthStencil = unchecked(65536),
        CpuLockable = unchecked(131072),
        MultisampleResolve = unchecked(262144),
        Display = unchecked(524288),
        CastWithinBitLayout = unchecked(1048576),
        MultisampleRendertarget = unchecked(2097152),
        MultisampleLoad = unchecked(4194304),
        ShaderGather = unchecked(8388608),
        BackBufferCast = unchecked(16777216),
        TypedUnorderedAccessView = unchecked(33554432),
        ShaderGatherComparison = unchecked(67108864),
        DecoderOutput = 134217728,
        VideoProcessorOutput = 268435456,
        VideoProcessorInput = 536870912,
        VideoEncoder = 1073741824
    }
}
