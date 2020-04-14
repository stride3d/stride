// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using Stride.Core.Serialization;
using Stride.UI;

namespace Stride.Engine.Design
{
    public class UICloner
    {
        private static readonly EntityCloner.CloneContext CloneContext = new EntityCloner.CloneContext();
        private static SerializerSelector cloneSerializerSelector;

        // CloneObject TLS used to clone objects, so that we don't create one everytime we clone
        [ThreadStatic] private static HashSet<object> clonedElementsLazy;

        private static HashSet<object> ClonedElements => clonedElementsLazy ?? (clonedElementsLazy = new HashSet<object>());

        public static UIElement Clone(UIElement element)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            var clonedObjects = ClonedElements;
            try
            {
                clonedObjects.Add(element);
                var clone = Clone(clonedObjects, element);
                return clone;
            }
            finally
            {
                clonedObjects.Clear();
            }
        }

        private static T Clone<T>(HashSet<object> clonedObjects, T entity) where T : class
        {
            if (cloneSerializerSelector == null)
            {
                cloneSerializerSelector = new SerializerSelector(true, false, "Default", "Clone");
            }
            // Initialize CloneContext
            lock (CloneContext)
            {
                try
                {
                    CloneContext.EntitySerializerSelector = cloneSerializerSelector;
                    CloneContext.ClonedObjects = clonedObjects;

                    // Serialize
                    var memoryStream = CloneContext.MemoryStream;
                    var writer = new BinarySerializationWriter(memoryStream);
                    writer.Context.SerializerSelector = cloneSerializerSelector;
                    writer.Context.Set(EntityCloner.CloneContextProperty, CloneContext);
                    writer.SerializeExtended(entity, ArchiveMode.Serialize);

                    // Deserialization reuses this list and expect it to be empty at the beginning.
                    CloneContext.SerializedObjects.Clear();

                    // Deserialize
                    T result = null;
                    memoryStream.Seek(0, SeekOrigin.Begin);
                    var reader = new BinarySerializationReader(memoryStream);
                    reader.Context.SerializerSelector = cloneSerializerSelector;
                    reader.Context.Set(EntityCloner.CloneContextProperty, CloneContext);
                    reader.SerializeExtended(ref result, ArchiveMode.Deserialize);

                    return result;
                }
                finally
                {
                    CloneContext.Cleanup();
                }
            }
        }
    }
}
