// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Serialization;

namespace Stride.Rendering;

/// <summary>
///   Provides functionality to serialize and deserialize <see cref="PermutationParameterKey{T}"/> objects.
/// </summary>
public class PermutationParameterKeySerializer<T> : DataSerializer<PermutationParameterKey<T>>
{
    /// <summary>
    ///   Serializes or deserializes a <see cref="PermutationParameterKey{T}"/> object.
    /// </summary>
    /// <param name="permutationParameterKey">The object to serialize or deserialize.</param>
    /// <inheritdoc/>
    public override void Serialize(ref PermutationParameterKey<T> permutationParameterKey, ArchiveMode mode, SerializationStream stream)
    {
        if (mode == ArchiveMode.Serialize)
        {
            stream.Write(permutationParameterKey.Name);
            stream.Write(permutationParameterKey.Length);
        }
        else
        {
            var parameterName = stream.ReadString();
            var parameterLength = stream.ReadInt32();

            permutationParameterKey = (PermutationParameterKey<T>)ParameterKeys.FindByName(parameterName);

            // If parameter could not be found, create one matching this type
            if (permutationParameterKey == null)
            {
                var metadata = new ParameterKeyValueMetadata<T>();
                permutationParameterKey = new PermutationParameterKey<T>(parameterName, parameterLength, metadata);

                ParameterKeys.Merge(permutationParameterKey, ownerType: null, parameterName);
            }
        }
    }
}
