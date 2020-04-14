using Xunit;
using Stride.Core.Assets.Quantum.Tests.Helpers;
using Stride.Core.Assets.Tests.Helpers;

namespace Stride.Core.Assets.Quantum.Tests
{
    public class TestAssetCompositeHierarchySerialization
    {
        const string SimpleHierarchyYaml = @"!MyAssetHierarchy
Id: 00000001-0001-0000-0100-000001000000
Tags: []
Hierarchy:
    RootParts:
        - ref!! 00000002-0002-0000-0200-000002000000
        - ref!! 00000001-0001-0000-0100-000001000000
    Parts:
        -   Part:
                Id: 00000001-0001-0000-0100-000001000000
                Children: []
        -   Part:
                Id: 00000002-0002-0000-0200-000002000000
                Children: []
";

        const string NestedHierarchyYaml = @"!MyAssetHierarchy
Id: 00000001-0001-0000-0100-000001000000
Tags: []
Hierarchy:
    RootParts:
        - ref!! 00000002-0002-0000-0200-000002000000
        - ref!! 00000001-0001-0000-0100-000001000000
    Parts:
        -   Part:
                Id: 00000001-0001-0000-0100-000001000000
                Children: []
        -   Part:
                Id: 00000002-0002-0000-0200-000002000000
                Children: []
        -   Part:
                Id: 00000003-0003-0000-0300-000003000000
                Children:
                    - ref!! 00000002-0002-0000-0200-000002000000
        -   Part:
                Id: 00000004-0004-0000-0400-000004000000
                Children:
                    - ref!! 00000001-0001-0000-0100-000001000000
";

        const string MissortedHierarchyYaml = @"!MyAssetHierarchy
Id: 00000001-0001-0000-0100-000001000000
Tags: []
Hierarchy:
    RootParts:
        - ref!! 00000002-0002-0000-0200-000002000000
        - ref!! 00000001-0001-0000-0100-000001000000
    Parts:
        -   Part:
                Id: 00000003-0003-0000-0300-000003000000
                Children:
                    - ref!! 00000002-0002-0000-0200-000002000000
        -   Part:
                Id: 00000002-0002-0000-0200-000002000000
                Children: []
        -   Part:
                Id: 00000001-0001-0000-0100-000001000000
                Children: []
        -   Part:
                Id: 00000004-0004-0000-0400-000004000000
                Children:
                    - ref!! 00000001-0001-0000-0100-000001000000
";

        [Fact]
        public void TestSimpleDeserialization()
        {
            var asset = AssetFileSerializer.Load<Types.MyAssetHierarchy>(AssetTestContainer.ToStream(SimpleHierarchyYaml), $"MyAsset{Types.FileExtension}");
            Assert.Equal(2, asset.Asset.Hierarchy.RootParts.Count);
            Assert.Equal(GuidGenerator.Get(2), asset.Asset.Hierarchy.RootParts[0].Id);
            Assert.Equal(GuidGenerator.Get(1), asset.Asset.Hierarchy.RootParts[1].Id);
            Assert.Equal(2, asset.Asset.Hierarchy.Parts.Count);
            Assert.True(asset.Asset.Hierarchy.Parts.ContainsKey(GuidGenerator.Get(1)));
            Assert.True(asset.Asset.Hierarchy.Parts.ContainsKey(GuidGenerator.Get(2)));
        }

        [Fact]
        public void TestSimpleSerialization()
        {
            //var asset = AssetFileSerializer.Load<Types.MyAssetHierarchy>(AssetTestContainer.ToStream(text), $"MyAsset{Types.FileExtension}");
            var asset = new Types.MyAssetHierarchy();
            asset.Hierarchy.Parts.Add(new Types.MyPartDesign { Part = new Types.MyPart { Id = GuidGenerator.Get(1) } });
            asset.Hierarchy.Parts.Add(new Types.MyPartDesign { Part = new Types.MyPart { Id = GuidGenerator.Get(2) } });
            asset.Hierarchy.RootParts.Add(asset.Hierarchy.Parts[GuidGenerator.Get(2)].Part);
            asset.Hierarchy.RootParts.Add(asset.Hierarchy.Parts[GuidGenerator.Get(1)].Part);
            var context = new AssetTestContainer<Types.MyAssetHierarchy, Types.MyAssetHierarchyPropertyGraph>(asset);
            context.BuildGraph();
            SerializationHelper.SerializeAndCompare(context.AssetItem, context.Graph, SimpleHierarchyYaml, false);
        }

