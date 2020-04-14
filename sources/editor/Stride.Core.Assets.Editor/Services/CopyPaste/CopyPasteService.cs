// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Stride.Core.Assets.Quantum;
using Stride.Core.Assets.Quantum.Visitors;
using Stride.Core.Assets.Yaml;
using Stride.Core.Annotations;
using Stride.Core.Extensions;
using Stride.Core.Yaml;
using Stride.Core.Yaml.Events;

namespace Stride.Core.Assets.Editor.Services
{
    internal class CopyPasteService : ICopyPasteService
    {
        private readonly List<ICopyProcessor> copyProcessors = new List<ICopyProcessor>();
        private readonly List<IPasteProcessor> pasteProcessors = new List<IPasteProcessor>();
        private readonly List<IAssetPostPasteProcessor> postProcessors = new List<IAssetPostPasteProcessor>();

        public CopyPasteService()
        {
        }

        public CopyPasteService(AssetPropertyGraphContainer propertyGraphContainer)
        {
            // NOTE: this constructor is used through reflection by unit tests!
            PropertyGraphContainer = propertyGraphContainer;
        }

        /// <inheritdoc/>
        public IReadOnlyCollection<IAssetPostPasteProcessor> PostProcessors => postProcessors;

        /// <inheritdoc/>
        public AssetPropertyGraphContainer PropertyGraphContainer { get; set; }

        /// <inheritdoc/>
        public string CopyFromAsset(AssetPropertyGraph propertyGraph, AssetId? sourceId, object value, bool isObjectReference)
        {
            return value != null ? CopyFromAssets(new[] { (propertyGraph, sourceId, value, isObjectReference) }, value.GetType()) : null;
        }

        /// <inheritdoc/>
        public string CopyFromAssets(IReadOnlyList<(AssetPropertyGraph propertyGraph, AssetId? sourceId, object value, bool isObjectReference)> items, Type itemType)
        {
            if (items == null) throw new ArgumentNullException(nameof(items));
            if (itemType == null) throw new ArgumentNullException(nameof(itemType));

            if (items.Count == 0)
                return null;

            var data = new CopyPasteData
            {
                ItemType = GetTagFromType(itemType),
            };
            data.Items.AddRange(items.Select(x => new CopyPasteItem
            {
                Data = x.value,
                IsRootObjectReference = x.isObjectReference,
                SourceId = x.sourceId
            }));

            // Create a node for the data so we can generate the overrides
            var metadata = new AttachedYamlAssetMetadata();
            var rootNode = PropertyGraphContainer.NodeContainer?.GetOrCreateNode(data) as IAssetNode;
            if (rootNode != null)
            {
                // Generate missing collection item identifiers
                data.Items.ForEach(x => AssetCollectionItemIdHelper.GenerateMissingItemIds(x.Data));
                // Generate the overrides
                var overrides = AssetPropertyGraph.GenerateOverridesForSerialization(rootNode);
                metadata.AttachMetadata(AssetObjectSerializerBackend.OverrideDictionaryKey, overrides);

                var objectReferences = new YamlAssetMetadata<Guid>();
                for (var i = 0; i < items.Count; i++)
                {
                    var item = items[i];
                    if (item.propertyGraph == null)
                        continue;

                    var references = item.propertyGraph.GenerateObjectReferencesForSerialization(rootNode);
                    // If the root object must be fully copied, make sure the property graph didn't consider it as an object reference.
                    if (!item.isObjectReference)
                    {
                        var dataPath = GetRootDataPath();
                        dataPath.PushIndex(i);
                        dataPath.PushMember(nameof(CopyPasteItem.Data));
                        references.Remove(dataPath);
                    }
                    references.GetEnumerator().Enumerate().ForEach(kv => objectReferences.Set(kv.Key, kv.Value));
                }

                metadata.AttachMetadata(AssetObjectSerializerBackend.ObjectReferencesKey, objectReferences);
            }

            // Process the data before copying
            var processors = GetValidCopyProcessors(itemType).ToList();
            foreach (var item in data.Items)
            {
                foreach (var processor in processors)
                {
                    // We clone the object to copy to allow the processor to safely modify it if necessary. The original object is left unchanged.
                    var clone = AssetCloner.Clone(item.Data);
                    if (processor.Process(ref clone, metadata))
                    {
                        item.Data = clone;
                        break;
                    }
                }
            }

            return SerializeData(data, metadata);
        }

