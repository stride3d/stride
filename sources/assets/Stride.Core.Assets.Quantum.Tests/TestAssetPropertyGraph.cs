// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xunit;
using Stride.Core.Assets.Quantum.Internal;
using Stride.Core.Assets.Quantum.Tests.Helpers;
using Stride.Core.Assets.Tests.Helpers;
using Stride.Core.Reflection;

namespace Stride.Core.Assets.Quantum.Tests
{
    public class TestAssetPropertyGraph
    {
        [Fact]
        public void TestSimpleConstruction()
        {
            var container = new AssetPropertyGraphContainer(new AssetNodeContainer { NodeBuilder = { NodeFactory = new AssetNodeFactory() } });
            var asset = new Types.MyAsset1 { MyString = "String" };
            var assetItem = new AssetItem("MyAsset", asset);
            var graph = AssetQuantumRegistry.ConstructPropertyGraph(container, assetItem, null);
            Assert.IsAssignableFrom<AssetObjectNode>(graph.RootNode);
        }

        [Fact]
        public void TestCollectionConstruction()
        {
            var container = new AssetPropertyGraphContainer(new AssetNodeContainer { NodeBuilder = { NodeFactory = new AssetNodeFactory() } });
            var asset = new Types.MyAsset2 { MyStrings = { "aaa", "bbb", "ccc" } };
            var assetItem = new AssetItem("MyAsset", asset);
            var graph = AssetQuantumRegistry.ConstructPropertyGraph(container, assetItem, null);
            Assert.IsAssignableFrom<AssetObjectNode>(graph.RootNode);
            CollectionItemIdentifiers ids;
            Assert.True(CollectionItemIdHelper.TryGetCollectionItemIds(asset.MyStrings, out ids));
            Assert.Equal(3, ids.KeyCount);
            Assert.Equal(0, ids.DeletedCount);
            Assert.True(ids.ContainsKey(0));
            Assert.True(ids.ContainsKey(1));
            Assert.True(ids.ContainsKey(2));
        }

        [Fact]
        public void TestNestedCollectionConstruction()
        {
            var container = new AssetPropertyGraphContainer(new AssetNodeContainer { NodeBuilder = { NodeFactory = new AssetNodeFactory() } });
            var asset = new Types.MyAsset7 { MyAsset2 = new Types.MyAsset2 { MyStrings = { "aaa", "bbb", "ccc" } } };
            var assetItem = new AssetItem("MyAsset", asset);
            var graph = AssetQuantumRegistry.ConstructPropertyGraph(container, assetItem, null);
            Assert.IsAssignableFrom<AssetObjectNode>(graph.RootNode);
            CollectionItemIdentifiers ids;
            Assert.True(CollectionItemIdHelper.TryGetCollectionItemIds(asset.MyAsset2.MyStrings, out ids));
            Assert.Equal(3, ids.KeyCount);
            Assert.Equal(0, ids.DeletedCount);
            Assert.True(ids.ContainsKey(0));
            Assert.True(ids.ContainsKey(1));
            Assert.True(ids.ContainsKey(2));
        }

        [Fact]
        public void TestCollectionItemIdentifierWithDuplicates()
        {
            var container = new AssetPropertyGraphContainer(new AssetNodeContainer { NodeBuilder = { NodeFactory = new AssetNodeFactory() } });
            var asset = new Types.MyAsset2 { MyStrings = { "aaa", "bbb", "ccc" } };
            var ids = CollectionItemIdHelper.GetCollectionItemIds(asset.MyStrings);
            ids.Add(0, IdentifierGenerator.Get(100));
            ids.Add(1, IdentifierGenerator.Get(200));
            ids.Add(2, IdentifierGenerator.Get(100));
            var assetItem = new AssetItem("MyAsset", asset);
            Assert.Equal(IdentifierGenerator.Get(100), ids[0]);
            Assert.Equal(IdentifierGenerator.Get(200), ids[1]);
            Assert.Equal(IdentifierGenerator.Get(100), ids[2]);
            var graph = AssetQuantumRegistry.ConstructPropertyGraph(container, assetItem, null);
            Assert.IsAssignableFrom<AssetObjectNode>(graph.RootNode);
            Assert.True(CollectionItemIdHelper.TryGetCollectionItemIds(asset.MyStrings, out ids));
            Assert.Equal(3, ids.KeyCount);
            Assert.Equal(0, ids.DeletedCount);
            Assert.Equal(IdentifierGenerator.Get(100), ids[0]);
            Assert.Equal(IdentifierGenerator.Get(200), ids[1]);
            Assert.NotEqual(IdentifierGenerator.Get(100), ids[2]);
            Assert.NotEqual(IdentifierGenerator.Get(200), ids[2]);
        }
    }
}
