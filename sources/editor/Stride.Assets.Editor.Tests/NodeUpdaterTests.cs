// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Editor.Quantum.NodePresenters.Updaters;
using Stride.Assets.Editor.Tests.Helpers;
using Stride.Assets.Materials;
using Stride.Assets.Models;
using Stride.Assets.Sprite;
using Stride.Core;
using Stride.Core.Assets.Editor.Quantum.NodePresenters.Commands;
using Stride.Core.IO;
using Stride.Core.Presentation.Quantum.ViewModels;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Rendering.Materials;

namespace Stride.Assets.Editor.Tests;

/// <summary>
/// The node updaters shape the property grid of the Stride assets: the tests build the properties of real assets of a session
/// with the updaters registered, as the editor does.
/// </summary>
public sealed class NodeUpdaterTests
{
    [Fact]
    public async Task GameSettingsConfigurationsAreNamedAfterTheirTypeAndCanBeAddedAndRemoved()
    {
        var gameSettings = GameSettingsFactory.Create();
        gameSettings.SplashScreenTexture = null!;
        using var fixture = await AssetsSessionFixture.OpenAsync(initializeEditor: false, ("GameSettings", gameSettings));
        fixture.Session.AssetViewProperties.RegisterNodePresenterUpdater(new GameSettingsAssetNodeUpdater());

        var root = await fixture.PropertiesOf(fixture.Asset("GameSettings"));

        var defaults = root.GetChild(nameof(GameSettingsAsset.Defaults));
        Assert.NotNull(defaults);
        Assert.NotNull(defaults.GetCommand(AddNewItemCommand.CommandName));
        Assert.NotNull(defaults.GetCommand(RemoveItemCommand.CommandName));
        var names = defaults.Children.Select(x => x.DisplayName).ToList();
        Assert.Contains(DisplayAttribute.GetDisplayName(typeof(RenderingSettings)), names);
        Assert.Contains(DisplayAttribute.GetDisplayName(typeof(EditorSettings)), names);
        // The configurations already present cannot be added a second time
        var addable = Assert.IsAssignableFrom<IEnumerable<AbstractNodeEntry>>(defaults.AssociatedData["AbstractNodeMatchingEntries"]);
        Assert.DoesNotContain(addable, x => x is AbstractNodeType type && type.Type == typeof(RenderingSettings));
    }

    [Fact]
    public async Task MaterialAttributesAreInlinedAndLayersAreNamed()
    {
        var material = new MaterialAsset();
        material.Layers.Add(new MaterialBlendLayer { Name = "Dirt" });
        material.Layers.Add(new MaterialBlendLayer());
        using var fixture = await AssetsSessionFixture.OpenAsync(initializeEditor: false, ("Material", material));
        fixture.Session.AssetViewProperties.RegisterNodePresenterUpdater(new MaterialAssetNodeUpdater());

        var root = await fixture.PropertiesOf(fixture.Asset("Material"));

        // The Attributes node is bypassed: it is hidden and its members show directly under the asset
        Assert.False(root.GetChild(nameof(MaterialAsset.Attributes))!.IsVisible);
        Assert.Empty(root.GetChild(nameof(MaterialAsset.Attributes))!.Children);
        // ... grouped by the categories of the attributes (Diffuse is a shading attribute)
        Assert.Contains(root.Children.SelectMany(x => x.Children), x => x.Name == nameof(MaterialAttributes.Diffuse));
        var layers = root.GetChild(nameof(MaterialAsset.Layers));
        Assert.NotNull(layers);
        Assert.Equal(["Dirt", "Layer 1"], layers.Children.Select(x => x.DisplayName));
        Assert.All(layers.Children, layer =>
        {
            Assert.False(layer.GetChild(nameof(MaterialBlendLayer.Name))!.IsVisible);
            Assert.NotNull(layer.GetCommand(RenameStringKeyCommand.CommandName));
        });
    }

    [Fact]
    public async Task ModelMaterialsShowTheirInstanceAndMorphOptionsFollowTheImport()
    {
        var model = new ModelAsset { Source = new UFile("model.fbx"), ImportMorphTargets = false };
        model.Materials.Add(new ModelMaterial { Name = "Skin" });
        model.Materials.Add(new ModelMaterial());
        using var fixture = await AssetsSessionFixture.OpenAsync(initializeEditor: false, ("Model", model));
        fixture.Session.AssetViewProperties.RegisterNodePresenterUpdater(new ModelAssetNodeUpdater());

        var root = await fixture.PropertiesOf(fixture.Asset("Model"));

        var materials = root.GetChild(nameof(ModelAsset.Materials));
        Assert.NotNull(materials);
        var visible = materials.Children.Where(x => x.IsVisible).ToList();
        Assert.Equal(["Skin", "Material 1"], visible.Select(x => x.DisplayName));
        Assert.All(visible, x => Assert.Equal(typeof(MaterialInstance), x.Type));
        // Without imported morph targets the delta sources cannot be chosen
        Assert.True(root.GetChild(nameof(ModelAsset.MorphNormals))!.IsReadOnly);
        Assert.True(root.GetChild(nameof(ModelAsset.MorphTangents))!.IsReadOnly);
        // Without a skeleton the import transform is editable
        Assert.True(root.GetChild(nameof(ModelAsset.PivotPosition))!.IsVisible);
        Assert.True(root.GetChild(nameof(ModelAsset.ScaleImport))!.IsVisible);
    }

    [Theory]
    [InlineData(SpriteSheetType.Sprite2D, nameof(SpriteInfo.Borders), nameof(SpriteInfo.Center))]
    [InlineData(SpriteSheetType.UI, nameof(SpriteInfo.Center), nameof(SpriteInfo.Borders))]
    public async Task SpriteSheetSpritesShowTheMembersOfTheirKind(SpriteSheetType type, string hidden, string shown)
    {
        var sheet = new SpriteSheetAsset { Type = type };
        sheet.Sprites.Add(new SpriteInfo { Name = "Sprite" });
        using var fixture = await AssetsSessionFixture.OpenAsync(initializeEditor: false, ("Sheet", sheet));
        fixture.Session.AssetViewProperties.RegisterNodePresenterUpdater(new SpriteSheetAssetNodeUpdater());

        var root = await fixture.PropertiesOf(fixture.Asset("Sheet"));

        // The sprites are edited in the sprite sheet editor, not in the asset's property grid
        var sprites = root.GetChild(nameof(SpriteSheetAsset.Sprites));
        Assert.NotNull(sprites);
        Assert.False(sprites.IsVisible);
        var sprite = Assert.Single(sprites.Children);
        Assert.False(sprite.GetChild(hidden)!.IsVisible);
        Assert.True(sprite.GetChild(shown)!.IsVisible);
    }

    [Fact]
    public async Task TheEditorPluginRegistersItsUpdatersWithTheSession()
    {
        var gameSettings = GameSettingsFactory.Create();
        gameSettings.SplashScreenTexture = null!;
        using var fixture = await AssetsSessionFixture.OpenAsync(initializeEditor: true, ("GameSettings", gameSettings));

        var root = await fixture.PropertiesOf(fixture.Asset("GameSettings"));

        // The game settings updater ran: the configurations carry their display names
        var defaults = root.GetChild(nameof(GameSettingsAsset.Defaults));
        Assert.NotNull(defaults);
        Assert.Contains(defaults.Children, x => x.DisplayName == DisplayAttribute.GetDisplayName(typeof(RenderingSettings)));
    }
}
