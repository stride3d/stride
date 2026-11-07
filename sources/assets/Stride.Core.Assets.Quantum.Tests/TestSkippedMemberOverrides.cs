// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;
using Stride.Core.Assets.Quantum.Tests.Helpers;
using Stride.Core.Quantum;

namespace Stride.Core.Assets.Quantum.Tests;

using SerializationHelper = Helpers.SerializationHelper;

/// <summary>
/// A member that its <c>ShouldSerialize</c> method skips when it holds nothing is still written when a derived asset
/// overrides something in it or deletes an item it inherits, which would otherwise be lost on reload.
/// </summary>
public class TestSkippedMemberOverrides
{
    [Fact]
    public void DefaultSettingsAreNotWritten()
    {
        var context = DeriveAssetTest<Types.MyAssetWithSkippedSettings, Types.MyAssetBasePropertyGraph>.DeriveAsset(new Types.MyAssetWithSkippedSettings(), deriveTwice: false);

        Assert.DoesNotContain("Settings:", Save(context.BaseAssetItem, context.BaseGraph, isDerived: false));
        Assert.DoesNotContain("Settings:", Save(context.DerivedAssetItem, context.DerivedGraph, isDerived: true));
    }

    [Fact]
    public void DeletedInheritedItemIsKept()
    {
        var asset = new Types.MyAssetWithSkippedSettings();
        asset.Settings.Strings.Add("Key1", "String1");
        var context = DeriveAssetTest<Types.MyAssetWithSkippedSettings, Types.MyAssetBasePropertyGraph>.DeriveAsset(asset, deriveTwice: false);
        var derivedStrings = context.DerivedGraph.RootNode[nameof(Types.MyAssetWithSkippedSettings.Settings)].Target[nameof(Types.SettingsSkippedWhenEmpty.Strings)];
        derivedStrings.Target.Remove("String1", new NodeIndex("Key1"));
        Assert.Empty(context.DerivedAsset.Settings.Strings);

        var reloaded = Reload(context);

        Assert.Equal("String1", reloaded.BaseAsset.Settings.Strings["Key1"]);
        Assert.Empty(reloaded.DerivedAsset.Settings.Strings);
    }

    [Fact]
    public void OverriddenNestedMemberIsKept()
    {
        var asset = new Types.MyAssetWithSkippedSettings();
        asset.Settings.Enabled = false;
        var context = DeriveAssetTest<Types.MyAssetWithSkippedSettings, Types.MyAssetBasePropertyGraph>.DeriveAsset(asset, deriveTwice: false);
        var derivedEnabled = context.DerivedGraph.RootNode[nameof(Types.MyAssetWithSkippedSettings.Settings)].Target[nameof(Types.SettingsSkippedWhenEmpty.Enabled)];
        derivedEnabled.Update(true);

        var reloaded = Reload(context);

        Assert.False(reloaded.BaseAsset.Settings.Enabled);
        Assert.True(reloaded.DerivedAsset.Settings.Enabled);
    }

    private static DeriveAssetTest<Types.MyAssetWithSkippedSettings, Types.MyAssetBasePropertyGraph> Reload(DeriveAssetTest<Types.MyAssetWithSkippedSettings, Types.MyAssetBasePropertyGraph> context)
    {
        var reloaded = DeriveAssetTest<Types.MyAssetWithSkippedSettings, Types.MyAssetBasePropertyGraph>.LoadFromYaml(
            Save(context.BaseAssetItem, context.BaseGraph, isDerived: false),
            Save(context.DerivedAssetItem, context.DerivedGraph, isDerived: true));
        // As a session load does: what the derived asset doesn't override or delete comes from the base
        reloaded.DerivedGraph.ReconcileWithBase();
        return reloaded;
    }

    private static string Save(AssetItem assetItem, AssetPropertyGraph graph, bool isDerived)
    {
        assetItem.Asset.Id = isDerived ? SerializationHelper.DerivedId : SerializationHelper.BaseId;
        if (isDerived)
            assetItem.Asset.Archetype = new AssetReference(SerializationHelper.BaseId, assetItem.Asset.Archetype?.Location);
        graph.PrepareForSave(null, assetItem);
        using var stream = new MemoryStream();
        AssetFileSerializer.Save(stream, assetItem.Asset, assetItem.YamlMetadata, null);
        return System.Text.Encoding.UTF8.GetString(stream.ToArray());
    }
}
