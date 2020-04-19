using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Stride.Core.Assets.Tests.Helpers;
using Stride.Core.Assets.Yaml;
using Stride.Core.Reflection;
using Stride.Core.Yaml;

namespace Stride.Core.Assets.Quantum.Tests.Helpers
{
    public static class SerializationHelper
    {
        public static readonly AssetId BaseId = (AssetId)GuidGenerator.Get(1);
        public static readonly AssetId DerivedId = (AssetId)GuidGenerator.Get(2);

        public static void SerializeAndCompare(AssetItem assetItem, AssetPropertyGraph graph, string expectedYaml, bool isDerived)
        {
            assetItem.Asset.Id = isDerived ? DerivedId : BaseId;
            Assert.Equal(isDerived, assetItem.Asset.Archetype != null);
            if (isDerived)
                assetItem.Asset.Archetype = new AssetReference(BaseId, assetItem.Asset.Archetype?.Location);
            graph.PrepareForSave(null, assetItem);
            var stream = new MemoryStream();
            AssetFileSerializer.Save(stream, assetItem.Asset, assetItem.YamlMetadata, null);
            stream.Position = 0;
            var streamReader = new StreamReader(stream);
            var yaml = streamReader.ReadToEnd();
            Assert.Equal(expectedYaml, yaml);
        }

        public static void SerializeAndCompare(object instance, YamlAssetMetadata<OverrideType> overrides, string expectedYaml)
        {
            var stream = new MemoryStream();
            var metadata = new AttachedYamlAssetMetadata();
            metadata.AttachMetadata(AssetObjectSerializerBackend.OverrideDictionaryKey, overrides);
            AssetFileSerializer.Default.Save(stream, instance, metadata, null);
            stream.Position = 0;
            var streamReader = new StreamReader(stream);
            var yaml = streamReader.ReadToEnd();
            Assert.Equal(expectedYaml, yaml);
        }
    }
}
