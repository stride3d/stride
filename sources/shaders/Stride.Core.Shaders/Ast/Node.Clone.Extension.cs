// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Stride.Core.Serialization;
using Stride.Core.Shaders.Utility;

namespace Stride.Core.Shaders.Ast
{
    public class CloneContext
    {
        private MemoryStream memoryStream;
        private BinarySerializationWriter writer;
        private BinarySerializationReader reader;
        private Dictionary<object, int> serializeReferences;
        private List<object> deserializeReferences;

        public CloneContext(CloneContext parent = null)
        {
            // Setup
            memoryStream = new MemoryStream(4096);
            writer = new BinarySerializationWriter(memoryStream);
            reader = new BinarySerializationReader(memoryStream);

            writer.Context.SerializerSelector = SerializerSelector.AssetWithReuse;
            reader.Context.SerializerSelector = SerializerSelector.AssetWithReuse;

            serializeReferences = writer.Context.Tags.Get(MemberSerializer.ObjectSerializeReferences);
            deserializeReferences = reader.Context.Tags.Get(MemberSerializer.ObjectDeserializeReferences);

            if (parent != null)
            {
                foreach (var item in parent.serializeReferences)
                    serializeReferences.Add(item.Key, item.Value);
                foreach (var item in parent.deserializeReferences)
                    deserializeReferences.Add(item);
            }
        }

        public void Add(object key, object value)
        {
            serializeReferences.Add(key, deserializeReferences.Count);
            deserializeReferences.Add(value);
        }

        public void Remove(object key)
        {
            // Swap remove with last one
            int index;
            if (serializeReferences.TryGetValue(key, out index))
            {
                serializeReferences.Remove(key);

                // Swap remove
                if (index < deserializeReferences.Count - 1)
                {
                    deserializeReferences[index] = deserializeReferences[deserializeReferences.Count - 1];
                    
                    // Update new object => index mapping
                    // Note: quite slow because we have to scan full dictionnary
                    foreach (var item in serializeReferences)
                    {
                        if (item.Value == deserializeReferences.Count - 1)
                        {
                            serializeReferences[item.Key] = index;
                            break;
                        }
                    }
                }

                deserializeReferences.RemoveAt(deserializeReferences.Count - 1);
            }
        }

        internal void DeepCollect<T>(T obj)
        {
            // Collect
            writer.SerializeExtended(obj, ArchiveMode.Serialize);

            // Reset stream and references
            memoryStream.Seek(0, SeekOrigin.Begin);
            memoryStream.SetLength(0);

            serializeReferences.Clear();
        }

        internal T DeepClone<T>(T obj)
        {
            // Serialize
            writer.SerializeExtended(obj, ArchiveMode.Serialize);

            // Deserialize
            obj = default(T);
            memoryStream.Seek(0, SeekOrigin.Begin);
            reader.SerializeExtended(ref obj, ArchiveMode.Deserialize);

            // Reset stream and references
            memoryStream.Seek(0, SeekOrigin.Begin);
            memoryStream.SetLength(0);

            return obj;
        }
    }

    public static class DeepCloner
    {
        public static void DeepCollect<T>(T obj, CloneContext context)
        {
            context.DeepCollect(obj);
        }

        public static T DeepClone<T>(this T obj, CloneContext context = null)
        {
            // Setup contexts
            if (context == null)
                context = new CloneContext();

            return context.DeepClone(obj);
        }
    }
}
