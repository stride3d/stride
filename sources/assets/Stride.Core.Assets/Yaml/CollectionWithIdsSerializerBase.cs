// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Stride.Core.Diagnostics;
using Stride.Core.Reflection;
using Stride.Core.Yaml.Events;
using Stride.Core.Yaml.Serialization;
using Stride.Core.Yaml.Serialization.Serializers;

namespace Stride.Core.Yaml
{
    /// <summary>
    /// A base class to serialize collections with unique identifiers for each item.
    /// </summary>
    [YamlSerializerFactory("Assets")]
    public abstract class CollectionWithIdsSerializerBase : DictionarySerializer
    {
        /// <summary>
        /// A string token to identify deleted items in a collection.
        /// </summary>
        public const string YamlDeletedKey = "~(Deleted)";
        /// <summary>
        /// A property key to indicate whether a collection has non-identifiable items
        /// </summary>
        public static readonly PropertyKey<bool> NonIdentifiableCollectionItemsKey = new PropertyKey<bool>("NonIdentifiableCollectionItems", typeof(CollectionWithIdsSerializer));
        /// <summary>
        /// A key that identifies the information about the instance that we need the store in the <see cref="ObjectContext.Properties"/> dictionary.
        /// </summary>
        protected static readonly PropertyKey<InstanceInfo> InstanceInfoKey = new PropertyKey<InstanceInfo>("InstanceInfo", typeof(CollectionWithIdsSerializer));
        /// <summary>
        /// A key that identifies deleted items during deserialization.
        /// </summary>
        protected static readonly PropertyKey<ICollection<ItemId>> DeletedItemsKey = new PropertyKey<ICollection<ItemId>>("DeletedItems", typeof(CollectionWithIdsSerializer));

        /// <summary>
        /// A structure containing the information about the instance that we need the store in the <see cref="ObjectContext.Properties"/> dictionary. 
        /// </summary>
        protected internal class InstanceInfo
        {
            public InstanceInfo(object instance, ITypeDescriptor typeDescriptor)
            {               
                Instance = instance;
                Descriptor = typeDescriptor;
            }
            public readonly object Instance;
            public readonly ITypeDescriptor Descriptor;
        }

        public override object ReadYaml(ref ObjectContext objectContext)
        {
            // Create or transform the value to deserialize
            // If the new value to serialize is not the same as the one we were expecting to serialize
            CreateOrTransformObject(ref objectContext);
            var newValue = objectContext.Instance;

            var transformed = false;
            if (newValue != null && newValue.GetType() != objectContext.Descriptor.Type)
            {
                transformed = true;
                objectContext.Descriptor = objectContext.SerializerContext.FindTypeDescriptor(newValue.GetType());
            }

            ReadYamlAfterTransform(ref objectContext, transformed);

            TransformObjectAfterRead(ref objectContext);

            // Process members
            return objectContext.Instance;
        }

        public override void WriteYaml(ref ObjectContext objectContext)
        {
            // TODO: the API could be changed at the ObjectSerializer level so we can, through override:
            // TODO: - customize what to do -if- the object got transformed (currently: ObjectSerializer = routing, here = keep same serializer)
            // TODO: - override WriteYamlAfterTransform without overriding WriteYaml
            // TODO: - and similar with reading

            CreateOrTransformObject(ref objectContext);
            var newValue = objectContext.Instance;

            var transformed = false;
            if (newValue != null && newValue.GetType() != objectContext.Descriptor.Type)
            {
                transformed = true;
                objectContext.Descriptor = objectContext.SerializerContext.FindTypeDescriptor(newValue.GetType());
            }

            WriteYamlAfterTransform(ref objectContext, transformed);
        }

