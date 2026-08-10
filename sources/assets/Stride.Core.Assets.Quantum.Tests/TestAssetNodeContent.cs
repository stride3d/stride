// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Quantum.Tests.Helpers;
using Xunit;

namespace Stride.Core.Assets.Quantum.Tests;

/// <summary>
/// Unit tests for the attached-content side-channel on <see cref="IAssetNode"/>
/// (<see cref="IAssetNode.SetContent"/> / <see cref="IAssetNode.GetContent"/>). The asset/composite layers use
/// this to hang extra nodes off a node by key (for example linking a part node to its owning part) without
/// changing the asset's data model.
/// </summary>
public class TestAssetNodeContent
{
    [Fact]
    public void TestAttachedContentRoundTrips()
    {
        var container = new AssetTestContainer<Types.MyAsset1, Types.MyAssetBasePropertyGraph>(new Types.MyAsset1 { MyString = "s" });
        container.BuildGraph();
        var node = container.Graph.RootNode;
        var attached = container.Graph.RootNode[nameof(Types.MyAsset1.MyString)];

        // Unknown keys return null.
        Assert.Null(node.GetContent("MyKey"));

        // Attached content can be retrieved by the same key, and overwritten.
        node.SetContent("MyKey", attached);
        Assert.Same(attached, node.GetContent("MyKey"));
    }
}
