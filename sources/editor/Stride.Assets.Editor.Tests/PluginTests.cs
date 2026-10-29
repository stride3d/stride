// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Editor.Preview;
using Stride.Assets.Editor.Tests.Helpers;
using Stride.Assets.Editor.ViewModels.Preview;
using Stride.Assets.Entities;
using Stride.Assets.Materials;
using Stride.Assets.Presentation;
using Stride.Assets.Presentation.Preview;
using Stride.Assets.Presentation.ViewModels;
using Stride.Assets.Textures;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Diagnostics;
using Stride.Core.Serialization;
using Stride.Editor.Build;
using Stride.Editor.Thumbnails;

namespace Stride.Assets.Editor.Tests;

public sealed class PluginTests
{
    [Fact]
    public void TheModulesRegisterTheDefaultAssetsAndEditorPlugins()
    {
        Assert.Contains(AssetsPlugin.RegisteredPlugins, x => x is StrideDefaultAssetsPlugin);
        Assert.Contains(AssetsPlugin.RegisteredPlugins, x => x is StrideEditorPlugin);
    }

    [Fact]
    public void TheEditorPluginUnregistersAndRegistersWithItsAssembly()
    {
        var assembly = typeof(StrideEditorPlugin).Assembly;

        var removed = AssetsPlugin.UnregisterPlugins(assembly);
        try
        {
            Assert.Single(removed, x => x is StrideEditorPlugin);
            Assert.DoesNotContain(AssetsPlugin.RegisteredPlugins, x => x is StrideEditorPlugin);
        }
        finally
        {
            var added = AssetsPlugin.RegisterPlugins(assembly);
            Assert.Single(added, x => x is StrideEditorPlugin);
        }
        Assert.Single(AssetsPlugin.RegisteredPlugins, x => x is StrideEditorPlugin);
        // Registering again adds nothing
        Assert.Empty(AssetsPlugin.RegisterPlugins(assembly));
    }

    [Fact]
    public void TheDefaultAssetsPluginDeclaresTheAssetViewModels()
    {
        var types = PluginRegistrations.Of(new StrideDefaultAssetsPlugin(), typeof(Stride.Core.Assets.Editor.Editors.IAssetEditorView)).AssetViewModelTypes;

        Assert.Equal(typeof(MaterialViewModel), types[typeof(MaterialAsset)]);
        Assert.Equal(typeof(SceneViewModel), types[typeof(SceneAsset)]);
        Assert.Equal(typeof(PrefabViewModel), types[typeof(PrefabAsset)]);
        Assert.Equal(typeof(GameSettingsViewModel), types[typeof(GameSettingsAsset)]);
        Assert.False(types.ContainsKey(typeof(TextureAsset)));
    }

    [Fact]
    public void TheDefaultAssetsPluginDeclaresTheReferencesAsPrimitiveTypes()
    {
        var types = new List<Type>();

        new StrideDefaultAssetsPlugin().RegisterPrimitiveTypes(types);

        Assert.Contains(typeof(AssetReference), types);
        Assert.Contains(typeof(UrlReferenceBase), types);
    }

    [Fact]
    public void TheEditorPluginDeclaresTheContentReferencesAsPrimitiveTypes()
    {
        var types = new List<Type>();

        new StrideEditorPlugin().RegisterPrimitiveTypes(types);

        // The content types serialized as references are not expanded by the property grid
        Assert.Contains(typeof(Stride.Graphics.Texture), types);
        Assert.Contains(typeof(Stride.Rendering.Model), types);
    }

    [Fact]
    public void TheEditorPluginDeclaresThePreviewViewModels()
    {
        var types = PluginRegistrations.Of(new StrideEditorPlugin(), typeof(Stride.Core.Assets.Editor.Editors.IAssetEditorView)).AssetPreviewViewModelTypes;

        Assert.Equal(typeof(TexturePreviewViewModel), types[typeof(TexturePreview)]);
        Assert.Equal(typeof(AnimationPreviewViewModel), types[typeof(AnimationPreview)]);
        Assert.Equal(typeof(MaterialPreviewViewModel), types[typeof(MaterialPreview)]);
        Assert.Equal(typeof(ModelPreviewViewModel), types[typeof(ModelPreview)]);
        Assert.Equal(typeof(SpriteSheetPreviewViewModel), types[typeof(SpriteSheetPreview)]);
        // Any sprite batch preview without a view model of its own gets the generic one
        Assert.Equal(typeof(SpritePreviewViewModel), types[typeof(PreviewFromSpriteBatch<>)]);
    }

