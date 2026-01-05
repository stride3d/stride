// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Rendering;

/// <summary>
///   Specifies the type of a parameter identified by a <see cref="ParameterKey"/>.
/// </summary>
public enum ParameterKeyType
{
    /// <summary>
    ///   The parameter is a value, such as a <see langword="float"/>,
    ///   <see langword="int"/>, vector (<c>float3</c>, <c>float4</c>, ...), etc.
    /// </summary>
    Value,

    /// <summary>
    ///   The parameter is an object, such as a <c>Texture</c>, a <c>SamplerState</c>,
    ///   a <c>Buffer</c>, or any other Graphics Resource that can be bound.
    /// </summary>
    Object,

    /// <summary>
    ///   The parameter is a permutation, which is used to define variations in
    ///   Effects and Shaders.
    /// </summary>
    Permutation
}
