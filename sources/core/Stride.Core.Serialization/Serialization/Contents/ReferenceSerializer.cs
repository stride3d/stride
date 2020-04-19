// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using Stride.Core.Assets;
using Stride.Core.Reflection;

namespace Stride.Core.Serialization.Contents
{
    public static class ReferenceSerializer
    {
        public static readonly PropertyKey<List<object>> CloneReferences = new PropertyKey<List<object>>("CloneReferences", typeof(SerializerExtensions), DefaultValueMetadata.Delegate(delegate { return new List<object>(); }));
    }

    /// <summary>
    /// Serialize object with its underlying Id and Location, and use <see cref="ContentManager"/> to generate a separate chunk.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed class ReferenceSerializer<T> : DataSerializer<T> where T : class
    {
        private IContentSerializer cachedContentSerializer;

        public override void Serialize(ref T obj, ArchiveMode mode, SerializationStream stream)
        {
            var referenceSerialization = stream.Context.Get(ContentSerializerContext.SerializeAttachedReferenceProperty);
            var contentSerializerContext = stream.Context.Get(ContentSerializerContext.ContentSerializerContextProperty);
            if (contentSerializerContext != null)
            {
                if (mode == ArchiveMode.Serialize)
                {
                    var contentReference = new ContentReference<T>(obj);
                    int index = contentSerializerContext.AddContentReference(contentReference);
                    stream.Write(index);
                }
                else
                {
                    int index = stream.ReadInt32();
                    var contentReference = contentSerializerContext.GetContentReference<T>(index);
                    obj = contentReference.Value;
                    if (obj == null)
                    {
                        // Check if already deserialized
                        var assetReference = contentSerializerContext.ContentManager.FindDeserializedObject(contentReference.Location, typeof(T));
                        if (assetReference != null)
                        {
                            obj = (T)assetReference.Object;
                            if (obj != null)
                                contentReference.Value = obj;
                        }
                    }

                    if (obj == null && contentSerializerContext.LoadContentReferences)
                    {
                        var contentSerializer = cachedContentSerializer ?? (cachedContentSerializer = contentSerializerContext.ContentManager.Serializer.GetSerializer(null, typeof(T)));
                        if (contentSerializer == null)
                        {
                            // Need to read chunk header to know actual type (note that we can't cache it in cachedContentSerializer as it depends on content)
                            var chunkHeader = contentSerializerContext.ContentManager.ReadChunkHeader(contentReference.Location);
                            if (chunkHeader == null || (contentSerializer = contentSerializerContext.ContentManager.Serializer.GetSerializer(AssemblyRegistry.GetType(chunkHeader.Type), typeof(T))) == null)
                                throw new InvalidOperationException($"Could not find a valid content serializer for {typeof(T)} when loading {contentReference.Location}");
                        }

                        // First time, let's create it
                        if (contentSerializerContext.ContentManager.Exists(contentReference.Location))
                        {
                            obj = (T)contentSerializer.Construct(contentSerializerContext);
                            contentSerializerContext.ContentManager.RegisterDeserializedObject(contentReference.Location, obj);
                            contentReference.Value = obj;
                        }
                    }
                }
            }
            else if (referenceSerialization == ContentSerializerContext.AttachedReferenceSerialization.AsNull)
            {
                if (mode == ArchiveMode.Deserialize)
                {
                    obj = default(T);
                }
            }
            else if (referenceSerialization == ContentSerializerContext.AttachedReferenceSerialization.AsSerializableVersion)
            {
                if (mode == ArchiveMode.Serialize)
                {
                    // This case will happen when serializing build engine command hashes: we still want Location to still be written
                    var attachedReference = AttachedReferenceManager.GetAttachedReference(obj);
                    if (attachedReference?.Url == null)
                        throw new InvalidOperationException("Error when serializing reference.");

                    // TODO: Do not use string
                    stream.Write(obj.GetType().AssemblyQualifiedName);
                    stream.Write(attachedReference.Id);
                    stream.Write(attachedReference.Url);
                }
                else
                {
                    var type = AssemblyRegistry.GetType(stream.ReadString());
                    var id = stream.Read<AssetId>();
                    var url = stream.ReadString();

                    obj = (T)AttachedReferenceManager.CreateProxyObject(type, id, url);
                }
            }
            else if (referenceSerialization == ContentSerializerContext.AttachedReferenceSerialization.Clone)
            {
                var cloneReferences = stream.Context.Get(ReferenceSerializer.CloneReferences);
                if (mode == ArchiveMode.Serialize)
                {
                    stream.Write(cloneReferences.Count);
                    cloneReferences.Add(obj);
                }
                else
                {
                    obj = (T)cloneReferences[stream.ReadInt32()];
                }
            }
            else
            {
                // This case will happen when serializing build engine command hashes: we still want Location to still be written
                if (mode == ArchiveMode.Serialize)
                {
                    // This case will happen when serializing build engine command hashes: we still want Location to still be written
                    var attachedReference = AttachedReferenceManager.GetAttachedReference(obj);
                    if (attachedReference?.Url == null)
                        throw new InvalidOperationException("Error when serializing reference.");

                    stream.Write(attachedReference.Url);
                }
                else
                {
                    // No real case yet
                    throw new NotSupportedException();
                }
            }
        }
    }
}
