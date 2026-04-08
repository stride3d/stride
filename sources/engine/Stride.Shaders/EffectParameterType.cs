// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;

namespace Stride.Shaders;

/// <summary>
///   Values that identify various types of data, Textures, and Buffers that can be assigned to a Shader parameter.
/// </summary>
[DataContract]
public enum EffectParameterType : byte
{
    /// <summary>
    ///   The parameter is a <see langword="void"/> reference.
    /// </summary>
    Void = 0,

    /// <summary>
    ///   The parameter is a <strong>boolean</strong> (i.e. <see langword="bool"/>).
    /// </summary>
    Bool = 1,

    /// <summary>
    ///   The parameter is an <strong>integer</strong> (i.e. <see langword="int"/>).
    /// </summary>
    Int = 2,

    /// <summary>
    ///   The parameter is a <strong>single precision (32-bit) floating-point number</strong> (i.e. <see langword="float"/>).
    /// </summary>
    Float = 3,

    /// <summary>
    ///   The parameter is a <see langword="string"/>.
    /// </summary>
    String = 4,

    /// <summary>
    ///   The parameter is a <strong>Texture</strong>.
    /// </summary>
    Texture = 5,

    /// <summary>
    ///   The parameter is a <strong>1D Texture</strong>.
    /// </summary>
    Texture1D = 6,

    /// <summary>
    ///   The parameter is a <strong>2D Texture</strong>.
    /// </summary>
    Texture2D = 7,

    /// <summary>
    ///   The parameter is a <strong>3D Texture</strong>.
    /// </summary>
    Texture3D = 8,

    /// <summary>
    ///   The parameter is a <strong>Texture Cube</strong>.
    /// </summary>
    TextureCube = 9,

    /// <summary>
    ///   The parameter is a <strong>Sampler</strong>.
    /// </summary>
    Sampler = 10,

    /// <summary>
    ///   The parameter is a <strong>1D Sampler</strong>.
    /// </summary>
    Sampler1D = 11,

    /// <summary>
    ///   The parameter is a <strong>2D Sampler</strong>.
    /// </summary>
    Sampler2D = 12,

    /// <summary>
    ///   The parameter is a <strong>3D Sampler</strong>.
    /// </summary>
    Sampler3D = 13,

    /// <summary>
    ///   The parameter is a <strong>Cube Sampler</strong>.
    /// </summary>
    SamplerCube = 14,

    /// <summary>
    ///   The parameter is an <strong>unsigned integer</strong> (i.e. <see langword="uint"/>).
    /// </summary>
    UInt = 19,

    /// <summary>
    ///   The parameter is an <strong>8-bit unsigned integer</strong> (i.e. <see langword="double"/>).
    /// </summary>
    UInt8 = 20,

    /// <summary>
    ///   The parameter is a <strong>Buffer</strong>.
    /// </summary>
    Buffer = 25,

    /// <summary>
    ///   The parameter is a <strong>Constant Buffer</strong>.
    /// </summary>
    ConstantBuffer = 26,

    /// <summary>
    ///   The parameter is a <strong>Texture</strong>.
    /// </summary>
    TextureBuffer = 27,

    /// <summary>
    ///   The parameter is a <strong>1D Texture Array</strong>.
    /// </summary>
    Texture1DArray = 28,

    /// <summary>
    ///   The parameter is a <strong>2D Texture Array</strong>.
    /// </summary>
    Texture2DArray = 29,

    /// <summary>
    ///   The parameter is a <strong>Multi-sampled 2D Texture</strong>.
    /// </summary>
    Texture2DMultisampled = 32,

    /// <summary>
    ///   The parameter is a <strong>Multi-sampled 2D Texture Array</strong>.
    /// </summary>
    Texture2DMultisampledArray = 33,

    /// <summary>
    ///   The parameter is a <strong>Cube Texture Array</strong>.
    /// </summary>
    TextureCubeArray = 34,

    /// <summary>
    ///   The parameter is a <strong>double precision (64-bit) floating-point number</strong>.
    /// </summary>
    Double = 39,

    /// <summary>
    ///   The parameter is a <strong>1D Read-and-Write Texture</strong>.
    /// </summary>
    RWTexture1D = 40,

    /// <summary>
    ///   The parameter is an <strong>Array of 1D Read-and-Write Textures</strong>.
    /// </summary>
    RWTexture1DArray = 41,

    /// <summary>
    ///   The parameter is a <strong>2D Read-and-Write Texture</strong>.
    /// </summary>
    RWTexture2D = 42,

    /// <summary>
    ///   The parameter is an <strong>Array of 2D Read-and-Write Textures</strong>.
    /// </summary>
    RWTexture2DArray = 43,

    /// <summary>
    ///   The parameter is a <strong>3D Read-and-Write Texture</strong>.
    /// </summary>
    RWTexture3D = 44,

    /// <summary>
    ///   The parameter is a <strong>Read-and-Write Buffer</strong>.
    /// </summary>
    RWBuffer = 45,

    /// <summary>
    ///   The parameter is a <strong>Byte-Address Buffer</strong>.
    /// </summary>
    ByteAddressBuffer = 46,

    /// <summary>
    ///   The parameter is a <strong>Read-and-Write Byte-Address Buffer</strong>.
    /// </summary>
    RWByteAddressBuffer = 47,

    /// <summary>
    ///   The parameter is a <strong>Structured Buffer</strong>.
    /// </summary>
    StructuredBuffer = 48,

    /// <summary>
    ///   The parameter is a <strong>Read-and-Write Structured Buffer</strong>.
    /// </summary>
    RWStructuredBuffer = 49,

    /// <summary>
    ///   The parameter is an <strong>Append Structured Buffer</strong>.
    /// </summary>
    AppendStructuredBuffer = 50,

    /// <summary>
    ///   The parameter is a <strong>Consume Structured Buffer</strong>.
    /// </summary>
    ConsumeStructuredBuffer = 51
}
