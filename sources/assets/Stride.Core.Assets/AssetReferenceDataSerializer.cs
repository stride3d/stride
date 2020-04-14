// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Xenko.Core.Serialization;

namespace Xenko.Core.Assets
{
    /// <summary>
    /// Serializer for <see cref="AssetReference"/>.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed class AssetReferenceDataSerializer : DataSerializer<AssetReference>
    {
        /// <inheritdoc/>
        public override void Serialize(ref AssetReference assetReference, ArchiveMode mode, SerializationStream stream)
        {
            if (mode == ArchiveMode.Serialize)
            {
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
}
