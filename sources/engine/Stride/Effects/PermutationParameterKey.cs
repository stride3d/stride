// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Core.Serialization;

namespace Stride.Rendering;

[DataSerializer(typeof(PermutationParameterKeySerializer<>), Mode = DataSerializerGenericMode.GenericArguments)]
public sealed class PermutationParameterKey<T> : ParameterKey<T>
{
    public PermutationParameterKey(string name, int length = 1, PropertyKeyMetadata? metadata = null)
        : base(ParameterKeyType.Permutation, name, length, metadata)
    {
    }

    public PermutationParameterKey(string name, int length = 1, params PropertyKeyMetadata[]? metadatas)
        : base(ParameterKeyType.Permutation, name, length, metadatas)
    {
    }
}
