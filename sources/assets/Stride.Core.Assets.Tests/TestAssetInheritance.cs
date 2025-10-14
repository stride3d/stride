// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Serializers;
using Stride.Core.Reflection;
using Stride.Core.Yaml;
using Stride.Core.Yaml.Events;

namespace Stride.Core.Assets.Tests;

public class TestAssetInheritance
{
    private const string ObjectWithStringsDerivedYaml =
        """
        MyStrings:
            0a0000000a0000000a0000000a000000*: MyDerivedString
            14000000140000001400000014000000: MyBaseString
        """;

    [DataContract]
    public class ObjectWithStrings
    {
        public List<string> MyStrings { get; set; } = [];
    }

    [Fact]
    public void TestWithParts()
    {
        // Create a derivative asset with asset parts
        var project = new Package();
        var assets = new List<TestAssetWithParts>();
        var assetItems = new List<AssetItem>();

        assets.Add(new TestAssetWithParts()
        {
            Parts =
            {
                new AssetPartTestItem(Guid.NewGuid()),
                new AssetPartTestItem(Guid.NewGuid())
            }
        });
        assetItems.Add(new AssetItem("asset-0", assets[0]));
        project.Assets.Add(assetItems[0]);

        var childAsset = (TestAssetWithParts)assetItems[0].CreateDerivedAsset();

        // Check that child asset has a base
        Assert.NotNull(childAsset.Archetype);

        // Check base asset
        Assert.Equal(assets[0].Id, childAsset.Archetype.Id);

        // Check that base is correctly setup for the part
        var i = 0;
        var instanceId = Guid.Empty;
        foreach (var part in childAsset.Parts)
        {
            Assert.Equal(assets[0].Id, part.Base.BasePartAsset.Id);
            Assert.Equal(assets[0].Parts[i].Id, part.Base.BasePartId);
            if (instanceId == Guid.Empty)
                instanceId = part.Base.InstanceId;
            Assert.NotEqual(Guid.Empty, instanceId);
            Assert.Equal(instanceId, part.Base.InstanceId);
            ++i;
        }
    }

    [Fact]
    public void TestParsingEventReuse()
    {
        // This test checks if a List<ParsingEvent> can be read multiple times without loosing its overrides
        // Built as part of a GameStudio reloading issue raised in #2718
        
        var reader = new EventReader(new Parser(new StringReader(ObjectWithStringsDerivedYaml)));
        var parsingEvents = new List<ParsingEvent>();
        reader.ReadCurrent(parsingEvents);

        var eventReader = new EventReader(new MemoryParser(parsingEvents));
        PropertyContainer properties;
        var obj = AssetYamlSerializer.Default.Deserialize(eventReader, null, typeof(ObjectWithStrings), out properties);
        
        eventReader = new EventReader(new MemoryParser(parsingEvents));
        obj = AssetYamlSerializer.Default.Deserialize(eventReader, null, typeof(ObjectWithStrings), out properties);

        var metadata = YamlAssetSerializer.CreateAndProcessMetadata(properties, obj, false);

        var overrides = metadata.RetrieveMetadata(AssetObjectSerializerBackend.OverrideDictionaryKey);
        Assert.NotNull(overrides);
        Assert.Equal(1, overrides.Count);
        Assert.Equal("(object).MyStrings{0a0000000a0000000a0000000a000000}", overrides.First().Key.ToString());
        Assert.Equal(OverrideType.New, overrides.First().Value);
    }
}
