// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using Stride.Core.Serialization.Serializers;

// Serializer for ContentSerializerContext.SerializeReferences()
namespace Stride.Core.Serialization.Contents
{
    // TODO: Many simplifications/cleaning (lot of leftover from old system)
    [DataSerializerGlobal(typeof(ListSerializer<ChunkReference>))]
    public class ContentSerializerContext
    {
        private readonly List<ChunkReference> chunkReferences = new List<ChunkReference>();
        private readonly Dictionary<Type, int> objectsPerType = new Dictionary<Type, int>();
        private readonly HashSet<object> generatedUrlObjects = new HashSet<object>();
        private readonly string generatedUrlPrefix;

        public enum AttachedReferenceSerialization
        {
            Unset,
            AsSerializableVersion,
            AsNull,
            Clone,
        }

        public static PropertyKey<ContentSerializerContext> ContentSerializerContextProperty = new PropertyKey<ContentSerializerContext>("ContentSerializerContext", typeof(ContentSerializerContext));

        public static PropertyKey<AttachedReferenceSerialization> SerializeAttachedReferenceProperty = new PropertyKey<AttachedReferenceSerialization>("SerializeAttachedReference", typeof(ContentSerializerContext));

        public ContentManager ContentManager { get; }

        public string Url { get; }

        public ArchiveMode Mode { get; }

        internal List<ContentReference> ContentReferences { get; }

        internal bool LoadContentReferences { get; set; }

        public bool AllowContentStreaming { get; set; }

        internal ContentSerializerContext(string url, ArchiveMode mode, ContentManager contentManager)
        {
            Url = url;
            Mode = mode;
            ContentManager = contentManager;
            ContentReferences = new List<ContentReference>();
            generatedUrlPrefix = Url + "/gen/";
        }

        internal void SerializeContent(SerializationStream stream, IContentSerializer serializer, object objToSerialize)
        {
            stream.Context.SerializerSelector = ContentManager.Serializer.LowLevelSerializerSelector;
            serializer.Serialize(this, stream, objToSerialize);
        }

        internal void SerializeReferences(SerializationStream stream)
        {
            var references = chunkReferences;
            stream.Context.SerializerSelector = ContentManager.Serializer.LowLevelSerializerSelector;
            stream.Serialize(ref references, Mode);
        }

        internal int AddContentReference(ContentReference reference)
        {
            if (reference == null)
                return ChunkReference.NullIdentifier;

            // TODO: This behavior should be controllable
            if (reference.State != ContentReferenceState.NeverLoad && reference.ObjectValue != null)
            {
                // Auto-generate URL if necessary
                BuildUrl(reference);
                //Executor.ProcessObject(this, reference.Type, reference);
                ContentReferences.Add(reference);
            }

            return AddChunkReference(reference.Location, reference.Type);
        }

        internal ContentReference<T> GetContentReference<T>(int index) where T : class
        {
            if (index == ChunkReference.NullIdentifier)
                return null;

            var chunkReference = GetChunkReference(index);

            var contentReference = new ContentReference<T>(chunkReference.Location);

            ContentReferences.Add(contentReference);

            return contentReference;
        }

        private ChunkReference GetChunkReference(int index)
        {
            return chunkReferences[index];
        }

        private int AddChunkReference(string url, Type type)
        {
            // Starting search from the end is maybe more likely to hit quickly (and cache friendly)?
            for (int i = chunkReferences.Count - 1; i >= 0; --i)
            {
                var currentReference = chunkReferences[i];
                if (currentReference.Location == url && currentReference.ObjectType == type)
                {
                    return i;
                }
            }

            var reference = new ChunkReference(type, url);
            var index = chunkReferences.Count;
            chunkReferences.Add(reference);
            return index;
        }

        private void BuildUrl(ContentReference reference)
        {
            var content = reference.ObjectValue;
            string url = reference.Location;

            if (content == null)
                return;

            if (url == null)
            {
                // Already registered?
                if (ContentManager.TryGetAssetUrl(content, out url))
                {
                    reference.Location = url;
                    return;
                }

                generatedUrlObjects.Add(content);

                // No URL, need to generate one.
                // Try to be as deterministic as possible (generated from root URL, type and index).
                var contentType = content.GetType();

                // Get and update current count
                int currentCount;
                objectsPerType.TryGetValue(contentType, out currentCount);
                objectsPerType[contentType] = ++currentCount;

                reference.Location = $"{generatedUrlPrefix}{content.GetType().Name}_{currentCount}";
            }

            // Register it
            //if (reference.Location != null)
            //    ContentManager.RegisterAsset(reference.Location, reference.ObjectValue, serializationType, false);
        }
    }
}
