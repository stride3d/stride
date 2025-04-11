// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Serialization;

namespace Stride.Core.Assets;

/// <summary>
/// Serializer for <see cref="AssetReference"/>.
/// </summary>
public sealed class AssetReferenceDataSerializer : DataSerializer<AssetReference>
{
    /// <inheritdoc/>
    public override void Serialize(ref AssetReference assetReference, ArchiveMode mode, SerializationStream stream)
    {
        if (mode == ArchiveMode.Serialize)
        {
            if (assetReference is null)
                return;

            stream.Write(assetReference.Id);
            stream.Write(assetReference.Location);
        }
        else
        {
            var id = stream.Read<AssetId>();
            var location = stream.ReadString();

            assetReference = new AssetReference(id, location);
        }
    }
}
