// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Quantum.Tests.Helpers;
using Stride.Core.Reflection;
using Xunit;

namespace Stride.Core.Assets.Quantum.Tests;

/// <summary>
/// Unit tests for <see cref="Visitors.OverrideTypePathGenerator"/> (exercised through
/// <see cref="AssetPropertyGraph.GenerateOverridesForSerialization"/>). When a derived asset is saved, the
/// override state of each member/item is emitted as YAML metadata so it can be restored on load. The generator
/// must emit exactly the overridden members and nothing for inherited ones.
/// </summary>
public class TestOverrideTypePathGenerator
{
    [Fact]
    public void TestNothingEmittedWhenAllInherited()
    {
        var context = DeriveAssetTest<Types.MyAsset1, Types.MyAssetBasePropertyGraph>.DeriveAsset(new Types.MyAsset1 { MyString = "String" });

        var overrides = AssetPropertyGraph.GenerateOverridesForSerialization(context.DerivedGraph.RootNode);

        Assert.Empty(overrides);
    }

    [Fact]
    public void TestOverriddenMemberIsEmittedAsNew()
    {
        var context = DeriveAssetTest<Types.MyAsset1, Types.MyAssetBasePropertyGraph>.DeriveAsset(new Types.MyAsset1 { MyString = "String" });
        context.DerivedGraph.RootNode[nameof(Types.MyAsset1.MyString)].Update("Derived");

        var overrides = AssetPropertyGraph.GenerateOverridesForSerialization(context.DerivedGraph.RootNode);

        var entry = Assert.Single(overrides);
        Assert.Equal(OverrideType.New, entry.Value);
    }
}
