// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Quantum.Tests.Helpers;
using Xunit;

namespace Stride.Core.Assets.Quantum.Tests;

/// <summary>
/// Unit tests for <see cref="AssetQuantumRegistry"/>, the lookup that maps an asset type to its
/// <see cref="AssetPropertyGraph"/> implementation and its <see cref="AssetPropertyGraphDefinition"/>.
/// Resolution walks up the base-type chain and falls back to the default registered for <see cref="Asset"/>.
/// </summary>
public class TestAssetQuantumRegistry
{
    [Fact]
    public void TestGetDefinitionReturnsTypeSpecificDefinition()
    {
        // MyAssetWithRef declares [AssetPropertyGraphDefinition(typeof(MyAssetWithRef))].
        var definition = AssetQuantumRegistry.GetDefinition(typeof(Types.MyAssetWithRef));
        Assert.IsType<Types.AssetWithRefPropertyGraphDefinition>(definition);
    }

    [Fact]
    public void TestGetDefinitionFallsBackToAssetDefault()
    {
        // MyAsset1 and MyAsset2 have no dedicated definition, so both resolve to the single default
        // definition registered for the Asset base type.
        var definition1 = AssetQuantumRegistry.GetDefinition(typeof(Types.MyAsset1));
        var definition2 = AssetQuantumRegistry.GetDefinition(typeof(Types.MyAsset2));

        Assert.IsType<AssetPropertyGraphDefinition>(definition1);
        Assert.Same(definition1, definition2);
    }

    [Fact]
    public void TestGetDefinitionIsCached()
    {
        var first = AssetQuantumRegistry.GetDefinition(typeof(Types.MyAssetWithRef));
        var second = AssetQuantumRegistry.GetDefinition(typeof(Types.MyAssetWithRef));
        Assert.Same(first, second);
    }

    [Fact]
    public void TestGetDefinitionThrowsForNonAssetType()
    {
        Assert.Throws<ArgumentException>(() => AssetQuantumRegistry.GetDefinition(typeof(string)));
    }

    [Fact]
    public void TestConstructPropertyGraphResolvesGraphFromBaseType()
    {
        // MyAsset1 has no graph of its own; the graph registered for its MyAssetBase base type is used.
        var container = new AssetPropertyGraphContainer(new AssetNodeContainer { NodeBuilder = { NodeFactory = new AssetNodeFactory() } });
        var assetItem = new AssetItem("MyAsset", new Types.MyAsset1 { MyString = "s" });

        var graph = AssetQuantumRegistry.ConstructPropertyGraph(container, assetItem, null);

        Assert.IsType<Types.MyAssetBasePropertyGraph>(graph);
    }

    [Fact]
    public void TestConstructPropertyGraphResolvesGenericHierarchyGraph()
    {
        // MyAssetHierarchy : AssetCompositeHierarchy<...> declares its own graph type directly.
        var container = new AssetPropertyGraphContainer(new AssetNodeContainer { NodeBuilder = { NodeFactory = new AssetNodeFactory() } });
        var assetItem = new AssetItem("MyAsset", new Types.MyAssetHierarchy());

        var graph = AssetQuantumRegistry.ConstructPropertyGraph(container, assetItem, null);

        Assert.IsType<Types.MyAssetHierarchyPropertyGraph>(graph);
    }
}
