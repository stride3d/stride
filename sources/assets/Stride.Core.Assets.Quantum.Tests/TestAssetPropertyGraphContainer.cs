// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Quantum.Tests.Helpers;
using Stride.Core.Diagnostics;
using Xunit;

namespace Stride.Core.Assets.Quantum.Tests;

/// <summary>
/// Unit tests for <see cref="AssetPropertyGraphContainer"/>, the registry that owns the shared
/// <see cref="AssetNodeContainer"/> and tracks the <see cref="AssetPropertyGraph"/> of every loaded asset by id.
/// The editor uses it to find the graph of an asset (e.g. to resolve references between assets) and to toggle
/// whether base-asset changes propagate into derived assets.
/// </summary>
public class TestAssetPropertyGraphContainer
{
    private static AssetPropertyGraphContainer CreateContainer()
        => new(new AssetNodeContainer { NodeBuilder = { NodeFactory = new AssetNodeFactory() } });

    [Fact]
    public void TestInitializeAssetRegistersRetrievableGraph()
    {
        var container = CreateContainer();
        var assetItem = new AssetItem("MyAsset", new Types.MyAsset1 { MyString = "s" });

        var graph = container.InitializeAsset(assetItem, new LoggerResult());

        Assert.NotNull(graph);
        Assert.Same(graph, container.TryGetGraph(assetItem.Id));
    }

    [Fact]
    public void TestTryGetGraphReturnsNullForUnknownAsset()
    {
        var container = CreateContainer();
        Assert.Null(container.TryGetGraph(AssetId.New()));
    }

    [Fact]
    public void TestUnregisterGraphRemovesIt()
    {
        var container = CreateContainer();
        var assetItem = new AssetItem("MyAsset", new Types.MyAsset1 { MyString = "s" });
        var graph = container.InitializeAsset(assetItem, new LoggerResult());
        Assert.NotNull(graph);

        Assert.True(container.UnregisterGraph(graph.Id));
        Assert.Null(container.TryGetGraph(graph.Id));
        // Removing an already-removed (or never-registered) graph reports false.
        Assert.False(container.UnregisterGraph(graph.Id));
    }

    [Fact]
    public void TestRegisterGraphRejectsDuplicateId()
    {
        var container = CreateContainer();
        var assetItem = new AssetItem("MyAsset", new Types.MyAsset1 { MyString = "s" });
        container.InitializeAsset(assetItem, new LoggerResult());

        // Initializing the same asset twice would register a second graph under the same id.
        Assert.Throws<ArgumentException>(() => container.InitializeAsset(assetItem, new LoggerResult()));
    }

    [Fact]
    public void TestPropagateChangesFromBaseDefaultsToTrue()
    {
        var container = CreateContainer();
        Assert.True(container.PropagateChangesFromBase);

        container.PropagateChangesFromBase = false;
        Assert.False(container.PropagateChangesFromBase);
    }
}
