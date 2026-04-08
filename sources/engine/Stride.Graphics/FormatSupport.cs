// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

namespace Stride.Graphics;

/// <summary>
///   Flags specifying which resources and features are supported for a given <see cref="PixelFormat"/>
///   for a <see cref="GraphicsDevice"/>.
/// </summary>
/// <remarks>
///   For more information, see <see cref="GraphicsDevice.Features"/>.
/// </remarks>
[Flags]
public enum FormatSupport : int
{
    None = 0,

    /// <summary>
    ///   The format can be used as a Buffer resource.
    /// </summary>
    Buffer = 1,

    /// <summary>
    ///   The format can be used as a Vertex Buffer in input assembly.
    /// </summary>
    InputAssemblyVertexBuffer = 2,

    /// <summary>
    ///   The format can be used as an Index Buffer in input assembly.
    /// </summary>
    InputAssemblyIndexBuffer = 4,

    /// <summary>
    ///   The format can be used as a Stream-output Buffer.
    /// </summary>
    StreamOutputBuffer = 8,

    /// <summary>
    ///   The format can be used as a 1D Texture.
    /// </summary>
    Texture1D = 16,

    /// <summary>
    ///   The format can be used as a 2D Texture.
    /// </summary>
    Texture2D = 32,

    /// <summary>
    ///   The format can be used as a 3D Texture.
    /// </summary>
    Texture3D = 64,

    /// <summary>
    ///   The format can be used as a Cube Texture.
    /// </summary>
    TextureCube = 128,

    /// <summary>
    ///   The format can be loaded in a Shader.
    /// </summary>
    ShaderLoad = 256,

    /// <summary>
    ///   The format can be sampled in a Shader.
    /// </summary>
    ShaderSample = 512,

    /// <summary>
    ///   The format can be used for comparison sampling in a Shader.
    /// </summary>
    ShaderSampleComparison = 1024,

    /// <summary>
    ///   The format can be used for monochrome text sampling in a Shader.
    /// </summary>
    ShaderSampleMonoText = 2048,

    /// <summary>
    ///   The format supports mipmaps.
    /// </summary>
    Mip = 4096,

    /// <summary>
    ///   The format supports automatic mipmap generation.
    /// </summary>
    MipAutogen = 8192,

    /// <summary>
    ///   The format can be used as a Render Target.
    /// </summary>
    RenderTarget = 16384,

    /// <summary>
    ///   The format supports blending as a Render Target.
    /// </summary>
    Blendable = 32768,

    /// <summary>
    ///   The format can be used as a Depth-Stencil Buffer.
    /// </summary>
    DepthStencil = 65536,

    /// <summary>
    ///   The format can be locked for CPU access.
    /// </summary>
    CpuLockable = 131072,

    /// <summary>
    ///   The format supports multisample resolve operations.
    /// </summary>
    MultisampleResolve = 262144,

    /// <summary>
    ///   The format can be used for display scan-out (to present to the screen).
    /// </summary>
    Display = 524288,

    /// <summary>
    ///   The format can be cast within a bit layout.
    /// </summary>
    CastWithinBitLayout = 1048576,

    /// <summary>
    ///   The format can be used as a multisample Render Target.
    /// </summary>
    MultisampleRendertarget = 2097152,

    /// <summary>
    ///   The format supports multisample load operations.
    /// </summary>
    MultisampleLoad = 4194304,

    /// <summary>
    ///   The format supports gather operations in a Shader.
    /// </summary>
    ShaderGather = 8388608,

    /// <summary>
    ///   The format can be cast to a Back-Buffer.
    /// </summary>
    BackBufferCast = 16777216,

    /// <summary>
    ///   The format supports typed Unordered Access Views.
    /// </summary>
    TypedUnorderedAccessView = 33554432,

    /// <summary>
    ///   The format supports gather comparison operations in a Shader.
    /// </summary>
    ShaderGatherComparison = 67108864,

    /// <summary>
    ///   The format can be used as a decoder output.
    /// </summary>
    DecoderOutput = 134217728,

    /// <summary>
    ///   The format can be used as a video processor output.
    /// </summary>
    VideoProcessorOutput = 268435456,

    /// <summary>
    ///   The format can be used as a video processor input.
    /// </summary>
    VideoProcessorInput = 536870912,

    /// <summary>
    ///   The format can be used as a video encoder.
    /// </summary>
    VideoEncoder = 1073741824
}
