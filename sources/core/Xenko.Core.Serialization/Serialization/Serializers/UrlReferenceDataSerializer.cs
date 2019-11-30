using System;
using System.Collections.Generic;
using System.Text;
using Xenko.Core.Annotations;
using Xenko.Core.Assets;

namespace Xenko.Core.Serialization.Serializers
{
    /// <summary>
    /// Serializer base class for for <see cref="UrlReference"/>.
    /// </summary>
    public abstract class UrlReferenceDataSerializerBase<T> : DataSerializer<T>
        where T: UrlReference
    {
        /// <inheritdoc/>
        public override void Serialize(ref T urlReference, ArchiveMode mode, [NotNull] SerializationStream stream)
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

                urlReference = (T)AttachedReferenceManager.CreateProxyObject(typeof(T),id,url);
            }
        }
    }

    /// <summary>
    /// Serializer for <see cref="UrlReference"/>.
    /// </summary>
    public sealed class UrlReferenceDataSerializer : UrlReferenceDataSerializerBase<UrlReference>
    {
 
    }

    /// <summary>
    /// Serializer for <see cref="UrlReference{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of asset.</typeparam>
    public sealed class UrlReferenceDataSerializer<T> : UrlReferenceDataSerializerBase<UrlReference<T>>
        where T : class
    {
       
    }
}
