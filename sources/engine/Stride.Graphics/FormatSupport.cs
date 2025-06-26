// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

namespace Stride.Graphics;

[Flags]
public enum FormatSupport : int
{
    /// <summary>
    ///   Flags specifying which resources and features are supported for a given pixel format for a graphics device.
    /// </summary>
    /// <remarks>
    ///   For more information, see <see cref="GraphicsDevice.Features"/>.
    /// </remarks>
    None = 0,

    Buffer = 1,
    InputAssemblyVertexBuffer = 2,
    InputAssemblyIndexBuffer = 4,
    StreamOutputBuffer = 8,
    Texture1D = 16,
    Texture2D = 32,
    Texture3D = 64,
    TextureCube = 128,
    ShaderLoad = 256,
    ShaderSample = 512,
    ShaderSampleComparison = 1024,
    ShaderSampleMonoText = 2048,
    Mip = 4096,
    MipAutogen = 8192,
    RenderTarget = 16384,
    Blendable = 32768,
    DepthStencil = 65536,
    CpuLockable = 131072,
    MultisampleResolve = 262144,
    Display = 524288,
    CastWithinBitLayout = 1048576,
    MultisampleRendertarget = 2097152,
    MultisampleLoad = 4194304,
    ShaderGather = 8388608,
    BackBufferCast = 16777216,
    TypedUnorderedAccessView = 33554432,
    ShaderGatherComparison = 67108864,
    DecoderOutput = 134217728,
    VideoProcessorOutput = 268435456,
    VideoProcessorInput = 536870912,
    VideoEncoder = 1073741824
}
