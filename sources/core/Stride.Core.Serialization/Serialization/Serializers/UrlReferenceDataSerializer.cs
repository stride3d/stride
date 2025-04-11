// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets;

namespace Stride.Core.Serialization.Serializers;

/// <summary>
/// Serializer base class for <see cref="UrlReference"/>.
/// </summary>
public abstract class UrlReferenceDataSerializerBase<T> : DataSerializer<T>
    where T : UrlReferenceBase, new()
{
    /// <inheritdoc/>
    public override void Serialize(ref T urlReference, ArchiveMode mode, SerializationStream stream)
    {
        if (mode == ArchiveMode.Serialize)
        {
            stream.Write(urlReference.Id);
            stream.Write(urlReference.Url);
        }
        else
        {
            var id = stream.Read<AssetId>();
            var url = stream.ReadString();

            urlReference = new T { Url = url, Id = id };
        }
    }
}

/// <summary>
/// Serializer for <see cref="UrlReference"/>.
/// </summary>
public sealed class UrlReferenceDataSerializer : UrlReferenceDataSerializerBase<UrlReference>;

/// <summary>
/// Serializer for <see cref="UrlReference{T}"/>.
/// </summary>
/// <typeparam name="T">The type of asset.</typeparam>
public sealed class UrlReferenceDataSerializer<T> : UrlReferenceDataSerializerBase<UrlReference<T>>
    where T : class;
