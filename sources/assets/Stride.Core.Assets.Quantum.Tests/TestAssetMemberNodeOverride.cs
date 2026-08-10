// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Quantum.Tests.Helpers;
using Stride.Core.Reflection;
using Xunit;

namespace Stride.Core.Assets.Quantum.Tests;

/// <summary>
/// Unit tests for the per-member override state on <see cref="IAssetMemberNode"/>. A derived asset (Archetype)
/// inherits its base's member values (<see cref="OverrideType.Base"/>); writing a value on the derived asset
/// turns that member into an override (<see cref="OverrideType.New"/>) that no longer follows the base, until
/// the override is reset. This is the heart of how the editor presents "inherited vs. overridden" properties.
/// </summary>
public class TestAssetMemberNodeOverride
{
    private static DeriveAssetTest<Types.MyAsset1, Types.MyAssetBasePropertyGraph> DeriveStringAsset(string value)
        => DeriveAssetTest<Types.MyAsset1, Types.MyAssetBasePropertyGraph>.DeriveAsset(new Types.MyAsset1 { MyString = value });

    [Fact]
    public void TestChangingDerivedMemberCreatesOverride()
    {
        var context = DeriveStringAsset("String");
        var baseNode = context.BaseGraph.RootNode[nameof(Types.MyAsset1.MyString)];
        var derivedNode = context.DerivedGraph.RootNode[nameof(Types.MyAsset1.MyString)];

        // A freshly derived asset inherits everything from its base.
        Assert.Equal(OverrideType.Base, derivedNode.GetContentOverride());
        Assert.True(derivedNode.IsContentInherited());
        Assert.False(derivedNode.IsContentOverridden());

        // Writing a value on the derived asset turns the member into an override.
        derivedNode.Update("Derived");
        Assert.Equal(OverrideType.New, derivedNode.GetContentOverride());
        Assert.True(derivedNode.IsContentOverridden());
        Assert.False(derivedNode.IsContentInherited());

        // The base is unaffected by the derived override.
        Assert.Equal(OverrideType.Base, baseNode.GetContentOverride());
        Assert.Equal("String", baseNode.Retrieve());
    }

    [Fact]
    public void TestOverriddenMemberStopsFollowingBaseChanges()
    {
        var context = DeriveStringAsset("String");
        var baseNode = context.BaseGraph.RootNode[nameof(Types.MyAsset1.MyString)];
        var derivedNode = context.DerivedGraph.RootNode[nameof(Types.MyAsset1.MyString)];

        // While inherited, a base change flows into the derived asset.
        baseNode.Update("BaseChanged");
        Assert.Equal("BaseChanged", derivedNode.Retrieve());

        // Once overridden, the derived value is independent of further base changes.
        derivedNode.Update("Derived");
        baseNode.Update("BaseChangedAgain");
        Assert.Equal("Derived", derivedNode.Retrieve());
        Assert.Equal(OverrideType.New, derivedNode.GetContentOverride());
    }

    [Fact]
    public void TestResetOverrideRestoresInheritance()
    {
        var context = DeriveStringAsset("String");
        var baseNode = context.BaseGraph.RootNode[nameof(Types.MyAsset1.MyString)];
        var derivedNode = context.DerivedGraph.RootNode[nameof(Types.MyAsset1.MyString)];

        derivedNode.Update("Derived");
        Assert.Equal(OverrideType.New, derivedNode.GetContentOverride());

        // Resetting the override makes the member inherit from the base again...
        derivedNode.ResetOverrideRecursively();
        Assert.Equal(OverrideType.Base, derivedNode.GetContentOverride());

        // ...so a subsequent base change once more flows through to the derived asset.
        baseNode.Update("AfterReset");
        Assert.Equal("AfterReset", derivedNode.Retrieve());
    }
}
