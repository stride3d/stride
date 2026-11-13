// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Editor.EditorGame;
using Stride.Assets.Editor.Preview;
using Stride.Assets.Editor.Tests.Helpers;
using Stride.Assets.Editor.ViewModels;
using Stride.Assets.Editor.ViewModels.Preview;
using Stride.Assets.Effect;
using Stride.Assets.Entities;
using Stride.Assets.Materials;
using Stride.Assets.Models;
using Stride.Assets.Presentation;
using Stride.Assets.Presentation.ViewModels;
using Stride.Assets.Scripts;
using Stride.Assets.Sprite;
using Stride.Assets.Textures;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Diagnostics;
using Stride.Core.Serialization;
using Stride.Editor.Build;
using Stride.Editor.Thumbnails;
using Stride.Shaders.Compiler;

namespace Stride.Assets.Editor.Tests;

public sealed class PluginTests
{
    [Fact]
    public void TheDefaultAssetsAndEditorPluginsAreLeftToTheirHost()
    {
        // The host registers them (here the test module, in the editor Stride.Assets.Editor.Avalonia); the discovery skips them
        Assert.Contains(AssetsPlugin.RegisteredPlugins, x => x is StrideDefaultAssetsPlugin);
        Assert.Contains(AssetsPlugin.RegisteredPlugins, x => x is StrideEditorPlugin);
        Assert.False(AssetsPlugin.IsDiscoverable(typeof(StrideDefaultAssetsPlugin)));
        Assert.False(AssetsPlugin.IsDiscoverable(typeof(StrideEditorPlugin)));
    }

    [Fact]
    public void TheDefaultPluginsUnregisterWithTheirAssemblyAndOnlyTheirHostRegistersThem()
    {
        // Both default plugins live in Stride.Assets.Editor
        var assembly = typeof(StrideEditorPlugin).Assembly;
        Assert.Same(assembly, typeof(StrideDefaultAssetsPlugin).Assembly);

        var removed = AssetsPlugin.UnregisterPlugins(assembly);
        try
        {
            Assert.Single(removed, x => x is StrideEditorPlugin);
            Assert.Single(removed, x => x is StrideDefaultAssetsPlugin);
            Assert.DoesNotContain(AssetsPlugin.RegisteredPlugins, x => x is StrideEditorPlugin or StrideDefaultAssetsPlugin);
            // The discovery of the assembly does not bring the default plugins back
            Assert.DoesNotContain(AssetsPlugin.RegisterPlugins(assembly), x => x is StrideEditorPlugin or StrideDefaultAssetsPlugin);
        }
        finally
        {
            // In the order of their host
            if (!AssetsPlugin.RegisteredPlugins.Any(x => x is StrideDefaultAssetsPlugin))
                AssetsPlugin.RegisterPlugin(typeof(StrideDefaultAssetsPlugin));
            if (!AssetsPlugin.RegisteredPlugins.Any(x => x is StrideEditorPlugin))
                AssetsPlugin.RegisterPlugin(typeof(StrideEditorPlugin));
        }
        Assert.Single(AssetsPlugin.RegisteredPlugins, x => x is StrideDefaultAssetsPlugin);
        Assert.Single(AssetsPlugin.RegisteredPlugins, x => x is StrideEditorPlugin);
    }

    [Fact]
    public void TheDefaultAssetsPluginDeclaresTheAssetViewModels()
    {
        var types = PluginRegistrations.Of(new StrideDefaultAssetsPlugin(), typeof(Stride.Core.Assets.Editor.Editors.IAssetEditorView)).AssetViewModelTypes;

        Assert.Equal(typeof(ScriptSourceFileViewModel), types[typeof(ScriptSourceFileAsset)]);
        // The view models that both editors share live in the same assembly (the WPF editor registers them itself)
        Assert.Equal(typeof(MaterialViewModel), types[typeof(MaterialAsset)]);
        Assert.Equal(typeof(ModelViewModel), types[typeof(ModelAsset)]);
        Assert.Equal(typeof(SpriteSheetViewModel), types[typeof(SpriteSheetAsset)]);
        Assert.Equal(typeof(SceneViewModel), types[typeof(SceneAsset)]);
        Assert.Equal(typeof(PrefabViewModel), types[typeof(PrefabAsset)]);
        Assert.Equal(typeof(GameSettingsViewModel), types[typeof(GameSettingsAsset)]);
        Assert.Equal(typeof(VisualScriptViewModel), types[typeof(VisualScriptAsset)]);
        Assert.False(types.ContainsKey(typeof(TextureAsset)));
        // The editor plugin declares none: its assembly has no asset view model
        Assert.Empty(PluginRegistrations.Of(new StrideEditorPlugin(), typeof(Stride.Core.Assets.Editor.Editors.IAssetEditorView)).AssetViewModelTypes);
    }

    [Fact]
    public void TheDefaultAssetsPluginDeclaresTheReferencesAsPrimitiveTypes()
    {
        var types = PluginRegistrations.Of(new StrideDefaultAssetsPlugin(), typeof(Stride.Core.Assets.Editor.Editors.IAssetEditorView)).PrimitiveTypes;

        Assert.Contains(typeof(AssetReference), types);
        Assert.Contains(typeof(UrlReferenceBase), types);
    }

