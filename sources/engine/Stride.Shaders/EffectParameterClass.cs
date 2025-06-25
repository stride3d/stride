// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;

namespace Stride.Shaders;

/// <summary>
///   Defines the class of a Effect / Shader parameter.
/// </summary>
/// <remarks>
///   The class of a Effect / Shader parameter is not a C# <see langword="class"/>; it identifies the kind of variable
///   such as <strong>scalar</strong>, <strong>vector</strong>, <strong>object</strong>, and so on.
/// </remarks>
[DataContract]
public enum EffectParameterClass : byte
{
    /// <summary>
    ///   The Shader parameter is a <strong>scalar value</strong>.
    /// </summary>
    Scalar = 0,

    /// <summary>
    ///   The Shader parameter is a <strong>vector value</strong>.
    /// </summary>
    Vector = 1,

    /// <summary>
    ///   The Shader parameter is a <strong>row-major matrix</strong>.
    /// </summary>
    MatrixRows = 2,

    /// <summary>
    ///   The Shader parameter is a <strong>column-major matrix</strong>.
    /// </summary>
    MatrixColumns = 3,

    /// <summary>
    ///   The Shader parameter is an <strong>object</strong>.
    /// </summary>
    Object = 4,

    /// <summary>
    ///   The Shader parameter is a <strong>structure</strong>.
    /// </summary>
    Struct = 5,

    /// <summary>
    ///   The Shader parameter is a <strong>class</strong>.
    /// </summary>
    InterfaceClass = 6,

    /// <summary>
    ///   The Shader parameter is an <strong>interface</strong>.
    /// </summary>
    InterfacePointer = 7,

    /// <summary>
    ///   The Shader parameter is a <strong>Sampler State object</strong>.
    /// </summary>
    Sampler = 8,

    /// <summary>
    ///   The Shader parameter is a <strong>Shader Resource View</strong>.
    /// </summary>
    ShaderResourceView = 9,

    /// <summary>
    ///   The Shader parameter is a <strong>Constant Buffer</strong>.
    /// </summary>
    ConstantBuffer = 10,

    /// <summary>
    ///   The Shader parameter is a <strong>Texture</strong>.
    /// </summary>
    TextureBuffer = 11,

    /// <summary>
    ///   The Shader parameter is an <strong>Unordered Access View</strong>.
    /// </summary>
    UnorderedAccessView = 12,

    /// <summary>
    ///   The Shader parameter is a vector value that represents a <strong>color</strong>.
    /// </summary>
    Color = 13
}