        /// <inheritdoc/>
        public string CopyMultipleAssets(object container)
        {
            if (container == null)
                return null;

            var type = container.GetType();
            var data = new CopyPasteData
            {
                ItemType = GetTagFromType(type),
                // TODO: items grouped by source ids
                Items =
                {
                    new CopyPasteItem
                    {
                        Data = container,
                    }
                }
            };

            // Create a node for the data so we can generate the overrides
            var metadata = new AttachedYamlAssetMetadata();
            var rootNode = PropertyGraphContainer.NodeContainer?.GetOrCreateNode(data);
            if (rootNode != null)
            {
                // Generate missing collection item identifiers
                data.Items.ForEach(x => AssetCollectionItemIdHelper.GenerateMissingItemIds(x.Data));
                var assets = AssetCollector.Collect(rootNode);
                // Generate the overrides
                var overrides = AssetPropertyGraph.GenerateOverridesForSerialization(rootNode);
                metadata.AttachMetadata(AssetObjectSerializerBackend.OverrideDictionaryKey, overrides);
                // Generate the references
                var objectReferences = new YamlAssetMetadata<Guid>();
                foreach (var asset in assets)
                {
                    var propertyGraph = PropertyGraphContainer.TryGetGraph(asset.Value.Id);
                    if (propertyGraph?.RootNode != null)
                    {
                        var assetObjectReferences = propertyGraph.GenerateObjectReferencesForSerialization(propertyGraph.RootNode);
                        foreach (var reference in assetObjectReferences)
                        {
                            var realPath = AssetNodeMetadataCollectorBase.ConvertPath(asset.Key).Append(reference.Key);
                            objectReferences.Set(realPath, reference.Value);
                        }
                    }
                }
                metadata.AttachMetadata(AssetObjectSerializerBackend.ObjectReferencesKey, objectReferences);
            }

            return SerializeData(data, metadata);
        }

        /// <inheritdoc/>
        public bool CanPaste(string text, Type targetRootType, Type targetMemberType, params Type[] expectedTypes)
        {
            if (targetRootType == null) throw new ArgumentNullException(nameof(targetRootType));

            if (string.IsNullOrEmpty(text))
                return false;

            var dataType = GetDataType(text);
            if (dataType == null)
                return false;

            if (expectedTypes.Length > 0 && !expectedTypes.Any(t => t.IsAssignableFrom(dataType)))
                return false;

            return GetValidPasteProcessors(targetRootType, targetMemberType, dataType).Any();
        }

        /// <inheritdoc/>
        public IPasteResult DeserializeCopiedData(string text, object targetObject, Type targetMemberType)
        {
            if (targetObject == null) throw new ArgumentNullException(nameof(targetObject));

            var result = new PasteResult();
            if (string.IsNullOrEmpty(text))
                return result;

            var dataType = GetDataType(text);
            if (dataType == null)
                return result;

            var data = DeserializeData(text);
            for (var i = 0; i < data.Items.Count; i++)
            {
                var item = data.Items[i];
                var resultItem = new PasteItem { Data = item.Data };
                result.AddItem(resultItem);

                var basePath = new YamlAssetPath();
                basePath.PushMember(nameof(CopyPasteData.Items));
                basePath.PushIndex(i);
                basePath.PushMember(nameof(CopyPasteItem.Data));

                foreach (var p in GetValidPasteProcessors(targetObject.GetType(), targetMemberType, dataType))
                {
                    if (p.ProcessDeserializedData(PropertyGraphContainer, targetObject, targetMemberType, ref resultItem.Data, item.IsRootObjectReference, item.SourceId, data.Overrides, basePath))
                    {
                        resultItem.Processor = p;
                        break;
                    }
                }
            }
            if (data.Overrides != null)
            {
                // Create a node for the data where we can apply the overrides
                if (PropertyGraphContainer.NodeContainer?.GetOrCreateNode(result) is IAssetNode rootNode)
                {
                    AssetPropertyGraph.ApplyOverrides(rootNode, data.Overrides);
                }
            }
            return result;
        }

        /// <inheritdoc/>
        public void RegisterProcessor(ICopyProcessor processor)
        {
            if (processor == null) throw new ArgumentNullException(nameof(processor));
            copyProcessors.Add(processor);
        }

        /// <inheritdoc/>
        public void RegisterProcessor(IPasteProcessor processor)
        {
            if (processor == null) throw new ArgumentNullException(nameof(processor));
            pasteProcessors.Add(processor);
        }
        /// <inheritdoc/>
        public void RegisterProcessor(IAssetPostPasteProcessor processor)
        {
            if (processor == null) throw new ArgumentNullException(nameof(processor));
            postProcessors.Add(processor);
        }

        /// <inheritdoc/>
        public void UnregisterProcessor(ICopyProcessor processor)
        {
            if (processor == null) throw new ArgumentNullException(nameof(processor));
            copyProcessors.Remove(processor);
        }

        /// <inheritdoc/>
        public void UnregisterProcessor(IPasteProcessor processor)
        {
            if (processor == null) throw new ArgumentNullException(nameof(processor));
            pasteProcessors.Remove(processor);
        }

        /// <inheritdoc/>
        public void UnregisterProcessor(IAssetPostPasteProcessor processor)
        {
            if (processor == null) throw new ArgumentNullException(nameof(processor));
            postProcessors.Remove(processor);
        }

