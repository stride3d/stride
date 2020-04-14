// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

namespace Stride.Core.Serialization.Contents
{
    internal sealed class ContentReferenceDataSerializer<T> : DataSerializer<ContentReference<T>> where T : class
    {
        public override void Serialize(ref ContentReference<T> reference, ArchiveMode mode, SerializationStream stream)
        {
            var contentSerializerContext = stream.Context.Get(ContentSerializerContext.ContentSerializerContextProperty);
            if (contentSerializerContext != null)
            {
                if (mode == ArchiveMode.Serialize)
                {
                    int index = contentSerializerContext.AddContentReference(reference);
                    stream.Write(index);
                }
                else
                {
                    int index = stream.ReadInt32();
                    reference = contentSerializerContext.GetContentReference<T>(index);
                }
            }
            else
            {
                // This case will happen when serializing build engine command hashes: we still want Location to still be written
                if (mode == ArchiveMode.Serialize)
                {
                    {
                        // This case will happen when serializing build engine command hashes: we still want Location to still be written
                        stream.Write(reference.Location);
                    }
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
