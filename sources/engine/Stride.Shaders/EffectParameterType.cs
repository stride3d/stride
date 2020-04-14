// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core;

namespace Stride.Shaders
{
    /// <summary>
    /// <p>Values that identify various data, texture, and buffer types that can be assigned to a shader variable.</p>
    /// </summary>
    [DataContract]
    public enum EffectParameterType : byte
    {
        /// <summary>
        /// <dd> <p>The variable is a void reference.</p> </dd>
        /// </summary>
        Void = unchecked((int)0),

        /// <summary>
        /// <dd> <p>The variable is a boolean.</p> </dd>
        /// </summary>
        Bool = unchecked((int)1),

        /// <summary>
        /// <dd> <p>The variable is an integer.</p> </dd>
        /// </summary>
        Int = unchecked((int)2),

        /// <summary>
        /// <dd> <p>The variable is a floating-point number.</p> </dd>
        /// </summary>
        Float = unchecked((int)3),

        /// <summary>
        /// <dd> <p>The variable is a string.</p> </dd>
        /// </summary>
        String = unchecked((int)4),

        /// <summary>
        /// <dd> <p>The variable is a texture.</p> </dd>
        /// </summary>
        Texture = unchecked((int)5),

        /// <summary>
        /// <dd> <p>The variable is a 1D texture.</p> </dd>
        /// </summary>
        Texture1D = unchecked((int)6),

        /// <summary>
        /// <dd> <p>The variable is a 2D texture.</p> </dd>
        /// </summary>
        Texture2D = unchecked((int)7),

        /// <summary>
        /// <dd> <p>The variable is a 3D texture.</p> </dd>
        /// </summary>
        Texture3D = unchecked((int)8),

        /// <summary>
        /// <dd> <p>The variable is a texture cube.</p> </dd>
        /// </summary>
        TextureCube = unchecked((int)9),

        /// <summary>
        /// <dd> <p>The variable is a sampler.</p> </dd>
        /// </summary>
        Sampler = unchecked((int)10),

        /// <summary>
        /// <dd> <p>The variable is a sampler.</p> </dd>
        /// </summary>
        Sampler1D = unchecked((int)11),

        /// <summary>
        /// <dd> <p>The variable is a sampler.</p> </dd>
        /// </summary>
        Sampler2D = unchecked((int)12),

        /// <summary>
        /// <dd> <p>The variable is a sampler.</p> </dd>
        /// </summary>
        Sampler3D = unchecked((int)13),

        /// <summary>
        /// <dd> <p>The variable is a sampler.</p> </dd>
        /// </summary>
        SamplerCube = unchecked((int)14),

        /// <summary>
        /// <dd> <p>The variable is an unsigned integer.</p> </dd>
        /// </summary>
        UInt = unchecked((int)19),

        /// <summary>
        /// <dd> <p>The variable is an 8-bit unsigned integer.</p> </dd>
        /// </summary>
        UInt8 = unchecked((int)20),

        /// <summary>
        /// <dd> <p>The variable is a buffer.</p> </dd>
        /// </summary>
        Buffer = unchecked((int)25),

        /// <summary>
        /// <dd> <p>The variable is a constant buffer.</p> </dd>
        /// </summary>
        ConstantBuffer = unchecked((int)26),

        /// <summary>
        /// <dd> <p>The variable is a texture buffer.</p> </dd>
        /// </summary>
        TextureBuffer = unchecked((int)27),

        /// <summary>
        /// <dd> <p>The variable is a 1D-texture array.</p> </dd>
        /// </summary>
        Texture1DArray = unchecked((int)28),

        /// <summary>
        /// <dd> <p>The variable is a 2D-texture array.</p> </dd>
        /// </summary>
        Texture2DArray = unchecked((int)29),

        /// <summary>
        /// <dd> <p>The variable is a 2D-multisampled texture.</p> </dd>
        /// </summary>
        Texture2DMultisampled = unchecked((int)32),

        /// <summary>
        /// <dd> <p>The variable is a 2D-multisampled-texture array.</p> </dd>
        /// </summary>
        Texture2DMultisampledArray = unchecked((int)33),

        /// <summary>
        /// <dd> <p>The variable is a texture-cube array.</p> </dd>
        /// </summary>
        TextureCubeArray = unchecked((int)34),

        /// <summary>
        /// <dd> <p>The variable is a double precision (64-bit) floating-point number.</p> </dd>
        /// </summary>
        Double = unchecked((int)39),

        /// <summary>
        /// <dd> <p>The variable is a 1D read-and-write texture.</p> </dd>
        /// </summary>
        RWTexture1D = unchecked((int)40),

        /// <summary>
        /// <dd> <p>The variable is an array of 1D read-and-write textures.</p> </dd>
        /// </summary>
        RWTexture1DArray = unchecked((int)41),

        /// <summary>
        /// <dd> <p>The variable is a 2D read-and-write texture.</p> </dd>
        /// </summary>
        RWTexture2D = unchecked((int)42),

        /// <summary>
        /// <dd> <p>The variable is an array of 2D read-and-write textures.</p> </dd>
        /// </summary>
        RWTexture2DArray = unchecked((int)43),

        /// <summary>
        /// <dd> <p>The variable is a 3D read-and-write texture.</p> </dd>
        /// </summary>
        RWTexture3D = unchecked((int)44),

        /// <summary>
        /// <dd> <p>The variable is a read-and-write buffer.</p> </dd>
        /// </summary>
        RWBuffer = unchecked((int)45),

        /// <summary>
        /// <dd> <p>The variable is a byte-address buffer.</p> </dd>
        /// </summary>
        ByteAddressBuffer = unchecked((int)46),

        /// <summary>
        /// <dd> <p>The variable is a read-and-write byte-address buffer.</p> </dd>
        /// </summary>
        RWByteAddressBuffer = unchecked((int)47),

        /// <summary>
        /// <dd> <p>The variable is a structured buffer. </p> <p>For more information about structured buffer, see the <strong>Remarks</strong> section.</p> </dd>
        /// </summary>
        StructuredBuffer = unchecked((int)48),

        /// <summary>
        /// <dd> <p>The variable is a read-and-write structured buffer.</p> </dd>
        /// </summary>
        RWStructuredBuffer = unchecked((int)49),

        /// <summary>
        /// <dd> <p>The variable is an append structured buffer.</p> </dd>
        /// </summary>
        AppendStructuredBuffer = unchecked((int)50),

        /// <summary>
        /// <dd> <p>The variable is a consume structured buffer.</p> </dd>
        /// </summary>
        ConsumeStructuredBuffer = unchecked((int)51),
    }
}
