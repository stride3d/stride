// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Quantum;
using Stride.Core.Assets.Quantum.Visitors;
using Stride.Core.Assets.Serializers;
using Stride.Core.Assets.Yaml;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Extensions;
using Stride.Core.Reflection;
using Stride.Core.TypeConverters;
using Stride.Core.Quantum;

namespace Stride.Core.Assets.Editor.ViewModel.CopyPasteProcessors
{
    /// <summary>
    /// Default paste processor for asset. Handles simple properties and collections.
    /// </summary>
    public class AssetPropertyPasteProcessor : PasteProcessorBase
    {
        public static readonly PropertyKey<bool> IsReplaceKey = new PropertyKey<bool>(nameof(IsReplaceKey), typeof(AssetPropertyPasteProcessor));

        /// <inheritdoc />
        public override bool Accept(Type targetRootType, Type targetMemberType, Type pastedDataType)
        {
            var sourceTypeDescriptor = TypeDescriptorFactory.Default.Find(pastedDataType);
            var targetTypeDescriptor = TypeDescriptorFactory.Default.Find(targetMemberType);

            // Can only paste a collection into another collection, or a dictionary into a dictionary
            if (sourceTypeDescriptor.Category == DescriptorCategory.Collection && targetTypeDescriptor.Category != DescriptorCategory.Collection ||
                sourceTypeDescriptor.Category == DescriptorCategory.Dictionary && targetTypeDescriptor.Category != DescriptorCategory.Dictionary)
                return false;

            // Special case: KeyValuePair<,>
            if (targetTypeDescriptor.Category == DescriptorCategory.Dictionary && sourceTypeDescriptor.Category != DescriptorCategory.Dictionary)
            {
                // Key and Value types must be compatible
                var sourceKeyType = sourceTypeDescriptor.Type.GetMember("Key").OfType<PropertyInfo>().FirstOrDefault()?.PropertyType;
                var sourceValueType = sourceTypeDescriptor.Type.GetMember("Value").OfType<PropertyInfo>().FirstOrDefault()?.PropertyType;
                return sourceKeyType != null && TypeConverterHelper.CanConvert(sourceKeyType, ((DictionaryDescriptor)targetTypeDescriptor).KeyType) &&
                       sourceValueType != null && TypeConverterHelper.CanConvert(sourceValueType, ((DictionaryDescriptor)targetTypeDescriptor).ValueType);
            }
            // Types must be compatible
            var sourceType = TypeDescriptorFactory.Default.Find(pastedDataType).GetInnerCollectionType();
            var destinationType = TypeDescriptorFactory.Default.Find(targetMemberType).GetInnerCollectionType();
            return TypeConverterHelper.CanConvert(sourceType, destinationType);
        }

