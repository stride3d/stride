// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Serialization;

namespace Stride.Rendering;

/// <summary>
///   Provides functionality to serialize and deserialize <see cref="ObjectParameterKey{T}"/> objects.
/// </summary>
public class ObjectParameterKeySerializer<T> : DataSerializer<ObjectParameterKey<T>>
{
    /// <summary>
    ///   Serializes or deserializes a <see cref="ObjectParameterKey{T}"/> object.
    /// </summary>
    /// <param name="objectParameterKey">The object to serialize or deserialize.</param>
    /// <inheritdoc/>
    public override void Serialize(ref ObjectParameterKey<T> objectParameterKey, ArchiveMode mode, SerializationStream stream)
    {
        if (mode == ArchiveMode.Serialize)
        {
            stream.Write(objectParameterKey.Name);
            stream.Write(objectParameterKey.Length);
        }
        else
        {
            var parameterName = stream.ReadString();
            var parameterLength = stream.ReadInt32();

            objectParameterKey = (ObjectParameterKey<T>) ParameterKeys.FindByName(parameterName);

            // If parameter could not be found, create one matching this type
            if (objectParameterKey is null)
            {
                var metadata = new ParameterKeyValueMetadata<T>();
                objectParameterKey = new ObjectParameterKey<T>(parameterName, parameterLength, metadata);

                ParameterKeys.Merge(objectParameterKey, ownerType: null, parameterName);
            }
        }
    }
}
