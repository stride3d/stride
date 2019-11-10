using System;
using System.Collections.Generic;
using System.Text;
using Xenko.Core.Annotations;
using Xenko.Core.Assets;

namespace Xenko.Core.Serialization.Serialization.Serializers
{
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
                stream.Write(urlReference.Url);
            }
            else
            {
                var url = stream.ReadString();

                urlReference = new UrlReference(url);
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
                stream.Write(urlReference.Url);
            }
            else
            {
                var url = stream.ReadString();

                urlReference = new UrlReference<T>(url);
            }
        }
    }
}
