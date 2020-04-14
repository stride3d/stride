// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.IO;
using System.Text;
using Stride.Core.Assets.TextAccessors;
using Stride.Core.Assets.Yaml;
using Stride.Core.Diagnostics;
using Stride.Core.IO;

namespace Stride.Core.Assets.Serializers
{
    internal class SourceCodeAssetSerializer : IAssetSerializer, IAssetSerializerFactory
    {
        public static readonly SourceCodeAssetSerializer Default = new SourceCodeAssetSerializer();

        public object Load(Stream stream, UFile filePath, ILogger log, bool clearBrokenObjectReferences, out bool aliasOccurred, out AttachedYamlAssetMetadata yamlMetadata)
        {
            aliasOccurred = false;

            var assetFileExtension = filePath.GetFileExtension().ToLowerInvariant();
            var type = AssetRegistry.GetAssetTypeFromFileExtension(assetFileExtension);
            var asset = (SourceCodeAsset)Activator.CreateInstance(type);

            var textAccessor = asset.TextAccessor as DefaultTextAccessor;
            if (textAccessor != null)
            {
                // Don't load the file if we have the file path
                textAccessor.FilePath = filePath;

                // Set the assets text if it loaded from an in-memory version
                // TODO: Propagate dirtiness?
                if (stream is MemoryStream)
                {
                    var reader = new StreamReader(stream, Encoding.UTF8);
                    textAccessor.Set(reader.ReadToEnd());
                }
            }

            // No override in source code assets
            yamlMetadata = new AttachedYamlAssetMetadata();
            return asset;
        }

        public void Save(Stream stream, object asset, AttachedYamlAssetMetadata yamlMetadata, ILogger log = null)
        {
            ((SourceCodeAsset)asset).Save(stream);
        }

        public IAssetSerializer TryCreate(string assetFileExtension)
        {
            var assetType = AssetRegistry.GetAssetTypeFromFileExtension(assetFileExtension);
            if (assetType != null && typeof(SourceCodeAsset).IsAssignableFrom(assetType))
            {
                return this;
            }
            return null;
        }
    }
}
