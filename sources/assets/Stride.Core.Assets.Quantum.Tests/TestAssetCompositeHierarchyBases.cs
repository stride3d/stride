// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Linq;
using Xunit;
using Stride.Core.Assets.Quantum.Tests.Helpers;

namespace Stride.Core.Assets.Quantum.Tests
{
    public class TestAssetCompositeHierarchyBases
    {
        [Fact]
        public void TestSimplePropertyChangeInBase()
        {
            var baseAsset = AssetHierarchyHelper.BuildAssetContainer(1, 2, 1, null, x => x.Parts[x.RootParts.Single().Id].Part.Name = "BaseName");
            var derivedAsset = AssetHierarchyHelper.BuildAssetContainer(1, 2, 1, baseAsset.Container);
            var instances = baseAsset.Asset.CreatePartInstances();
            var baseRootId = baseAsset.Asset.Hierarchy.RootParts.Single().Id;
            var derivedRootId = instances.RootParts.Single().Id;
            derivedAsset.Graph.AddPartToAsset(instances.Parts, instances.Parts[derivedRootId], null, 1);
            derivedAsset.Graph.RefreshBase();
            Assert.Equal(2, derivedAsset.Asset.Hierarchy.RootParts.Count);
            Assert.Equal(baseRootId, derivedAsset.Asset.Hierarchy.Parts[derivedRootId].Base?.BasePartId);
            Assert.Equal("BaseName", derivedAsset.Asset.Hierarchy.Parts[derivedRootId].Part.Name);
            var baseRootPartNode = (IAssetObjectNode)baseAsset.Graph.Container.NodeContainer.GetNode(baseAsset.Asset.Hierarchy.Parts[baseRootId].Part);
            var derivedRootPartNode = (IAssetObjectNode)baseAsset.Graph.Container.NodeContainer.GetNode(derivedAsset.Asset.Hierarchy.Parts[derivedRootId].Part);
            baseRootPartNode[nameof(Types.MyPart.Name)].Update("NewBaseName");
            Assert.Equal("NewBaseName", derivedAsset.Asset.Hierarchy.Parts[derivedRootId].Part.Name);
            baseRootPartNode[nameof(Types.MyPart.Name)].Update("NewBaseName2");
            Assert.Equal("NewBaseName2", derivedAsset.Asset.Hierarchy.Parts[derivedRootId].Part.Name);
            derivedRootPartNode[nameof(Types.MyPart.Name)].Update("NewDerivedName");
            Assert.True(derivedRootPartNode[nameof(Types.MyPart.Name)].IsContentOverridden());
            Assert.Equal("NewDerivedName", derivedAsset.Asset.Hierarchy.Parts[derivedRootId].Part.Name);
            baseRootPartNode[nameof(Types.MyPart.Name)].Update("NewBaseName3");
            Assert.True(derivedRootPartNode[nameof(Types.MyPart.Name)].IsContentOverridden());
            Assert.Equal("NewDerivedName", derivedAsset.Asset.Hierarchy.Parts[derivedRootId].Part.Name);
        }

        [Fact]
        public void TestSimpleNestedPropertyChangeInBase()
        {
            var baseAsset = AssetHierarchyHelper.BuildAssetContainer(1, 2, 1, null, x => x.Parts[x.RootParts.Single().Id].Part.Object = new Types.SomeObject { Value = "BaseName" });
            var derivedAsset = AssetHierarchyHelper.BuildAssetContainer(1, 2, 1, baseAsset.Container);
            var instances = baseAsset.Asset.CreatePartInstances();
            var baseRootId = baseAsset.Asset.Hierarchy.RootParts.Single().Id;
            var derivedRootId = instances.RootParts.Single().Id;
            derivedAsset.Graph.AddPartToAsset(instances.Parts, instances.Parts[derivedRootId], null, 1);
            derivedAsset.Graph.RefreshBase();
            Assert.Equal(2, derivedAsset.Asset.Hierarchy.RootParts.Count);
            Assert.Equal(baseRootId, derivedAsset.Asset.Hierarchy.Parts[derivedRootId].Base?.BasePartId);
            Assert.Equal("BaseName", derivedAsset.Asset.Hierarchy.Parts[derivedRootId].Part.Object.Value);
            var baseRootPartNode = (IAssetObjectNode)baseAsset.Graph.Container.NodeContainer.GetNode(baseAsset.Asset.Hierarchy.Parts[baseRootId].Part.Object);
            var derivedRootPartNode = (IAssetObjectNode)baseAsset.Graph.Container.NodeContainer.GetNode(derivedAsset.Asset.Hierarchy.Parts[derivedRootId].Part.Object);
            baseRootPartNode[nameof(Types.SomeObject.Value)].Update("NewBaseName");
            Assert.Equal("NewBaseName", derivedAsset.Asset.Hierarchy.Parts[derivedRootId].Part.Object.Value);
            baseRootPartNode[nameof(Types.SomeObject.Value)].Update("NewBaseName2");
            Assert.Equal("NewBaseName2", derivedAsset.Asset.Hierarchy.Parts[derivedRootId].Part.Object.Value);
            derivedRootPartNode[nameof(Types.SomeObject.Value)].Update("NewDerivedName");
            Assert.True(derivedRootPartNode[nameof(Types.SomeObject.Value)].IsContentOverridden());
            Assert.Equal("NewDerivedName", derivedAsset.Asset.Hierarchy.Parts[derivedRootId].Part.Object.Value);
            baseRootPartNode[nameof(Types.SomeObject.Value)].Update("NewBaseName3");
            Assert.True(derivedRootPartNode[nameof(Types.SomeObject.Value)].IsContentOverridden());
            Assert.Equal("NewDerivedName", derivedAsset.Asset.Hierarchy.Parts[derivedRootId].Part.Object.Value);
        }

