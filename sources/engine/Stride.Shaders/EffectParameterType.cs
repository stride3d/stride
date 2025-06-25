// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;

namespace Stride.Shaders;

[DataContract]
public enum EffectParameterType : byte
{
    /// <summary>
    /// <p>Values that identify various data, texture, and buffer types that can be assigned to a shader variable.</p>
    /// </summary>
        /// <summary>
        /// <dd> <p>The variable is a void reference.</p> </dd>
        /// </summary>
        /// <summary>
        /// <dd> <p>The variable is a boolean.</p> </dd>
        /// </summary>
        /// <summary>
        /// <dd> <p>The variable is an integer.</p> </dd>
        /// </summary>
        /// <summary>
        /// <dd> <p>The variable is a floating-point number.</p> </dd>
        /// </summary>
        /// <summary>
        /// <dd> <p>The variable is a string.</p> </dd>
        /// </summary>
        /// <summary>
        /// <dd> <p>The variable is a texture.</p> </dd>
        /// </summary>
        /// <summary>
        /// <dd> <p>The variable is a 1D texture.</p> </dd>
        /// </summary>
        /// <summary>
        /// <dd> <p>The variable is a 2D texture.</p> </dd>
        /// </summary>
        /// <summary>
        /// <dd> <p>The variable is a 3D texture.</p> </dd>
        /// </summary>
        /// <summary>
        /// <dd> <p>The variable is a texture cube.</p> </dd>
        /// </summary>
        /// <summary>
        /// <dd> <p>The variable is a sampler.</p> </dd>
        /// </summary>
        /// <summary>
        /// <dd> <p>The variable is a sampler.</p> </dd>
        /// </summary>
        /// <summary>
        /// <dd> <p>The variable is a sampler.</p> </dd>
        /// </summary>
        /// <summary>
        /// <dd> <p>The variable is a sampler.</p> </dd>
        /// </summary>
        /// <summary>
        /// <dd> <p>The variable is a sampler.</p> </dd>
        /// </summary>
        /// <summary>
        /// <dd> <p>The variable is an unsigned integer.</p> </dd>
        /// </summary>
        /// <summary>
        /// <dd> <p>The variable is an 8-bit unsigned integer.</p> </dd>
        /// </summary>
        /// <summary>
        /// <dd> <p>The variable is a buffer.</p> </dd>
        /// </summary>
        /// <summary>
        /// <dd> <p>The variable is a constant buffer.</p> </dd>
        /// </summary>
        /// <summary>
        /// <dd> <p>The variable is a texture buffer.</p> </dd>
        /// </summary>
        /// <summary>
        /// <dd> <p>The variable is a 1D-texture array.</p> </dd>
        /// </summary>
        /// <summary>
        /// <dd> <p>The variable is a 2D-texture array.</p> </dd>
        /// </summary>
        /// <summary>
        /// <dd> <p>The variable is a 2D-multisampled texture.</p> </dd>
        /// </summary>
        /// <summary>
        /// <dd> <p>The variable is a 2D-multisampled-texture array.</p> </dd>
        /// </summary>
        /// <summary>
        /// <dd> <p>The variable is a texture-cube array.</p> </dd>
        /// </summary>
        /// <summary>
        /// <dd> <p>The variable is a double precision (64-bit) floating-point number.</p> </dd>
        /// </summary>
        /// <summary>
        /// <dd> <p>The variable is a 1D read-and-write texture.</p> </dd>
        /// </summary>
        /// <summary>
        /// <dd> <p>The variable is an array of 1D read-and-write textures.</p> </dd>
        /// </summary>
        /// <summary>
        /// <dd> <p>The variable is a 2D read-and-write texture.</p> </dd>
        /// </summary>
        /// <summary>
        /// <dd> <p>The variable is an array of 2D read-and-write textures.</p> </dd>
        /// </summary>
        /// <summary>
        /// <dd> <p>The variable is a 3D read-and-write texture.</p> </dd>
        /// </summary>
        /// <summary>
        /// <dd> <p>The variable is a read-and-write buffer.</p> </dd>
        /// </summary>
        /// <summary>
        /// <dd> <p>The variable is a byte-address buffer.</p> </dd>
        /// </summary>
        /// <summary>
        /// <dd> <p>The variable is a read-and-write byte-address buffer.</p> </dd>
        /// </summary>
        /// <summary>
        /// <dd> <p>The variable is a structured buffer. </p> <p>For more information about structured buffer, see the <strong>Remarks</strong> section.</p> </dd>
        /// </summary>
        /// <summary>
        /// <dd> <p>The variable is a read-and-write structured buffer.</p> </dd>
        /// </summary>
        /// <summary>
        /// <dd> <p>The variable is an append structured buffer.</p> </dd>
        /// </summary>
        /// <summary>
        /// <dd> <p>The variable is a consume structured buffer.</p> </dd>
        /// </summary>
    /// </summary>
    Void = 0,

    Bool = 1,

    Int = 2,

    Float = 3,

    String = 4,

    Texture = 5,

    Texture1D = 6,

    Texture2D = 7,

    Texture3D = 8,

    TextureCube = 9,

    Sampler = 10,

    Sampler1D = 11,

    Sampler2D = 12,

    Sampler3D = 13,

    SamplerCube = 14,

    UInt = 19,

    UInt8 = 20,

    Buffer = 25,

    ConstantBuffer = 26,

    TextureBuffer = 27,

    Texture1DArray = 28,

    Texture2DArray = 29,

    Texture2DMultisampled = 32,

    Texture2DMultisampledArray = 33,

    TextureCubeArray = 34,

    Double = 39,

    RWTexture1D = 40,

    RWTexture1DArray = 41,

    RWTexture2D = 42,

    RWTexture2DArray = 43,

    RWTexture3D = 44,

    RWBuffer = 45,

    ByteAddressBuffer = 46,

    RWByteAddressBuffer = 47,

    StructuredBuffer = 48,

    RWStructuredBuffer = 49,

    AppendStructuredBuffer = 50,

    ConsumeStructuredBuffer = 51
}
