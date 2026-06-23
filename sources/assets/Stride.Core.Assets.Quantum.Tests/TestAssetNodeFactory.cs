// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Quantum.Internal;
using Stride.Core.Assets.Quantum.Tests.Helpers;
using Xunit;

namespace Stride.Core.Assets.Quantum.Tests;

/// <summary>
/// Unit tests for <see cref="AssetNodeFactory"/>. When a graph is built through an
/// <see cref="AssetNodeContainer"/>, every node must be an asset-aware node (so it can carry override state
/// and a back-reference to its <see cref="AssetPropertyGraph"/>): objects become <see cref="AssetObjectNode"/>,
/// members become <see cref="AssetMemberNode"/>, and boxed value types become <see cref="AssetBoxedNode"/>.
/// </summary>
public class TestAssetNodeFactory
{
    [Fact]
    public void TestObjectAndMemberNodesAreAssetNodes()
    {
        var asset = new Types.MyAsset9 { MyObject = new Types.SomeObject { Value = "v" } };
        var container = new AssetTestContainer<Types.MyAsset9, Types.MyAssetBasePropertyGraph>(asset);
        container.BuildGraph();

        var rootNode = container.Graph.RootNode;
        Assert.IsAssignableFrom<AssetObjectNode>(rootNode);
        Assert.IsAssignableFrom<IAssetObjectNode>(rootNode);
        Assert.NotEqual(Guid.Empty, rootNode.Guid);

        var memberNode = rootNode[nameof(Types.MyAsset9.MyObject)];
        Assert.IsAssignableFrom<AssetMemberNode>(memberNode);
        Assert.IsAssignableFrom<IAssetMemberNode>(memberNode);

        // The referenced SomeObject is itself reached through an object node.
        Assert.IsAssignableFrom<AssetObjectNode>(memberNode.Target);
    }

    [Fact]
    public void TestStructMemberTargetIsBoxedNode()
    {
        var asset = new Types.MyAssetWithStructWithPrimitives { StructValue = new Types.StructWithPrimitives { Value1 = 1, Value2 = 2 } };
        var container = new AssetTestContainer<Types.MyAssetWithStructWithPrimitives, Types.MyAssetBasePropertyGraph>(asset);
        container.BuildGraph();

        var memberNode = container.Graph.RootNode[nameof(Types.MyAssetWithStructWithPrimitives.StructValue)];
        Assert.IsAssignableFrom<AssetMemberNode>(memberNode);

        // A struct is boxed to be represented as a reference target in the graph.
        Assert.IsAssignableFrom<AssetBoxedNode>(memberNode.Target);
        Assert.Equal(typeof(Types.StructWithPrimitives), memberNode.Target.Type);
    }
}