    [Fact]
    public async Task TheEditorDeclaresTheContentReferencesAsPrimitiveTypesOnce()
    {
        await using var fixture = await AssetsSessionFixture.OpenAsync(initializeEditor: false);

        // The content types serialized as references are not expanded by the property grid; the host registers them once,
        // whatever the number of plugins
        var types = fixture.Plugins.GetPrimitiveTypes(fixture.Session).ToList();
        Assert.Single(types, x => x == typeof(Stride.Graphics.Texture));
        Assert.Single(types, x => x == typeof(Stride.Rendering.Model));
        Assert.Empty(PluginRegistrations.Of(new StrideEditorPlugin(), typeof(Stride.Core.Assets.Editor.Editors.IAssetEditorView)).PrimitiveTypes);
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
        // Both font previews share the font view, so both get the font view model, not the generic sprite one
        Assert.Equal(typeof(SpriteFontPreviewViewModel), types[typeof(SpriteFontPreview)]);
        Assert.Equal(typeof(PrecompiledSpriteFontPreviewViewModel), types[typeof(PrecompiledSpriteFontPreview)]);
        // Any sprite batch preview without a view model of its own gets the generic one
        Assert.Equal(typeof(SpritePreviewViewModel), types[typeof(PreviewFromSpriteBatch<>)]);
        // The asset view model plugin declares none
        Assert.Empty(PluginRegistrations.Of(new StrideDefaultAssetsPlugin(), typeof(Stride.Core.Assets.Editor.Editors.IAssetEditorView)).AssetPreviewViewModelTypes);
    }

    [Fact]
    public void TheEditorAssemblyDeclaresThePreviews()
    {
        // The previews register through the assets assemblies; the editor's preview service collects them by attribute
        var previews = Stride.Core.Reflection.AssemblyRegistry.GetScanTypes(typeof(StrideEditorPlugin).Assembly, typeof(Stride.Editor.Preview.IAssetPreview)).ToList();

        Assert.Contains(typeof(TexturePreview), previews);
        Assert.Contains(typeof(ScenePreview), previews);
        Assert.Contains(typeof(SkyboxPreview), previews);
        Assert.Empty(Stride.Core.Reflection.AssemblyRegistry.GetScanTypes(typeof(MaterialViewModel).Assembly, typeof(Stride.Editor.Preview.IAssetPreview)));
    }

    [Fact]
    public void TheEditorPluginCollectsTheGizmosOfTheAssetAssemblies()
    {
        var registrations = PluginRegistrations.Of(new StrideEditorPlugin(), typeof(Stride.Core.Assets.Editor.Editors.IAssetEditorView));

        // The shared editor assembly declares the gizmos of the engine components
        Assert.Equal(typeof(Stride.Assets.Editor.AssetEditors.Gizmos.CameraGizmo), GizmoTypeRegistry.GizmoTypeDictionary[typeof(Stride.Engine.CameraComponent)]);
        Assert.Equal(typeof(Stride.Assets.Editor.AssetEditors.Gizmos.DispatcherLightGizmo), GizmoTypeRegistry.GizmoTypeDictionary[typeof(Stride.Engine.LightComponent)]);
        // The entity factories of the engine components come from the shared assembly, in the same categories as in WPF
        Assert.Equal(["2D", "Camera", "Light", "Model"], StrideEditorPlugin.EntityFactoryCategories.Select(x => x.Name));
        Assert.Contains(StrideEditorPlugin.EntityFactoryCategories.Single(x => x.Name == "Light").Factories, x => x.Factory is EntityFactories.PointLightEntityFactory);
        // The add-asset policies of the engine assets come from the shared assembly too
        Assert.Contains(typeof(Stride.Assets.Editor.AssetEditors.EntityHierarchyEditor.ViewModels.AddPrefabAssetPolicy), AddAssetPolicyRegistry.PolicyTypes);
        Assert.Contains(AddAssetPolicyRegistry.PolicyTypes, x => x.Name == "AddSceneAssetPolicy");
        Assert.Contains(AddAssetPolicyRegistry.PolicyTypes, x => x.Name == "AddModelAssetPolicy");
        Assert.Equal(2, registrations.ProfileSettings.Count);
    }

    [Fact]
    public async Task TheEditorPluginRegistersTheBuildServicesOfTheSession()
    {
        var gameSettings = GameSettingsFactory.Create();
        gameSettings.SplashScreenTexture = null!;
        await using var fixture = await AssetsSessionFixture.OpenAsync(initializeEditor: true, ("GameSettings", gameSettings));

        Assert.NotNull(fixture.ServiceProvider.TryGet<GameSettingsProviderService>());
        var builder = fixture.ServiceProvider.TryGet<GameStudioBuilderService>();
        Assert.NotNull(builder);
        Assert.Same(builder.Database, fixture.ServiceProvider.TryGet<GameStudioDatabase>());
        // The effects read the shader files of the session where they are: nothing to import first
        Assert.IsType<ShaderSourceFileProvider>(((EffectCompilerBase)builder.EffectCompiler).FileProvider);
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