        [Fact]
        public void TestNestedDeserialization()
        {
            var asset = AssetFileSerializer.Load<Types.MyAssetHierarchy>(AssetTestContainer.ToStream(NestedHierarchyYaml), $"MyAsset{Types.FileExtension}");
            Assert.Equal(2, asset.Asset.Hierarchy.RootParts.Count);
            Assert.Equal(GuidGenerator.Get(2), asset.Asset.Hierarchy.RootParts[0].Id);
            Assert.Equal(GuidGenerator.Get(1), asset.Asset.Hierarchy.RootParts[1].Id);
            Assert.Equal(4, asset.Asset.Hierarchy.Parts.Count);
            Assert.True(asset.Asset.Hierarchy.Parts.ContainsKey(GuidGenerator.Get(1)));
            Assert.True(asset.Asset.Hierarchy.Parts.ContainsKey(GuidGenerator.Get(2)));
            Assert.True(asset.Asset.Hierarchy.Parts.ContainsKey(GuidGenerator.Get(3)));
            Assert.True(asset.Asset.Hierarchy.Parts.ContainsKey(GuidGenerator.Get(4)));
            Assert.Single(asset.Asset.Hierarchy.Parts[GuidGenerator.Get(3)].Part.Children);
            Assert.Equal(asset.Asset.Hierarchy.Parts[GuidGenerator.Get(2)].Part, asset.Asset.Hierarchy.Parts[GuidGenerator.Get(3)].Part.Children[0]);
            Assert.Single(asset.Asset.Hierarchy.Parts[GuidGenerator.Get(4)].Part.Children);
            Assert.Equal(asset.Asset.Hierarchy.Parts[GuidGenerator.Get(1)].Part, asset.Asset.Hierarchy.Parts[GuidGenerator.Get(4)].Part.Children[0]);
        }

        [Fact]
        public void TestNestedSerialization()
        {
            //var asset = AssetFileSerializer.Load<Types.MyAssetHierarchy>(AssetTestContainer.ToStream(text), $"MyAsset{Types.FileExtension}");
            var asset = new Types.MyAssetHierarchy();
            asset.Hierarchy.Parts.Add(new Types.MyPartDesign { Part = new Types.MyPart { Id = GuidGenerator.Get(1) } });
            asset.Hierarchy.Parts.Add(new Types.MyPartDesign { Part = new Types.MyPart { Id = GuidGenerator.Get(2) } });
            asset.Hierarchy.Parts.Add(new Types.MyPartDesign { Part = new Types.MyPart { Id = GuidGenerator.Get(3), Children = { asset.Hierarchy.Parts[GuidGenerator.Get(2)].Part } } });
            asset.Hierarchy.Parts.Add(new Types.MyPartDesign { Part = new Types.MyPart { Id = GuidGenerator.Get(4), Children = { asset.Hierarchy.Parts[GuidGenerator.Get(1)].Part } } });
            asset.Hierarchy.RootParts.Add(asset.Hierarchy.Parts[GuidGenerator.Get(2)].Part);
            asset.Hierarchy.RootParts.Add(asset.Hierarchy.Parts[GuidGenerator.Get(1)].Part);
            var context = new AssetTestContainer<Types.MyAssetHierarchy, Types.MyAssetHierarchyPropertyGraph>(asset);
            context.BuildGraph();
            SerializationHelper.SerializeAndCompare(context.AssetItem, context.Graph, NestedHierarchyYaml, false);
        }

        [Fact]
        public void TestMissortedPartsDeserialization()
        {
            var asset = AssetFileSerializer.Load<Types.MyAssetHierarchy>(AssetTestContainer.ToStream(MissortedHierarchyYaml), $"MyAsset{Types.FileExtension}");
            Assert.Equal(2, asset.Asset.Hierarchy.RootParts.Count);
            Assert.Equal(GuidGenerator.Get(2), asset.Asset.Hierarchy.RootParts[0].Id);
            Assert.Equal(GuidGenerator.Get(1), asset.Asset.Hierarchy.RootParts[1].Id);
            Assert.Equal(4, asset.Asset.Hierarchy.Parts.Count);
            Assert.True(asset.Asset.Hierarchy.Parts.ContainsKey(GuidGenerator.Get(1)));
            Assert.True(asset.Asset.Hierarchy.Parts.ContainsKey(GuidGenerator.Get(2)));
            Assert.True(asset.Asset.Hierarchy.Parts.ContainsKey(GuidGenerator.Get(3)));
            Assert.True(asset.Asset.Hierarchy.Parts.ContainsKey(GuidGenerator.Get(4)));
            Assert.Single(asset.Asset.Hierarchy.Parts[GuidGenerator.Get(3)].Part.Children);
            Assert.Equal(asset.Asset.Hierarchy.Parts[GuidGenerator.Get(2)].Part, asset.Asset.Hierarchy.Parts[GuidGenerator.Get(3)].Part.Children[0]);
            Assert.Single(asset.Asset.Hierarchy.Parts[GuidGenerator.Get(4)].Part.Children);
            Assert.Equal(asset.Asset.Hierarchy.Parts[GuidGenerator.Get(1)].Part, asset.Asset.Hierarchy.Parts[GuidGenerator.Get(4)].Part.Children[0]);
        }
    }
}