    [Fact]
    public void TheEditorPluginDeclaresThePreviews()
    {
        // The previews register through the assets assemblies; the editor's preview service collects them by attribute
        var previews = Stride.Core.Reflection.AssemblyRegistry.GetScanTypes(typeof(StrideEditorPlugin).Assembly, typeof(Stride.Editor.Preview.IAssetPreview)).ToList();

        Assert.Contains(typeof(TexturePreview), previews);
        Assert.Contains(typeof(ScenePreview), previews);
        Assert.Contains(typeof(SkyboxPreview), previews);
    }

    [Fact]
    public void TheEditorPluginCollectsTheGizmosOfTheAssetAssemblies()
    {
        var plugin = new StrideEditorPlugin();

        plugin.InitializePlugin(new LoggerResult());

        // The shared editor assembly declares the gizmos of the engine components
        Assert.Equal(typeof(Stride.Assets.Presentation.AssetEditors.Gizmos.CameraGizmo), StrideEditorPlugin.GizmoTypeDictionary[typeof(Stride.Engine.CameraComponent)]);
        Assert.Equal(typeof(Stride.Assets.Presentation.AssetEditors.Gizmos.DispatcherLightGizmo), StrideEditorPlugin.GizmoTypeDictionary[typeof(Stride.Engine.LightComponent)]);
        // The engine's entity factories and add-asset policies still live in the WPF assembly: only the plugin packages
        // bring some, and none is loaded here
        Assert.Empty(StrideEditorPlugin.EntityFactoryCategories);
        Assert.Empty(StrideEditorPlugin.AddAssetPolicyTypeList);
        Assert.Equal(2, plugin.ProfileSettings.Count);
    }

    [Fact]
    public async Task TheEditorPluginRegistersTheBuildServicesOfTheSession()
    {
        var gameSettings = GameSettingsFactory.Create();
        gameSettings.SplashScreenTexture = null!;
        using var fixture = await AssetsSessionFixture.OpenAsync(initializeEditor: true, ("GameSettings", gameSettings));

        Assert.NotNull(fixture.ServiceProvider.TryGet<GameSettingsProviderService>());
        var builder = fixture.ServiceProvider.TryGet<GameStudioBuilderService>();
        Assert.NotNull(builder);
        Assert.Same(builder.Database, fixture.ServiceProvider.TryGet<GameStudioDatabase>());
        // The builder imports the shaders of the session first, with the Stride shader importer
        Assert.NotNull(builder.ShaderImportProgress);
        await builder.WaitForShadersAsync().WaitAsync(TimeSpan.FromSeconds(30));
        // The thumbnail service is off in this fixture (no graphics device)
        Assert.Null(fixture.ServiceProvider.TryGet<GameStudioThumbnailService>());
        // The build directory lives under the package that owns the game settings
        var expected = Path.Combine(fixture.PackageDirectory, "obj", "stride", "assetbuild", "data");
        Assert.Equal(Path.GetFullPath(expected), Path.GetFullPath(StrideEditorPlugin.GetBuildDirectory(fixture.Session)));
        Assert.True(Directory.Exists(expected));
    }

    [Fact]
    public void TheBuildCacheCollectionLeavesAFreshCacheAlone()
    {
        var directory = Path.Combine(Path.GetTempPath(), "Stride.Assets.Editor.Tests", Guid.NewGuid().ToString("N"));
        var blobs = Path.Combine(directory, "db", "ab");
        Directory.CreateDirectory(blobs);
        var fresh = Path.Combine(blobs, "abcdef");
        File.WriteAllText(fresh, "fresh");
        try
        {
            StrideEditorPlugin.CollectBuildCache(directory);

            Assert.True(File.Exists(fresh));
            // A marker gates the next collection
            Assert.True(File.Exists(Path.Combine(directory, "gc.stamp")));
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void TheBuildCacheCollectionRemovesStaleBlobs()
    {
        var directory = Path.Combine(Path.GetTempPath(), "Stride.Assets.Editor.Tests", Guid.NewGuid().ToString("N"));
        var blobs = Path.Combine(directory, "db", "ab");
        Directory.CreateDirectory(blobs);
        var stale = Path.Combine(blobs, "abcdef");
        File.WriteAllText(stale, "stale");
        File.SetLastWriteTimeUtc(stale, DateTime.UtcNow - TimeSpan.FromDays(365));
        var fresh = Path.Combine(blobs, "abcd00");
        File.WriteAllText(fresh, "fresh");
        try
        {
            StrideEditorPlugin.CollectBuildCache(directory);

            Assert.False(File.Exists(stale));
            Assert.True(File.Exists(fresh));
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }
}
