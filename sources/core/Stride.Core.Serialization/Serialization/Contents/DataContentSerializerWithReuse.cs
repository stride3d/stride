// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
namespace Stride.Core.Serialization.Contents
{
    /// <summary>
    /// ContentSerializer that simply defers serialization to low level serialization, with <see cref="SerializerSelector.ReuseReferences"/> set to true.
    /// </summary>
    /// <typeparam name="T">The type to serialize.</typeparam>
    public class DataContentSerializerWithReuse<T> : DataContentSerializer<T> where T : new()
    {
        public override void Serialize(ContentSerializerContext context, SerializationStream stream, T obj)
        {
            // Save and change serializer selector to the optimized one
            var previousSerializerSelector = stream.Context.SerializerSelector;
            stream.Context.SerializerSelector = context.ContentManager.Serializer.LowLevelSerializerSelectorWithReuse;

            // Serialize
            base.Serialize(context, stream, obj);

            // Restore serializer selector
            stream.Context.SerializerSelector = previousSerializerSelector;
        }
    }
}