        [NotNull]
        private static CopyPasteData DeserializeData(string text)
        {
            try
            {
                // First try to deserialize as usual: the data could have been encapsulated in a CopyPasteData
                var stream = new MemoryStream();
                var writer = new StreamWriter(stream);
                writer.Write(text);
                writer.Flush();
                stream.Position = 0;

                var deserializedData = AssetFileSerializer.Default.Load(stream, null, null, false, out bool _, out AttachedYamlAssetMetadata yamlMetadata);
                var objectReferences = yamlMetadata.RetrieveMetadata(AssetObjectSerializerBackend.ObjectReferencesKey);
                var result = deserializedData as CopyPasteData ?? new CopyPasteData
                {
                    ItemType = GetTagFromType(deserializedData.GetType()),
                    Items = { new CopyPasteItem { Data = deserializedData } }
                };
                result.Overrides = yamlMetadata.RetrieveMetadata(AssetObjectSerializerBackend.OverrideDictionaryKey);
                if (objectReferences != null)
                {
                    for (var i = 0; i < result.Items.Count; i++)
                    {
                        var item = result.Items[i];
                        var dataPath = GetRootDataPath();
                        dataPath.PushIndex(i);
                        dataPath.PushMember(nameof(CopyPasteItem.Data));
                        item.IsRootObjectReference = objectReferences.TryGet(dataPath) != Guid.Empty;
                    }
                }
                return result;
            }
            catch (YamlException)
            {
                // In case of a primitive type, we return the data as-is: the data is raw (was not encapsulated)
                return new CopyPasteData
                {
                    ItemType = GetTagFromType(typeof(string)),
                    Items = { new CopyPasteItem { Data = text } }
                };
            }
        }

        [NotNull]
        private static YamlAssetPath GetRootDataPath()
        {
            var dataPath = new YamlAssetPath();
            dataPath.PushMember(nameof(CopyPasteData.Items));
            return dataPath;
        }

        /// <summary>
        /// Gets the type of data contained in the serialized <paramref name="text"/>.
        /// </summary>
        /// <param name="text"></param>
        /// <returns>The type of data contained in the serialized <paramref name="text"/>.</returns>
        /// <remakrs>If the serialized text is not valid YAML, this method returns <see cref="string"/>.
        /// If the serialized text is valid YAML but the type cannot be determined, this method returns <see cref="object"/>.</remakrs>
        private static Type GetDataType([NotNull]string text)
        {
            try
            {
                var input = new StringReader(text);
                var reader = new EventReader(new Parser(input));
                // Check that we start by StreamStart and DocumentStart
                if (reader.Allow<StreamStart>() == null || reader.Allow<DocumentStart>() == null)
                    return typeof(string);

                // If we have a scalar or a mapping, we can check the tag and retrieve the type. Otherwise, assume string.
                var node = reader.Allow<Scalar>() ?? (NodeEvent)reader.Allow<MappingStart>();
                var typeFromTag = GetTypeFromTag(node?.Tag);
                if (typeFromTag == null)
                    return typeof(string);

                // Check that we have a CopyPasteData instance
                if (typeFromTag != typeof(CopyPasteData))
                    return typeFromTag;

                // Check that the first member is ItemType. Otherwise, assume string.
                var itemTypeEntry = reader.Expect<Scalar>();
                if (itemTypeEntry.Value != nameof(CopyPasteData.ItemType))
                    return typeof(string);

                var itemTypeValue = reader.Expect<Scalar>();
                return !string.IsNullOrEmpty(itemTypeValue.Value) ? GetTypeFromTag(itemTypeValue.Value) : typeof(string);
            }
            catch (YamlException)
            {
                return typeof(string);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static string GetTagFromType(Type type)
        {
            return AssetYamlSerializer.Default.GetSerializerSettings().TagTypeRegistry.TagFromType(type);
        }

        private static Type GetTypeFromTag(string tagName)
        {
            return AssetYamlSerializer.Default.GetSerializerSettings().TagTypeRegistry.TypeFromTag(tagName, out bool _);
        }

        [ItemNotNull, NotNull]
        private IEnumerable<ICopyProcessor> GetValidCopyProcessors([NotNull] Type dataType)
        {
            for (var i = copyProcessors.Count - 1; i >= 0; i--)
            {
                if (copyProcessors[i].Accept(dataType))
                    yield return copyProcessors[i];
            }
        }

        [ItemNotNull, NotNull]
        private IEnumerable<IPasteProcessor> GetValidPasteProcessors([NotNull] Type targetRootType, [NotNull] Type targetMemberType, [NotNull] Type pastedDataType)
        {
            for (var i = pasteProcessors.Count - 1; i >= 0; i--)
            {
                if (pasteProcessors[i].Accept(targetRootType, targetMemberType, pastedDataType))
                    yield return pasteProcessors[i];
            }
        }

        [NotNull]
        private static string SerializeData([NotNull] CopyPasteData data, [NotNull] AttachedYamlAssetMetadata metadata)
        {
            var stream = new MemoryStream();
            AssetFileSerializer.Default.Save(stream, data, metadata);
            stream.Position = 0;
            return new StreamReader(stream).ReadToEnd();
        }
    }
}
