// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Editor.Tests.Helpers;
using Stride.Assets.Entities;
using Stride.Assets.Materials;
using Stride.Assets.Models;
using Stride.Assets.Presentation.ViewModels;
using Stride.Assets.Sprite;
using Stride.Assets.Textures;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.IO;
using Stride.Core.Presentation.Services;
using Stride.Core.Serialization;
using Stride.Engine;

namespace Stride.Assets.Editor.Tests;

public sealed class AssetViewModelTests
{
    [Fact]
    public async Task TheSessionCreatesTheViewModelOfEachAssetType()
    {
        var gameSettings = GameSettingsFactory.Create();
        gameSettings.SplashScreenTexture = null!;
        using var fixture = await AssetsSessionFixture.OpenAsync(initializeEditor: false,
            ("Material", new MaterialAsset()),
            ("Model", new ModelAsset { Source = new UFile("model.fbx") }),
            ("Sheet", new SpriteSheetAsset()),
            ("GameSettings", gameSettings),
            ("Scene", new SceneAsset()),
            ("Prefab", new PrefabAsset()),
            ("Texture", new TextureAsset { Source = new UFile("texture.png") }));

        Assert.False(fixture.Result.HasErrors, fixture.Log);
        Assert.IsType<MaterialViewModel>(fixture.Asset("Material"));
        Assert.IsType<ModelViewModel>(fixture.Asset("Model"));
        Assert.IsType<SpriteSheetViewModel>(fixture.Asset("Sheet"));
        Assert.IsType<GameSettingsViewModel>(fixture.Asset("GameSettings"));
        Assert.IsType<SceneViewModel>(fixture.Asset("Scene"));
        Assert.IsType<PrefabViewModel>(fixture.Asset("Prefab"));
        // No view model declares the texture asset: the generic one
        Assert.IsType<AssetViewModel<TextureAsset>>(fixture.Asset("Texture"));
    }

    [Fact]
    public async Task GameSettingsAreNeverLockedAndHaveNoDefaultSceneWithoutOne()
    {
        var gameSettings = GameSettingsFactory.Create();
        gameSettings.SplashScreenTexture = null!;
        using var fixture = await AssetsSessionFixture.OpenAsync(initializeEditor: false, ("GameSettings", gameSettings));

        var viewModel = fixture.Asset<GameSettingsViewModel>("GameSettings");

        Assert.False(viewModel.IsLocked);
        Assert.Null(viewModel.DefaultScene);
    }

    [Fact]
    public async Task ScenesAreParentedThroughTheirChildrenCollection()
    {
        using var fixture = await AssetsSessionFixture.OpenAsync(initializeEditor: false,
            ("Root", new SceneAsset()),
            ("Child", new SceneAsset()),
            ("GrandChild", new SceneAsset()));
        var root = fixture.Asset<SceneViewModel>("Root");
        var child = fixture.Asset<SceneViewModel>("Child");
        var grandChild = fixture.Asset<SceneViewModel>("GrandChild");

        // The relationship is written to the assets, so it is an undoable action
        using (fixture.ServiceProvider.Get<IUndoRedoService>().CreateTransaction())
        {
            root.Children.Add(child);
            child.Children.Add(grandChild);
        }

        Assert.Same(root, child.Parent);
        Assert.Same(child, grandChild.Parent);
        Assert.Equal([child.Id], root.Asset.ChildrenIds);
        Assert.Same(root, grandChild.GetRoot());
        Assert.True(root.CanBeParentOf(grandChild, out _));
        Assert.False(grandChild.CanBeParentOf(root, out var message));
        Assert.Equal("Circular scene hierarchy", message);
        Assert.False(root.CanBeParentOf(root, out _));
        Assert.False(root.CanBeParentOf(child, out _, checkSameParent: true));
    }

    [Fact]
    public async Task ScenesLoadTheirHierarchyFromTheAssets()
    {
        var root = new SceneAsset();
        var child = new SceneAsset();
        root.ChildrenIds.Add(child.Id);
        // Both sides of the relationship are saved: the child references its parent scene
        child.Parent = AttachedReferenceManager.CreateProxyObject<Scene>(root.Id, "Root");
        using var fixture = await AssetsSessionFixture.OpenAsync(initializeEditor: false, ("Root", root), ("Child", child));

        var rootViewModel = fixture.Asset<SceneViewModel>("Root");
        var childViewModel = fixture.Asset<SceneViewModel>("Child");

        Assert.Equal([childViewModel], rootViewModel.Children);
        Assert.Same(rootViewModel, childViewModel.Parent);
    }

    [Fact]
    public async Task ThePrefabHasNoPropertiesOfItsOwn()
    {
        using var fixture = await AssetsSessionFixture.OpenAsync(initializeEditor: false, ("Prefab", new PrefabAsset()));

        var prefab = fixture.Asset<PrefabViewModel>("Prefab");

        // The prefab's content is edited in the prefab editor, not in the property grid
        Assert.Null(((Stride.Core.Presentation.Quantum.IPropertyProviderViewModel)prefab).GetRootNode());
    }
}
