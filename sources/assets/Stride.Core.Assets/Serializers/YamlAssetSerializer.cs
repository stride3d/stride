// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Yaml;
using Stride.Core.Diagnostics;
using Stride.Core.IO;
using Stride.Core.Yaml;
using Stride.Core.Yaml.Serialization;

namespace Stride.Core.Assets.Serializers;

/// <summary>
/// Default serializer used for all Yaml content
/// </summary>
public class YamlAssetSerializer : IAssetSerializer, IAssetSerializerFactory
{
    public static AttachedYamlAssetMetadata CreateAndProcessMetadata(PropertyContainer yamlPropertyContainer, object deserializedObject, bool clearBrokenObjectReferences, ILogger? log = null)
    {
        var yamlMetadata = AttachedYamlAssetMetadata.FromPropertyContainer(yamlPropertyContainer);
        var objectReferences = yamlMetadata.RetrieveMetadata(AssetObjectSerializerBackend.ObjectReferencesKey);
        if (objectReferences != null)
        {
            // This metadata is consumed here, no need to return it as attached metadata
            FixupObjectReferences.RunFixupPass(deserializedObject, objectReferences, clearBrokenObjectReferences, false, log);
        }
        return yamlMetadata;
    }

    public object Load(Stream stream, UFile filePath, ILogger? log, bool clearBrokenObjectReferences, out bool aliasOccurred, out AttachedYamlAssetMetadata yamlMetadata)
    {
        var result = AssetYamlSerializer.Default.Deserialize(stream, null, log != null ? new SerializerContextSettings { Logger = log } : null, out aliasOccurred, out var properties);
        yamlMetadata = CreateAndProcessMetadata(properties, result, clearBrokenObjectReferences, log);
        return result;
    }

    public void Save(Stream stream, object asset, AttachedYamlAssetMetadata? yamlMetadata, ILogger? log = null, string? assetNamespace = null)
    {
        var settings = new SerializerContextSettings(log);
        if (assetNamespace is not null)
        {
            settings.Properties.Add(AssetObjectSerializerBackend.AssetNamespaceKey, assetNamespace);
        }
        // Serialization mutates the metadata paths in place (AssetPartCollectionSerializer rewrites
        // part collection indices between the guid keys and the serialized list indices). Work on a
        // copy so the asset's attached metadata keeps its canonical (guid-keyed) form.
        // A shallow copy is enough: FixupPaths builds fresh path objects rather than mutating the
        // existing ones, so the original entries stay valid.
        var overrides = Clone(yamlMetadata?.RetrieveMetadata(AssetObjectSerializerBackend.OverrideDictionaryKey));
        if (overrides != null)
        {
            settings.Properties.Add(AssetObjectSerializerBackend.OverrideDictionaryKey, overrides);
        }
        var objectReferences = Clone(yamlMetadata?.RetrieveMetadata(AssetObjectSerializerBackend.ObjectReferencesKey));
        if (objectReferences != null)
        {
            settings.Properties.Add(AssetObjectSerializerBackend.ObjectReferencesKey, objectReferences);
        }
        AssetYamlSerializer.Default.Serialize(stream, asset, null, settings);
    }

    private static YamlAssetMetadata<T>? Clone<T>(YamlAssetMetadata<T>? source)
    {
        if (source == null)
            return null;
        var copy = new YamlAssetMetadata<T>();
        foreach (var entry in source)
            copy.Set(entry.Key, entry.Value);
        return copy;
    }

    public IAssetSerializer TryCreate(string assetFileExtension)
    {
        return this;
    }
}