        /// <inheritdoc/>
        protected override void CreateOrTransformObject(ref ObjectContext objectContext)
        {
            base.CreateOrTransformObject(ref objectContext);

            // Allow to deserialize the old way
            if (!objectContext.SerializerContext.IsSerializing && objectContext.Reader.Accept<SequenceStart>())
                return;

            // Ignore collections flagged as having non-identifiable items
            if (!AreCollectionItemsIdentifiable(ref objectContext))
                return;

            // Store the information on the actual instance before transforming.
            var info = new InstanceInfo(objectContext.Instance, objectContext.Descriptor);
            objectContext.Properties.Add(InstanceInfoKey, info);

            if (objectContext.SerializerContext.IsSerializing && objectContext.Instance != null)
            {
                // Store deleted items in the context
                CollectionItemIdentifiers identifier;
                if (CollectionItemIdHelper.TryGetCollectionItemIds(objectContext.Instance, out identifier))
                {
                    var deletedItems = identifier.DeletedItems.ToList();
                    deletedItems.Sort();
                    objectContext.Properties.Add(DeletedItemsKey, deletedItems);
                }
                // We're serializing, transform the collection to a dictionary of <id, items>
                objectContext.Instance = TransformForSerialization(objectContext.Descriptor, objectContext.Instance);
            }
            else
            {
                // We're deserializing, create an empty dictionary of <id, items>
                objectContext.Instance = CreatEmptyContainer(objectContext.Descriptor);
            }
        }

        /// <summary>
        /// Reads the dictionary items key-values.
        /// </summary>
        /// <param name="objectContext"></param>
        protected override void ReadDictionaryItems(ref ObjectContext objectContext)
        {
            var dictionaryDescriptor = (DictionaryDescriptor)objectContext.Descriptor;

            var deletedItems = new HashSet<ItemId>();

            var reader = objectContext.Reader;
            while (!reader.Accept<MappingEnd>())
            {
                var currentDepth = objectContext.Reader.CurrentDepth;
                var startParsingEvent = objectContext.Reader.Parser.Current;

                try
                {
                    // Read key and value
                    var keyValue = ReadDictionaryItem(ref objectContext, new KeyValuePair<Type, Type>(dictionaryDescriptor.KeyType, dictionaryDescriptor.ValueType));
                    if (!Equals(keyValue.Value, YamlDeletedKey) || !(keyValue.Key is ItemId))
                    {
                        dictionaryDescriptor.AddToDictionary(objectContext.Instance, keyValue.Key, keyValue.Value);
                    }
                    else
                    {
                        deletedItems.Add((ItemId)keyValue.Key);
                    }
                }
                catch (YamlException ex)
                {
                    if (objectContext.SerializerContext.AllowErrors)
                    {
                        var logger = objectContext.SerializerContext.Logger;
                        logger?.Warning($"{ex.Message}, this dictionary item will be ignored", ex);
                        objectContext.Reader.Skip(currentDepth, objectContext.Reader.Parser.Current == startParsingEvent);
                    }
                    else throw;
                }
            }

            objectContext.Properties.Add(DeletedItemsKey, deletedItems);
        }

        protected override void WriteDictionaryItems(ref ObjectContext objectContext)
        {
            var dictionaryDescriptor = (DictionaryDescriptor)objectContext.Descriptor;
            var keyValues = dictionaryDescriptor.GetEnumerator(objectContext.Instance).ToList();

            // Not sorting the keys here, they should be already properly sorted when we arrive here
            // TODO: Allow to disable sorting externally, to avoid overriding this method. NOTE: tampering with Settings.SortKeyForMapping is not an option, it is not local but applied to all children. (ParameterKeyDictionarySerializer is doing that and is buggy)

            var keyValueType = new KeyValuePair<Type, Type>(dictionaryDescriptor.KeyType, dictionaryDescriptor.ValueType);

            foreach (var keyValue in keyValues)
            {
                WriteDictionaryItem(ref objectContext, keyValue, keyValueType);
            }

            // Store deleted items in the context
            WriteDeletedItems(ref objectContext);
        }

