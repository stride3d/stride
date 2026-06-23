// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Quantum.Tests.Helpers;
using Stride.Core.Assets.Tests.Helpers;
using Stride.Core.Quantum;
using Xunit;

namespace Stride.Core.Assets.Quantum.Tests;

/// <summary>
/// Unit tests for collection item identity on <see cref="IAssetObjectNode"/>. Collections track a stable
/// <c>ItemId</c> per element (decoupled from the element's index) so that overrides and deletions on a derived
/// asset keep referring to the right element even when the base collection is reordered. These tests pin the
/// id↔index mapping and the deletion-as-override behavior that reconciliation depends on.
/// </summary>
public class TestAssetObjectNodeItemIds
{
    private static IAssetObjectNode BuildStringListNode(params string[] values)
    {
        var asset = new Types.MyAsset2();
        asset.MyStrings.AddRange(values);
        var container = new AssetTestContainer<Types.MyAsset2, Types.MyAssetBasePropertyGraph>(asset);
        container.BuildGraph();
        return container.Graph.RootNode[nameof(Types.MyAsset2.MyStrings)].Target!;
    }

    [Fact]
    public void TestIndexToIdRoundTrip()
    {
        var list = BuildStringListNode("a", "b", "c");

        var idB = list.IndexToId(new NodeIndex(1));
        Assert.True(list.HasId(idB));
        Assert.True(list.TryIndexToId(new NodeIndex(1), out var idBAgain));
        Assert.Equal(idB, idBAgain);
        Assert.True(list.TryIdToIndex(idB, out var index));
        Assert.Equal(new NodeIndex(1), index);

        // An id that does not belong to the collection resolves to nothing.
        Assert.False(list.TryIdToIndex(IdentifierGenerator.Get(999), out _));
        Assert.False(list.HasId(IdentifierGenerator.Get(999)));
    }

    [Fact]
    public void TestItemIdsStayBoundToValuesAcrossRemoval()
    {
        var list = BuildStringListNode("a", "b", "c");
        var idA = list.IndexToId(new NodeIndex(0));
        var idC = list.IndexToId(new NodeIndex(2));
        Assert.NotEqual(idA, idC);

        // Removing the first element shifts indices, but ids remain bound to their original values.
        list.Remove("a", new NodeIndex(0));

        Assert.False(list.HasId(idA));            // "a" (and its id) is gone
        Assert.True(list.HasId(idC));             // "c" still present
        Assert.Equal(new NodeIndex(1), list.IdToIndex(idC)); // "c" moved from index 2 to index 1
    }

    [Fact]
    public void TestRemovingInheritedItemIsTrackedAsDeletedOverride()
    {
        var baseAsset = new Types.MyAsset2();
        baseAsset.MyStrings.AddRange(["a", "b", "c"]);
        var context = DeriveAssetTest<Types.MyAsset2, Types.MyAssetBasePropertyGraph>.DeriveAsset(baseAsset);
        var baseList = context.BaseGraph.RootNode[nameof(Types.MyAsset2.MyStrings)].Target!;
        var derivedList = context.DerivedGraph.RootNode[nameof(Types.MyAsset2.MyStrings)].Target!;

        var removedId = baseList.IndexToId(new NodeIndex(2));

        // Removing an inherited item on the derived asset is recorded as a deletion override,
        // so it is not silently re-added when reconciling with the base.
        derivedList.Remove("c", new NodeIndex(2));
        Assert.True(derivedList.IsItemDeleted(removedId));
    }
}
