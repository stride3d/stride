// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;

namespace Stride.Shaders;

[DataContract]
public enum EffectParameterClass : byte
{
    /// <summary>
    /// Values that identify the class of a shader variable.
    /// </summary>
    /// <remarks>
    /// The class of a shader variable is not a programming class; the class identifies the variable class such as scalar, vector, object, and so on.
    /// </remarks>
        /// <summary>
        /// <dd> <p>The shader variable is a scalar.</p> </dd>
        /// </summary>
        /// <summary>
        /// <dd> <p>The shader variable is a vector.</p> </dd>
        /// </summary>
        /// <summary>
        /// <dd> <p>The shader variable is a row-major matrix.</p> </dd>
        /// </summary>
        /// <summary>
        /// <dd> <p>The shader variable is a column-major matrix.</p> </dd>
        /// </summary>
        /// <summary>
        /// <dd> <p>The shader variable is an object.</p> </dd>
        /// </summary>
        /// <summary>
        /// <dd> <p>The shader variable is a structure.</p> </dd>
        /// </summary>
        /// <summary>
        /// <dd> <p>The shader variable is a class.</p> </dd>
        /// </summary>
        /// <summary>
        /// <dd> <p>The shader variable is an interface.</p> </dd>
        /// </summary>
        /// <summary>
        /// A sampler state object.
        /// </summary>
        /// <summary>
        /// A shader resource view.
        /// </summary>
        /// <summary>
        /// A constant buffer
        /// </summary>
        /// <summary>
        /// A constant buffer
        /// </summary>
        /// <summary>
        /// An unordered access view
        /// </summary>
        /// <summary>
        /// <dd> <p>The shader variable is a vector.</p> </dd>
        /// </summary>
    /// </summary>
    Scalar = 0,

    Vector = 1,

    MatrixRows = 2,

    MatrixColumns = 3,

    Object = 4,

    Struct = 5,

    InterfaceClass = 6,

    InterfacePointer = 7,

    Sampler = 8,

    ShaderResourceView = 9,

    ConstantBuffer = 10,

    TextureBuffer = 11,

    UnorderedAccessView = 12,

    Color = 13
}