        [Fact(Skip = "Overriding an object does not override its member currently, so this test cannot pass")]
        public void TestObjectPropertyChangeInBase()
        {
            var baseAsset = AssetHierarchyHelper.BuildAssetContainer(1, 2, 1, null, x => x.Parts[x.RootParts.Single().Id].Part.Object = new Types.SomeObject { Value = "BaseName" });
            var derivedAsset = AssetHierarchyHelper.BuildAssetContainer(1, 2, 1, baseAsset.Container);
            var instances = baseAsset.Asset.CreatePartInstances();
            var baseRootId = baseAsset.Asset.Hierarchy.RootParts.Single().Id;
            var derivedRootId = instances.RootParts.Single().Id;
            derivedAsset.Graph.AddPartToAsset(instances.Parts, instances.Parts[derivedRootId], null, 1);
            derivedAsset.Graph.RefreshBase();
            Assert.Equal(2, derivedAsset.Asset.Hierarchy.RootParts.Count);
            Assert.Equal(baseRootId, derivedAsset.Asset.Hierarchy.Parts[derivedRootId].Base?.BasePartId);
            Assert.Equal("BaseName", derivedAsset.Asset.Hierarchy.Parts[derivedRootId].Part.Object.Value);
            var baseRootPartNode = (IAssetObjectNode)baseAsset.Graph.Container.NodeContainer.GetNode(baseAsset.Asset.Hierarchy.Parts[baseRootId].Part);
            baseRootPartNode[nameof(Types.MyPart.Object)].Update(new Types.SomeObject { Value = "NewBaseName" });
            Assert.Equal("NewBaseName", derivedAsset.Asset.Hierarchy.Parts[derivedRootId].Part.Object.Value);
            baseRootPartNode = (IAssetObjectNode)baseAsset.Graph.Container.NodeContainer.GetNode(baseAsset.Asset.Hierarchy.Parts[baseRootId].Part);
            baseRootPartNode[nameof(Types.MyPart.Object)].Update(new Types.SomeObject { Value = "NewBaseName2" });
            Assert.Equal("NewBaseName2", derivedAsset.Asset.Hierarchy.Parts[derivedRootId].Part.Object.Value);
            var derivedRootPartNode = (IAssetObjectNode)baseAsset.Graph.Container.NodeContainer.GetNode(derivedAsset.Asset.Hierarchy.Parts[derivedRootId].Part);
            derivedRootPartNode[nameof(Types.MyPart.Object)].Update(new Types.SomeObject { Value = "NewDerivedName" });
            Assert.True(derivedRootPartNode[nameof(Types.MyPart.Object)].IsContentOverridden());
            Assert.Equal("NewDerivedName", derivedAsset.Asset.Hierarchy.Parts[derivedRootId].Part.Object.Value);
            baseRootPartNode = (IAssetObjectNode)baseAsset.Graph.Container.NodeContainer.GetNode(baseAsset.Asset.Hierarchy.Parts[baseRootId].Part);
            derivedRootPartNode = (IAssetObjectNode)baseAsset.Graph.Container.NodeContainer.GetNode(derivedAsset.Asset.Hierarchy.Parts[derivedRootId].Part);
            baseRootPartNode[nameof(Types.MyPart.Object)].Update(new Types.SomeObject { Value = "NewBaseName3" });
            Assert.True(derivedRootPartNode[nameof(Types.MyPart.Object)].IsContentOverridden());
            Assert.Equal("NewDerivedName", derivedAsset.Asset.Hierarchy.Parts[derivedRootId].Part.Object.Value);
        }

        [Fact]
        public void TestMultiplePropertyChangesInBase()
        {
            var baseAsset = AssetHierarchyHelper.BuildAssetContainer(1, 2, 1, null, x => x.Parts[x.RootParts.Single().Id].Part.Name = "BaseName");
            var derivedAsset = AssetHierarchyHelper.BuildAssetContainer(1, 2, 1, baseAsset.Container);
            var instances = baseAsset.Asset.CreatePartInstances();
            var baseRootId = baseAsset.Asset.Hierarchy.RootParts.Single().Id;
            var derivedRootId = instances.RootParts.Single().Id;
            derivedAsset.Graph.AddPartToAsset(instances.Parts, instances.Parts[derivedRootId], null, 1);
            derivedAsset.Graph.RefreshBase();
            Assert.Equal(2, derivedAsset.Asset.Hierarchy.RootParts.Count);
            Assert.Equal(baseRootId, derivedAsset.Asset.Hierarchy.Parts[derivedRootId].Base?.BasePartId);
            Assert.Equal("BaseName", derivedAsset.Asset.Hierarchy.Parts[derivedRootId].Part.Name);
            var baseRootPartNode = (IAssetObjectNode)baseAsset.Graph.Container.NodeContainer.GetNode(baseAsset.Asset.Hierarchy.Parts[baseRootId].Part);
            baseRootPartNode[nameof(Types.MyPart.Object)].Update(new Types.SomeObject { Value = "NewBaseValue" });
            Assert.NotNull(derivedAsset.Asset.Hierarchy.Parts[derivedRootId].Part.Object);
            Assert.Equal("NewBaseValue", derivedAsset.Asset.Hierarchy.Parts[derivedRootId].Part.Object.Value);
            baseRootPartNode[nameof(Types.MyPart.Name)].Update("NewBaseName");
            Assert.Equal("NewBaseName", derivedAsset.Asset.Hierarchy.Parts[derivedRootId].Part.Name);
        }

    }
}
