// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Serialization;

namespace Stride.Rendering;

/// <summary>
///   Provides functionality to serialize and deserialize <see cref="ValueParameterKey{T}"/> objects.
/// </summary>
public class ValueParameterKeySerializer<T> : DataSerializer<ValueParameterKey<T>> where T : struct
{
    /// <summary>
    ///   Serializes or deserializes a <see cref="ValueParameterKey{T}"/> object.
    /// </summary>
    /// <param name="valueParameterKey">The object to serialize or deserialize.</param>
    /// <inheritdoc/>
    public override void Serialize(ref ValueParameterKey<T> valueParameterKey, ArchiveMode mode, SerializationStream stream)
    {
        if (mode == ArchiveMode.Serialize)
        {
            stream.Write(valueParameterKey.Name);
            stream.Write(valueParameterKey.Length);
        }
        else
        {
            var parameterName = stream.ReadString();
            var parameterLength = stream.ReadInt32();

            valueParameterKey = (ValueParameterKey<T>) ParameterKeys.FindByName(parameterName);

            // If parameter could not be found, create one matching this type
            if (valueParameterKey is null)
            {
                var metadata = new ParameterKeyValueMetadata<T>();
                valueParameterKey = new ValueParameterKey<T>(parameterName, parameterLength, metadata);

                ParameterKeys.Merge(valueParameterKey, ownerType: null, parameterName);
            }
        }
    }
}