        protected override KeyValuePair<object, object> ReadDictionaryItem(ref ObjectContext objectContext, KeyValuePair<Type, Type> keyValueTypes)
        {
            var keyResult = objectContext.ObjectSerializerBackend.ReadDictionaryKey(ref objectContext, keyValueTypes.Key);
            var peek = objectContext.SerializerContext.Reader.Peek<Scalar>();
            if (Equals(peek?.Value, YamlDeletedKey))
            {
                return ReadDeletedDictionaryItem(ref objectContext, keyResult);
            }
            var valueResult = objectContext.ObjectSerializerBackend.ReadDictionaryValue(ref objectContext, keyValueTypes.Value, keyResult);
            return new KeyValuePair<object, object>(keyResult, valueResult);
        }

        protected abstract KeyValuePair<object, object> ReadDeletedDictionaryItem(ref ObjectContext objectContext, object keyResult);

        protected override bool CheckIsSequence(ref ObjectContext objectContext)
        {
            var collectionDescriptor = objectContext.Descriptor as CollectionDescriptor;

            // If the dictionary is pure, we can directly output a sequence instead of a mapping
            return collectionDescriptor != null && collectionDescriptor.IsPureCollection;
        }

        protected virtual void ReadYamlAfterTransform(ref ObjectContext objectContext, bool transformed)
        {
            ReadMembers<MappingStart, MappingEnd>(ref objectContext);
        }

        protected virtual void WriteYamlAfterTransform(ref ObjectContext objectContext, bool transformed)
        {
            var type = objectContext.Instance.GetType();
            var context = objectContext.SerializerContext;

            // Resolve the style, use default style if not defined.
            var style = GetStyle(ref objectContext);

            context.Writer.Emit(new MappingStartEventInfo(objectContext.Instance, type) { Tag = objectContext.Tag, Anchor = objectContext.Anchor, Style = style });
            WriteMembers(ref objectContext);
            //WriteDeleted(ref objectContext);
            context.Writer.Emit(new MappingEndEventInfo(objectContext.Instance, type));
        }

        /// <summary>
        /// Transforms the given collection or dictionary into a dictionary of (ids, items) or a dictionary of (ids & keys, items).
        /// </summary>
        /// <param name="descriptor">The type descriptor of the collection.</param>
        /// <param name="collection">The collection for which to create the mapping dictionary.</param>
        /// <returns>A dictionary mapping the id to the element of the initial collection.</returns>
        protected abstract object TransformForSerialization(ITypeDescriptor descriptor, object collection);

        /// <summary>
        /// Creates an empty dictionary that can store the mapping of ids to items of the collection.
        /// </summary>
        /// <param name="descriptor">The type descriptor of the collection for which to create the dictionary.</param>
        /// <returns>An empty dictionary for mapping ids to elements.</returns>
        protected abstract IDictionary CreatEmptyContainer(ITypeDescriptor descriptor);

        /// <summary>
        /// Transforms a dictionary containing the mapping of ids to items into the actual collection, and store the ids in the <see cref="Reflection.ShadowObject"/>.
        /// </summary>
        /// <param name="container">The dictionary mapping ids to item.</param>
        /// <param name="targetDescriptor">The type descriptor of the actual collection to fill.</param>
        /// <param name="targetCollection">The instance of the actual collection to fill.</param>
        /// <param name="deletedItems">A collection of items that are marked as deleted. Can be null.</param>
        protected abstract void TransformAfterDeserialization(IDictionary container, ITypeDescriptor targetDescriptor, object targetCollection, ICollection<ItemId> deletedItems = null);

        protected abstract void WriteDeletedItems(ref ObjectContext objectContext);

        protected static bool AreCollectionItemsIdentifiable(ref ObjectContext objectContext)
        {
            bool nonIdentifiableItems;

            // Check in the serializer context first, for disabling of item identifiers at parent type level
            if (objectContext.SerializerContext.Properties.TryGetValue(NonIdentifiableCollectionItemsKey, out nonIdentifiableItems) && nonIdentifiableItems)
                return false;

            // Then check locally for disabling of item identifiers at member level
            if (objectContext.Properties.TryGetValue(NonIdentifiableCollectionItemsKey, out nonIdentifiableItems) && nonIdentifiableItems)
                return false;

            return true;
        }
    }
}