        /// <inheritdoc />
        public override bool ProcessDeserializedData(AssetPropertyGraphContainer graphContainer, object targetRootObject, Type targetMemberType, ref object data, bool isRootDataObjectReference, AssetId? sourceId, YamlAssetMetadata<OverrideType> overrides, YamlAssetPath basePath)
        {
            if (targetRootObject == null) throw new ArgumentNullException(nameof(targetRootObject));
            if (data == null) throw new ArgumentNullException(nameof(data));

            var asset = (Asset)targetRootObject;
            var targetPropertyGraph = graphContainer.TryGetGraph(asset.Id);

            // We use a container object in case the data itself is an object reference
            var container = isRootDataObjectReference ? new FixupContainer { Data = data } : data;
            var rootNode = targetPropertyGraph.Container.NodeContainer.GetOrCreateNode(container);
            var externalReferences = ExternalReferenceCollector.GetExternalReferences(targetPropertyGraph.Definition, rootNode);

            try
            {
                // Clone to create new ids for any IIdentifiable, except passed external references that will be maintained
                Dictionary<Guid, Guid> idRemapping;
                data = AssetCloner.Clone<object>(data, AssetClonerFlags.GenerateNewIdsForIdentifiableObjects, externalReferences, out idRemapping);
            }
            // TODO: have a proper exception type for serialization failure
            catch (Exception)
            {
                // Note: this can fail if the type doesn't have a binary serializer.
                return false;
            }

            var targetTypeDescriptor = TypeDescriptorFactory.Default.Find(targetMemberType);
            bool result;
            switch (targetTypeDescriptor.Category)
            {
                case DescriptorCategory.Collection:
                    result = ConvertForCollection((CollectionDescriptor)targetTypeDescriptor, ref data);
                    break;
                case DescriptorCategory.Dictionary:
                    result = ConvertForDictionary((DictionaryDescriptor)targetTypeDescriptor, ref data);
                    break;
                case DescriptorCategory.Primitive:
                case DescriptorCategory.Object:
                case DescriptorCategory.NotSupportedObject:
                case DescriptorCategory.Nullable:
                    result = ConvertForProperty(targetTypeDescriptor.Type, ref data);
                    break;
                case DescriptorCategory.Array:
                case DescriptorCategory.Custom:
                    throw new NotSupportedException();

                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (!result)
                return false;

            // Collect all referenceable objects from the target asset (where we're pasting)
            var referenceableObjects = IdentifiableObjectCollector.Collect(targetPropertyGraph.Definition, targetPropertyGraph.RootNode);

            // We use a container object in case the data itself is an object reference
            container = isRootDataObjectReference ? new FixupContainer { Data = data } : data;
            rootNode = targetPropertyGraph.Container.NodeContainer.GetOrCreateNode(container);

            // Generate YAML paths for the external reference so we can go through the normal deserialization fixup method.
            var externalReferenceIds = new HashSet<Guid>(externalReferences.Select(x => x.Id));
            var visitor = new ObjectReferencePathGenerator(targetPropertyGraph.Definition)
            {
                ShouldOutputReference = x => externalReferenceIds.Contains(x)
            };
            visitor.Visit(rootNode);

            // Fixup external references
            FixupObjectReferences.FixupReferences(container, visitor.Result, referenceableObjects, true);

            data = (container as FixupContainer)?.Data ?? data;
            return true;
        }

        /// <inheritdoc />
        public override Task Paste(IPasteItem pasteResultItem, AssetPropertyGraph propertyGraph, ref NodeAccessor nodeAccessor, ref PropertyContainer propertyContainer)
        {
            propertyContainer.TryGetValue(IsReplaceKey, out bool replace);
            Paste(pasteResultItem, nodeAccessor.Node, nodeAccessor.Index, replace);
            return Task.CompletedTask;
        }

        // TODO: replace targetNode & index arguments by a NodeAccessor
        private void Paste([NotNull] IPasteItem pasteResultItem, IGraphNode targetNode, NodeIndex index, bool replace)
        {
            if (pasteResultItem?.Data == null) throw new ArgumentNullException(nameof(pasteResultItem));
            if (targetNode == null) throw new ArgumentNullException(nameof(targetNode));

            var copiedData = pasteResultItem.Data;
            var copiedDataType = copiedData.GetType();
            var copiedDataDescriptor = TypeDescriptorFactory.Default.Find(copiedDataType);
            var memberNode = targetNode as IMemberNode;

            // We're pasting in a node that is not a collection (nor a dictionary), let's just do a member update
            if (!CollectionDescriptor.IsCollection(targetNode.Type))
            {
                if (CanUpdateMember(memberNode, copiedData))
                {
                    UpdateMember(memberNode, copiedData);
                }
                return;
            }

            // Check if target collection/dictionary is null.
            if (memberNode != null && memberNode.Target == null)
            {
                // Check if the type has a public constructor with no arguments
                if (targetNode.Type.GetConstructor(Type.EmptyTypes) != null)
                {
                    // Instantiate a new collection (based on node type)
                    memberNode.Update(Activator.CreateInstance(targetNode.Type));
                }
            }

            var collectionNode = memberNode != null ? memberNode.Target : (IObjectNode)targetNode;
            // The collection/dictionary is null and we couldn't construct it, let's stop here
            if (collectionNode == null)
                return;

            // We're pasting in a dictionary. In this case the only accepted input is a (compatible) dictionary
            if (copiedDataDescriptor.Category == DescriptorCategory.Dictionary && DictionaryDescriptor.IsDictionary(targetNode.Type))
            {
                var copiedDataDictionaryDescriptor = (DictionaryDescriptor)copiedDataDescriptor;
                var existingKeys = collectionNode.Indices.ToList();
                if (replace)
                {
                    var keys = ((DictionaryDescriptor)collectionNode.Descriptor).GetKeys(collectionNode.Retrieve()).Cast<object>().ToList();
                    if (index.IsEmpty)
                    {
                        // If this operation is a replace of the whole dictionary, let's first clear it
                        foreach (var key in keys)
                        {
                            var itemIndex = new NodeIndex(key);
                            if (CanRemoveItem(collectionNode, itemIndex))
                            {
                                var itemToRemove = targetNode.Retrieve(itemIndex);
                                collectionNode.Remove(itemToRemove, itemIndex);
                            }
                        }
                    }
                    else
                    {
                        // Otherwise, just remove the corresponding item
                        if (CanRemoveItem(collectionNode, index))
                        {
                            var itemToRemove = targetNode.Retrieve(index);
                            collectionNode.Remove(itemToRemove, index);
                        }
                    }
                }
                foreach (var kv in copiedDataDictionaryDescriptor.GetEnumerator(copiedData))
                {
                    var itemIndex = new NodeIndex(kv.Key);
                    if (existingKeys.Contains(itemIndex))
                    {
                        // Replace if the key already exists
                        if (CanReplaceItem(collectionNode, itemIndex, kv.Value))
                        {
                            ReplaceItem(collectionNode, itemIndex, kv.Value);
                        }
                    }
                    else
                    {
                        // Add if the key does not exist
                        if (CanInsertItem(collectionNode, itemIndex, kv.Value))
                        {
                            InsertItem(collectionNode, itemIndex, kv.Value);
                        }
                    }
                }
            }
            else if (targetNode.Descriptor.Category == DescriptorCategory.Collection)
            {
                var targetCollectionDescriptor = (CollectionDescriptor)targetNode.Descriptor;
                if (replace)
                {
                    // No index, we're replacing the whole collection
                    if (index.IsEmpty)
                    {
                        // First clear the collection
                        var count = targetCollectionDescriptor.GetCollectionCount(targetNode.Retrieve());
                        for (var j = count - 1; j >= 0; j--)
                        {
                            var itemIndex = new NodeIndex(j);
                            if (CanRemoveItem(collectionNode, itemIndex))
                            {
                                var itemToRemove = targetNode.Retrieve(itemIndex);
                                collectionNode.Remove(itemToRemove, itemIndex);
                            }
                        }

                        // Then add the new items
                        var i = 0;
                        foreach (var item in EnumerateItems(copiedData, copiedDataDescriptor))
                        {
                            var itemIndex = new NodeIndex(i);
                            if (CanInsertItem(collectionNode, itemIndex, item))
                            {
                                InsertItem(collectionNode, itemIndex, item);
                                i++;
                            }
                        }
                    }
                    else
                    {
                        // We're replacing a single item with a given index...
                        var startIndex = index.Int;
                        var i = 0;
                        bool firstItemReplaced = false;
                        foreach (var item in EnumerateItems(copiedData, copiedDataDescriptor))
                        {
                            var itemIndex = new NodeIndex(startIndex + i);
                            if (!firstItemReplaced)
                            {
                                // We replace the first item.
                                if (CanReplaceItem(collectionNode, itemIndex, item))
                                {
                                    ReplaceItem(collectionNode, itemIndex, item);
                                    firstItemReplaced = true;
                                    i++;
                                }
                            }
                            else if (CanInsertItem(collectionNode, itemIndex, item))
                            {
                                // We insert the following items that have no pre-existing counter-part.
                                InsertItem(collectionNode, itemIndex, item);
                                i++;
                            }
                        }
                    }
                }
                else
                {
                    // No index, we're replacing the whole collection
                    if (index.IsEmpty)
                    {
                        // Add the new items
                        var i = targetCollectionDescriptor.GetCollectionCount(targetNode.Retrieve());
                        foreach (var item in EnumerateItems(copiedData, copiedDataDescriptor))
                        {
                            var itemIndex = new NodeIndex(i);
                            if (CanInsertItem(collectionNode, itemIndex, item))
                            {
                                InsertItem(collectionNode, itemIndex, item);
                                i++;
                            }
                        }
                    }
                    else
                    {
                        // Handling non-replacing paste
                        var i = index.Int;
                        foreach (var item in EnumerateItems(copiedData, copiedDataDescriptor))
                        {
                            // We're pasting a collection into the collection, let's add all items at the given index if provided or at the end of the collection.
                            var itemIndex = new NodeIndex(i);
                            if (CanInsertItem(collectionNode, itemIndex, item))
                            {
                                InsertItem(collectionNode, itemIndex, item);
                                i++;
                            }
                        }
                    }
                }
            }
        }

        protected virtual bool CanUpdateMember(IMemberNode member, object newValue)
        {
            return member != null && member.MemberDescriptor.HasSet;
        }

        protected virtual bool CanRemoveItem(IObjectNode collection, NodeIndex index)
        {
            return true;
        }

        protected virtual bool CanReplaceItem(IObjectNode collection, NodeIndex index, object newItem)
        {
            return true;
        }

        protected virtual bool CanInsertItem(IObjectNode collection, NodeIndex index, object newItem)
        {
            return true;
        }

        protected virtual void UpdateMember(IMemberNode member, object newValue)
        {
            member.Update(newValue);
        }

        protected virtual void ReplaceItem(IObjectNode collection, NodeIndex index, object newItem)
        {
            collection.Update(newItem, index);
        }

        protected virtual void InsertItem(IObjectNode collection, NodeIndex index, object newItem)
        {
            collection.Add(newItem, index);
        }

        [DataContract]
        private class FixupContainer
        {
            // ReSharper disable once UnusedAutoPropertyAccessor.Local - used by Quantum
            public object Data { get; set; }
        }

        private static bool ConvertForCollection(CollectionDescriptor collectionDescriptor, [NotNull] ref object data)
        {
            if (CollectionDescriptor.IsCollection(data.GetType()))
            {
                if (!TryConvertCollectionData(data, collectionDescriptor, out data))
                    return false;
            }
            else
            {
                object convertedData;
                if (!TypeConverterHelper.TryConvert(data, collectionDescriptor.ElementType, out convertedData))
                    return false;

                var convertedCollection = Activator.CreateInstance(collectionDescriptor.Type, true);
                collectionDescriptor.Add(convertedCollection, convertedData);
                data = convertedCollection;
            }
            return true;
        }

        private static bool ConvertForDictionary(DictionaryDescriptor dictionaryDescriptor, ref object data)
        {
            object convertedDictionary;
            if (DictionaryDescriptor.IsDictionary(data.GetType()))
            {
                if (!TryConvertDictionaryData(data, dictionaryDescriptor, out convertedDictionary))
                    return false;
            }
            else
            {
                var dataType = data.GetType();
                var key = dataType.GetMember("Key").OfType<PropertyInfo>().FirstOrDefault()?.GetValue(data);
                if (key == null || !TypeConverterHelper.TryConvert(key, dictionaryDescriptor.KeyType, out key))
                    return false;

                var value = dataType.GetMember("Value").OfType<PropertyInfo>().FirstOrDefault()?.GetValue(data);
                if (value == null || !TypeConverterHelper.TryConvert(value, dictionaryDescriptor.ValueType, out value))
                    return false;

                convertedDictionary = Activator.CreateInstance(dictionaryDescriptor.Type, true);
                dictionaryDescriptor.SetValue(convertedDictionary, key, value);
            }
            data = convertedDictionary;
            return true;
        }

        private static bool ConvertForProperty(Type targetType, ref object data)
        {
            return TypeConverterHelper.TryConvert(data, targetType, out data);
        }

        private static IEnumerable EnumerateCollection([NotNull] object collection, [NotNull] CollectionDescriptor collectionDescriptor)
        {
            var count = collectionDescriptor.GetCollectionCount(collection);
            for (var i = 0; i < count; i++)
            {
                yield return collectionDescriptor.GetValue(collection, i);
            }
        }

        private static IEnumerable EnumerateItems([NotNull] object collectionOrSingleItem, ITypeDescriptor typeDescriptor)
        {
            var collectionDescriptor = typeDescriptor as CollectionDescriptor;
            if (collectionDescriptor != null)
            {
                var count = collectionDescriptor.GetCollectionCount(collectionOrSingleItem);
                for (var i = 0; i < count; i++)
                {
                    yield return collectionDescriptor.GetValue(collectionOrSingleItem, i);
                }
            }
            else
            {
                yield return collectionOrSingleItem;
            }
        }

        /// <summary>
        /// Tries to convert the <paramref name="sourceCollection"/> to the type described by <paramref name="collectionDescriptor"/>.
        /// </summary>
        /// <param name="sourceCollection"></param>
        /// <param name="collectionDescriptor"></param>
        /// <param name="convertedCollection"></param>
        /// <returns><c>true</c> if the <paramref name="sourceCollection"/> could be converted to the type described by <paramref name="collectionDescriptor"/>; otherwise, <c>false</c>.</returns>
        private static bool TryConvertCollectionData([NotNull] object sourceCollection, [NotNull] CollectionDescriptor collectionDescriptor, out object convertedCollection)
        {
            try
            {
                var sourceCollectionType = sourceCollection.GetType();
                // Already same type
                if (collectionDescriptor.Type == sourceCollectionType)
                {
                    convertedCollection = sourceCollection;
                    return true;
                }

                convertedCollection = Activator.CreateInstance(collectionDescriptor.Type, true);
                var sourceCollectionDescriptor = (CollectionDescriptor)TypeDescriptorFactory.Default.Find(sourceCollectionType);
                foreach (var item in EnumerateCollection(sourceCollection, sourceCollectionDescriptor))
                {
                    object obj;
                    if (!TypeConverterHelper.TryConvert(item, collectionDescriptor.ElementType, out obj))
                    {
                        // (optimistic) try to convert the remaining items
                        continue;
                    }
                    collectionDescriptor.Add(convertedCollection, obj);
                }
                return collectionDescriptor.GetCollectionCount(convertedCollection) > 0;
            }
            catch (InvalidCastException) { }
            catch (InvalidOperationException) { }
            catch (FormatException) { }
            catch (NotSupportedException) { }
            catch (Exception ex) when (ex.InnerException is InvalidCastException) { }
            catch (Exception ex) when (ex.InnerException is InvalidOperationException) { }
            catch (Exception ex) when (ex.InnerException is FormatException) { }
            catch (Exception ex) when (ex.InnerException is NotSupportedException) { }

            // Incompatible type and no conversion available
            convertedCollection = null;
            return false;
        }

        /// <summary>
        /// Tries to convert the <paramref name="sourceDictionary"/> to the type described by <paramref name="dictionaryDescriptor"/>.
        /// </summary>
        /// <param name="sourceDictionary"></param>
        /// <param name="dictionaryDescriptor"></param>
        /// <param name="convertedDictionary"></param>
        /// <returns><c>true</c> if the <paramref name="sourceDictionary"/> could be converted to the type described by <paramref name="dictionaryDescriptor"/>; otherwise, <c>false</c>.</returns>
        private static bool TryConvertDictionaryData([NotNull] object sourceDictionary, [NotNull] DictionaryDescriptor dictionaryDescriptor, out object convertedDictionary)
        {
            try
            {
                var sourceDictionaryType = sourceDictionary.GetType();
                // Already same type
                if (dictionaryDescriptor.Type == sourceDictionary.GetType())
                {
                    convertedDictionary = sourceDictionary;
                    return true;
                }

                convertedDictionary = Activator.CreateInstance(dictionaryDescriptor.Type, true);
                var sourceDictionaryDescriptor = (DictionaryDescriptor)TypeDescriptorFactory.Default.Find(sourceDictionaryType);
                foreach (var k in sourceDictionaryDescriptor.GetKeys(sourceDictionary))
                {
                    var key = k;
                    if (!TypeConverterHelper.TryConvert(key, dictionaryDescriptor.KeyType, out key))
                    {
                        // (optimistic) try to convert the remaining items
                        continue;
                    }
                    var value = sourceDictionaryDescriptor.GetValue(sourceDictionary, k);
                    if (!TypeConverterHelper.TryConvert(value, dictionaryDescriptor.ValueType, out value))
                    {
                        // (optimistic) try to convert the remaining items
                        continue;
                    }
                    dictionaryDescriptor.SetValue(convertedDictionary, key, value);
                }
                return dictionaryDescriptor.GetKeys(convertedDictionary)?.Count > 0;
            }
            catch (InvalidCastException) { }
            catch (InvalidOperationException) { }
            catch (FormatException) { }
            catch (NotSupportedException) { }
            catch (Exception ex) when (ex.InnerException is InvalidCastException) { }
            catch (Exception ex) when (ex.InnerException is InvalidOperationException) { }
            catch (Exception ex) when (ex.InnerException is FormatException) { }
            catch (Exception ex) when (ex.InnerException is NotSupportedException) { }

            // Incompatible type and no conversion available
            convertedDictionary = null;
            return false;
        }
    }
}
