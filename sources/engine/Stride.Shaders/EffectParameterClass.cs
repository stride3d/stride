// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core;

namespace Stride.Shaders
{
    /// <summary>
    /// Values that identify the class of a shader variable.
    /// </summary>
    /// <remarks>
    /// The class of a shader variable is not a programming class; the class identifies the variable class such as scalar, vector, object, and so on.
    /// </remarks>
    [DataContract]
    public enum EffectParameterClass : byte
    {
        /// <summary>
        /// <dd> <p>The shader variable is a scalar.</p> </dd>
        /// </summary>
        Scalar = unchecked((int)0),

        /// <summary>
        /// <dd> <p>The shader variable is a vector.</p> </dd>
        /// </summary>
        Vector = unchecked((int)1),

        /// <summary>
        /// <dd> <p>The shader variable is a row-major matrix.</p> </dd>
        /// </summary>
        MatrixRows = unchecked((int)2),

        /// <summary>
        /// <dd> <p>The shader variable is a column-major matrix.</p> </dd>
        /// </summary>
        MatrixColumns = unchecked((int)3),

        /// <summary>
        /// <dd> <p>The shader variable is an object.</p> </dd>
        /// </summary>
        Object = unchecked((int)4),

        /// <summary>
        /// <dd> <p>The shader variable is a structure.</p> </dd>
        /// </summary>
        Struct = unchecked((int)5),

        /// <summary>
        /// <dd> <p>The shader variable is a class.</p> </dd>
        /// </summary>
        InterfaceClass = unchecked((int)6),

        /// <summary>
        /// <dd> <p>The shader variable is an interface.</p> </dd>
        /// </summary>
        InterfacePointer = unchecked((int)7),

        /// <summary>
        /// A sampler state object.
        /// </summary>
        Sampler = unchecked((int)8),

        /// <summary>
        /// A shader resource view.
        /// </summary>
        ShaderResourceView = unchecked((int)9),

        /// <summary>
        /// A constant buffer
        /// </summary>
        ConstantBuffer = unchecked((int)10),

        /// <summary>
        /// A constant buffer
        /// </summary>
        TextureBuffer = unchecked((int)11),

        /// <summary>
        /// An unordered access view
        /// </summary>
        UnorderedAccessView = unchecked((int)12),

        /// <summary>
        /// <dd> <p>The shader variable is a vector.</p> </dd>
        /// </summary>
        Color = unchecked((int)13),

    }
}
