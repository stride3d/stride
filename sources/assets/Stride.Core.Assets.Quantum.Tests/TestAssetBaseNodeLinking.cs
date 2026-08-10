// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Quantum.Tests.Helpers;
using Xunit;

namespace Stride.Core.Assets.Quantum.Tests;

/// <summary>
/// Unit tests for base-to-derived node linking. After a derived asset's graph is refreshed against its base
/// (Archetype), every derived <see cref="IAssetNode"/> exposes a <see cref="IAssetNode.BaseNode"/> pointing at
/// the matching node in the base graph. This linkage is what lets value changes propagate from base to derived
/// and what reconciliation relies on; here we assert the public, observable result of that wiring.
/// </summary>
public class TestAssetBaseNodeLinking
{
    [Fact]
    public void TestDerivedNodesLinkToMatchingBaseNodes()
    {
        var context = DeriveAssetTest<Types.MyAsset1, Types.MyAssetBasePropertyGraph>.DeriveAsset(new Types.MyAsset1 { MyString = "String" });

        // The derived root node is linked to the base root node.
        Assert.Same(context.BaseGraph.RootNode, context.DerivedGraph.RootNode.BaseNode);

        // And so is each derived member node.
        var baseMember = context.BaseGraph.RootNode[nameof(Types.MyAsset1.MyString)];
        var derivedMember = context.DerivedGraph.RootNode[nameof(Types.MyAsset1.MyString)];
        Assert.Same(baseMember, derivedMember.BaseNode);
    }

    [Fact]
    public void TestNonDerivedAssetHasNoBaseNode()
    {
        // An asset that was not derived from anything has no base to link to.
        var container = new AssetTestContainer<Types.MyAsset1, Types.MyAssetBasePropertyGraph>(new Types.MyAsset1 { MyString = "String" });
        container.BuildGraph();

        Assert.Null(container.Graph.RootNode.BaseNode);
        Assert.Null(container.Graph.RootNode[nameof(Types.MyAsset1.MyString)].BaseNode);
    }
}
