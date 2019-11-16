using System;
using System.Collections.Generic;
using System.Text;
using Xenko.Core.Annotations;
using Xenko.Core.Assets;

namespace Xenko.Core.Serialization.Serialization.Serializers
{
    //TODO: Can we make it so AssetId is not serialized and not create AttachedReference at runtime.

    /// <summary>
    /// Serializer for <see cref="UrlReference"/>.
    /// </summary>
    public sealed class UrlReferenceDataSerializer : DataSerializer<UrlReference>
    {
        /// <inheritdoc/>
        public override void Serialize(ref UrlReference urlReference, ArchiveMode mode, [NotNull] SerializationStream stream)
        {
            if (mode == ArchiveMode.Serialize)
            {
                var id = AttachedReferenceManager.GetAttachedReference(urlReference)?.Id ?? throw new Exception("OH NOES, no Id");
                stream.Write(id);

                stream.Write(urlReference.Url);
            }
            else
            {
                var id = stream.Read<AssetId>();

                var url = stream.ReadString();

                urlReference = (UrlReference)UrlReferenceHelper.CreateReference(id, url, typeof(UrlReference));
            }
        }
    }

    /// <summary>
    /// Serializer for <see cref="UrlReference{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of asset.</typeparam>
    public sealed class UrlReferenceDataSerializer<T> : DataSerializer<UrlReference<T>>
        where T: class
    {
        /// <inheritdoc/>
        public override void Serialize(ref UrlReference<T> urlReference, ArchiveMode mode, [NotNull] SerializationStream stream)
        {
            if (mode == ArchiveMode.Serialize)
            {
                var id = AttachedReferenceManager.GetAttachedReference(urlReference)?.Id ?? throw new Exception("OH NOES, no Id");
                stream.Write(id);
                stream.Write(urlReference.Url);
            }
            else
            {
                var id = stream.Read<AssetId>();
                var url = stream.ReadString();

                urlReference = (UrlReference<T>)UrlReferenceHelper.CreateReference(id, url, typeof(UrlReference<T>));
            }
        }
    }
}
