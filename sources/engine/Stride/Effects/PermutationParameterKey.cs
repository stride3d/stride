// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Core.Serialization;

namespace Stride.Rendering;

/// <summary>
///   A parameter key that identifies an Effect permutation parameter,
///   usually for use by Effects (<c>.sdfx</c>).
/// </summary>
/// <typeparam name="T">The type of the parameter.</typeparam>
/// <remarks>
///   An <see cref="ValueParameterKey{T}"/> can be used to represent values like, for example,
///   vectors, matrices, <see langword="int"/>s, <see langword="float"/>s, etc.
/// </remarks>
[DataSerializer(typeof(PermutationParameterKeySerializer<>), Mode = DataSerializerGenericMode.GenericArguments)]
public sealed class PermutationParameterKey<T> : ParameterKey<T>
{
    /// <summary>
    ///   Initializes a new instance of the <see cref="PermutationParameterKey{T}"/> class.
    /// </summary>
    /// <param name="name">The name with which to identify the parameter key.</param>
    /// <param name="length">The number of elements the parameter is composed of.</param>
    /// <param name="metadata">
    ///   Optional metadata object providing additional information about the parameter or its type.
    /// </param>
    public PermutationParameterKey(string name, int length = 1, PropertyKeyMetadata? metadata = null)
        : base(ParameterKeyType.Permutation, name, length, metadata)
    {
    }

    /// <summary>
    ///   Initializes a new instance of the <see cref="PermutationParameterKey{T}"/> class.
    /// </summary>
    /// <param name="name">The name with which to identify the parameter key.</param>
    /// <param name="length">The number of elements the parameter is composed of.</param>
    /// <param name="metadatas">
    ///   Optional metadata objects providing additional information about the parameter or its type.
    /// </param>
    public PermutationParameterKey(string name, int length = 1, params PropertyKeyMetadata[]? metadatas)
        : base(ParameterKeyType.Permutation, name, length, metadatas)
    {
    }
}
