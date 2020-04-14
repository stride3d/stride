// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Serialization;

namespace Stride.Engine.Design
{
    /// <summary>
    /// Serializer for helping cloning of <see cref="Entity"/>.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class CloneSerializer<T> : DataSerializer<T> where T : class
    {
        public override void PreSerialize(ref T obj, ArchiveMode mode, SerializationStream stream)
        {
            var cloneContext = stream.Context.Get(EntityCloner.CloneContextProperty);

            if (mode == ArchiveMode.Serialize)
            {
                // Check against list of items that should be included in the stream (Entity and EntityComponent whose parent is the cloned Entity).
                // First try MappedObjects, then ClonedObjects.
                object mappedObject = null;
                bool isSharedObject = cloneContext.MappedObjects != null && cloneContext.MappedObjects(obj, out mappedObject);

                if (!isSharedObject && cloneContext.ClonedObjects != null && !cloneContext.ClonedObjects.Contains(obj))
                {
                    isSharedObject = true;
                    mappedObject = obj;
                }

                stream.Write(isSharedObject);

                if (isSharedObject)
                {
                    stream.Write(cloneContext.SharedObjects.Count);
                    cloneContext.SharedObjects.Add(mappedObject);
                }
                else
                {
                    cloneContext.SerializedObjects.Add(obj);
                }
            }
            else
            {
                bool isSharedObject = stream.ReadBoolean();

                if (isSharedObject)
                {
                    var sharedObjectIndex = stream.ReadInt32();
                    obj = (T)cloneContext.SharedObjects[sharedObjectIndex];

                    // TODO: Hardcoded
                    // Model need to be cloned
                    //if (obj is Model)
                    //{
                    //    obj = (T)(object)((Model)(object)obj).Instantiate();
                    //}
                }
                else
                {
                    base.PreSerialize(ref obj, mode, stream);
                    cloneContext.SerializedObjects.Add(obj);
                }
            }
        }

        public override void Serialize(ref T obj, ArchiveMode mode, SerializationStream stream)
        {
            var cloneContext = stream.Context.Get(EntityCloner.CloneContextProperty);

            if (cloneContext.SerializedObjects.Contains(obj))
            {
                // Get actual serializer
                var dataSerializer = cloneContext.EntitySerializerSelector.GetSerializer<T>();

                // Serialize object
                //stream.Context.Set(EntitySerializer.InsideEntityComponentProperty, false);
                dataSerializer.Serialize(ref obj, mode, stream);

                if (obj is EntityComponent)
                {
                    // Serialize underlying Entity (needs to be part of the object graph)
                    var entity = ((EntityComponent)(object)obj).Entity;
                    //var entityDataSerializer = cloneContext.EntitySerializer.GetSerializer<Entity>();
                    stream.Serialize(ref entity, mode);

                    ((EntityComponent)(object)obj).Entity = entity;
                }
            }
        }
    }
}
