// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Quantum.Tests.Helpers;
using Stride.Core.Assets.Quantum.Visitors;
using Stride.Core.Assets.Tests.Helpers;
using Xunit;

namespace Stride.Core.Assets.Quantum.Tests;

/// <summary>
/// Unit tests for <see cref="IdentifiableObjectCollector"/>. It walks an asset graph and gathers the
/// <see cref="IIdentifiable"/> objects that the asset <em>owns</em> (reached through normal members), while
/// ignoring objects that are only <em>referenced</em> (object references). This is used, for example, when
/// cloning a sub-hierarchy to know which identifiable objects must get fresh ids.
/// </summary>
public class TestIdentifiableObjectCollector
{
    [Fact]
    public void TestCollectsOwnedIdentifiablesButNotReferencedOnes()
    {
        // In MyAssetWithRef2, the "Reference" member is an object reference while "NonReference" is owned.
        var owned = new Types.MyReferenceable { Id = GuidGenerator.Get(1), Value = "owned" };
        var referenced = new Types.MyReferenceable { Id = GuidGenerator.Get(2), Value = "referenced" };
        var asset = new Types.MyAssetWithRef2 { NonReference = owned, Reference = referenced };
        var container = new AssetTestContainer<Types.MyAssetWithRef2, Types.MyAssetBasePropertyGraph>(asset);
        container.BuildGraph();
        var definition = AssetQuantumRegistry.GetDefinition(typeof(Types.MyAssetWithRef2));

        var collected = IdentifiableObjectCollector.Collect(definition, container.Graph.RootNode);

        Assert.True(collected.ContainsKey(owned.Id));
        Assert.False(collected.ContainsKey(referenced.Id));
        Assert.Same(owned, collected[owned.Id]);
    }

    [Fact]
    public void TestCollectValidatesArguments()
    {
        var container = new AssetTestContainer<Types.MyAssetWithRef2, Types.MyAssetBasePropertyGraph>(new Types.MyAssetWithRef2());
        container.BuildGraph();
        var definition = AssetQuantumRegistry.GetDefinition(typeof(Types.MyAssetWithRef2));

        Assert.Throws<ArgumentNullException>(() => IdentifiableObjectCollector.Collect(null!, container.Graph.RootNode));
        Assert.Throws<ArgumentNullException>(() => IdentifiableObjectCollector.Collect(definition, null!));
    }
}
