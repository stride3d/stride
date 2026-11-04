// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Reflection;
using Avalonia.Headless.XUnit;
using Stride.Assets.Editor;
using Stride.Assets.Editor.Avalonia;
using Stride.Assets.Editor.Avalonia.Views;
using Stride.Assets.Editor.EntityFactories;
using Stride.Assets.Editor.Preview;
using Stride.Assets.Editor.ViewModels;
using Stride.Assets.Editor.ViewModels.Preview;
using Stride.Assets.Materials;
using Stride.Assets.Models;
using Stride.Assets.Presentation;
using Stride.Assets.Presentation.ViewModels;
using Stride.Assets.Sprite;
using Stride.Assets.Textures;
using Stride.Core.Assets.Editor;
using Stride.Core.Assets.Editor.Annotations;
using Stride.Core.Assets.Editor.Avalonia;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Diagnostics;
using Stride.Core.Reflection;
using Stride.Core.Presentation.Quantum;
using Stride.Editor;
using Stride.Editor.Annotations;
using Stride.Editor.Preview;
using Stride.Editor.Preview.ViewModel;
using Stride.GameStudio.Avalonia.Tests.Helpers;

namespace Stride.GameStudio.Avalonia.Tests;

/// <summary>
/// The shell's plugin service: the plugins of every asset assembly, registered when the session is created.
/// </summary>
public sealed class PluginServiceTests
{
    [AvaloniaFact]
    public void TheApplicationLoadsTheEditorsOwnPlugins()
    {
        // App.InitializePlugins ran the module constructors of the editor's own layers
        var plugins = AssetsPlugin.RegisteredPlugins;
        Assert.Contains(plugins, x => x is StrideDefaultAssetsPlugin);
        Assert.Contains(plugins, x => x is StrideEditorPlugin);
        Assert.Contains(plugins, x => x is StrideCoreEditorViewPlugin);
        Assert.Contains(plugins, x => x is StrideEditorViewPlugin);
        // With the command line of the tests: no thumbnail service
        Assert.All(plugins.OfType<StrideEditorPlugin>(), x => Assert.False(x.EnableThumbnailService));
    }

    [AvaloniaFact]
    public async Task TheSessionRegistersEveryDiscoveredPlugin()
    {
        using var fixture = await ShellSessionFixture.OpenAsync(("Sheet", new SpriteSheetAsset()));
        var service = fixture.Plugins;

        Assert.Same(service, fixture.Session.ServiceProvider.Get<IAssetsPluginService>());
        Assert.Equal(AssetsPlugin.RegisteredPlugins.Count, service.Plugins.Count);
        Assert.Equal(AssetsPlugin.RegisteredPlugins.Count, service.SessionPlugins.Count);

        // Types resolve to the registration of the type or of a base type
        Assert.Equal(typeof(SpriteSheetViewModel), service.GetAssetViewModelType(typeof(SpriteSheetAsset)));
        Assert.Equal(typeof(SpriteSheetEditorViewModel), service.GetEditorViewModelType(typeof(SpriteSheetViewModel)));
        Assert.Equal(typeof(SpriteSheetEditorView), service.GetEditorViewType(typeof(SpriteSheetEditorViewModel)));
        Assert.True(service.HasEditorView(fixture.Session, typeof(SpriteSheetViewModel)));
        Assert.Contains(typeof(Stride.Core.Assets.AssetReference), service.GetPrimitiveTypes(fixture.Session));
    }

    [AvaloniaFact]
    public async Task EachAssetTypeHasOneViewModel()
    {
        using var fixture = await ShellSessionFixture.OpenAsync(("Sheet", new SpriteSheetAsset()));
        var service = fixture.Plugins;
        var assemblies = AssemblyRegistry.Find(AssemblyCommonCategories.Assets);

        // The shared view models come from Stride.Assets.Presentation.Common, through its own plugin; the WPF assembly is not
        // loaded
        Assert.Contains(service.SessionPlugins, x => x is StrideSharedAssetsPlugin);
        Assert.Equal(typeof(StrideSharedAssetsPlugin).Assembly, typeof(MaterialViewModel).Assembly);
        Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), x => x.GetName().Name == "Stride.Assets.Presentation.Wpf");

        var declarations = assemblies
            .SelectMany(x => AssemblyRegistry.GetScanTypes(x, typeof(AssetViewModel)))
            .Select(x => (x.GetCustomAttribute<AssetViewModelAttribute>()?.AssetType, ViewModel: x))
            .Where(x => x.AssetType is not null)
            .ToList();
        var duplicates = declarations
            .GroupBy(x => x.AssetType)
            .Where(x => x.Count() > 1)
            .Select(x => $"{x.Key!.Name}: {string.Join(", ", x.Select(y => y.ViewModel.FullName))}");
        Assert.Empty(duplicates);
        // Every declared view model is the one the editor resolves for its asset type
        Assert.All(declarations, x => Assert.Equal(x.ViewModel, service.GetAssetViewModelType(x.AssetType!)));
        Assert.Equal(typeof(MaterialViewModel), service.GetAssetViewModelType(typeof(MaterialAsset)));
        Assert.Equal(typeof(ModelViewModel), service.GetAssetViewModelType(typeof(ModelAsset)));
    }

    [AvaloniaFact]
    public async Task EachAssetTypeHasOnePreviewAndEachPreviewOneViewModel()
    {
        using var fixture = await ShellSessionFixture.OpenAsync(("Sheet", new SpriteSheetAsset()));
        var service = fixture.Plugins;
        var assemblies = AssemblyRegistry.Find(AssemblyCommonCategories.Assets);

        // The previews and their view models come from Stride.Assets.Presentation.Common; the view models through the shared
        // plugin, which must be a Stride plugin for the host to register them
        Assert.Equal(typeof(StrideSharedAssetsPlugin).Assembly, typeof(ModelPreview).Assembly);
        Assert.Equal(typeof(StrideSharedAssetsPlugin).Assembly, typeof(ModelPreviewViewModel).Assembly);
        Assert.IsAssignableFrom<StrideAssetsPlugin>(Assert.Single(service.SessionPlugins.OfType<StrideSharedAssetsPlugin>()));

        // The preview service keeps one preview per asset type (the last one registered wins): two would hide one silently
        var previews = assemblies
            .SelectMany(x => AssemblyRegistry.GetScanTypes(x, typeof(IAssetPreview)))
            .Select(x => (x.GetCustomAttribute<AssetPreviewAttribute>()?.AssetType, Preview: x))
            .Where(x => x.AssetType is not null)
            .ToList();
        Assert.Empty(Duplicates(previews, x => x.AssetType!, x => x.Preview));
        Assert.Contains(previews, x => x.AssetType == typeof(TextureAsset) && x.Preview == typeof(TexturePreview));

        // One view model per preview type, and each declared one is what the editor resolves
        var viewModels = assemblies
            .SelectMany(x => AssemblyRegistry.GetScanTypes(x, typeof(IAssetPreviewViewModel)))
            .Select(x => (x.GetCustomAttribute<AssetPreviewViewModelAttribute>()?.AssetPreviewType, ViewModel: x))
            .Where(x => x.AssetPreviewType is not null)
            .ToList();
        Assert.Empty(Duplicates(viewModels, x => x.AssetPreviewType!, x => x.ViewModel));
        Assert.All(viewModels, x => Assert.Equal(x.ViewModel, service.GetPreviewViewModelType(x.AssetPreviewType!)));

        // Each shared preview gets its view model, its own or the one of a base type, except the scene and prefab previews,
        // which have none in either editor
        var withoutViewModel = previews
            .Where(x => x.Preview.Assembly == typeof(ModelPreview).Assembly && service.GetPreviewViewModelType(x.Preview) is null)
            .Select(x => x.Preview)
            .ToHashSet();
        Assert.Equal(new HashSet<Type> { typeof(ScenePreview), typeof(PrefabPreview), typeof(PrefabModelPreview) }, withoutViewModel);
        Assert.Equal(typeof(SpritePreviewViewModel), service.GetPreviewViewModelType(typeof(PrecompiledSpriteFontPreview)));
        Assert.Equal(typeof(TexturePreviewViewModel), service.GetPreviewViewModelType(typeof(TexturePreview)));
    }

    [AvaloniaFact]
    public async Task EachEntityFactoryIsInTheAddEntityMenuOnce()
    {
        using var fixture = await ShellSessionFixture.OpenAsync(("Sheet", new SpriteSheetAsset()));

        // The add-entity menus list the factories of the assets assemblies; the shared ones come from Stride.Assets.Presentation.Common
        var factories = StrideEditorPlugin.EntityFactoryCategories
            .SelectMany(x => x.Factories.Select(y => (Entry: $"{x.Name}/{y.Name}", Type: y.Factory.GetType())))
            .ToList();
        Assert.Equal(typeof(StrideSharedAssetsPlugin).Assembly, typeof(SpriteEntityFactory).Assembly);
        Assert.Contains(factories, x => x.Type == typeof(SpriteEntityFactory));
        Assert.Contains(factories, x => x.Type == typeof(ModelEntityFactory));
        Assert.Contains(factories, x => x.Type == typeof(SkyboxLightEntityFactory));
        Assert.Empty(Duplicates(factories, x => x.Type, x => x.Type));
        Assert.Empty(Duplicates(factories, x => x.Entry, x => x.Type));
    }

    private static string[] Duplicates<T, TKey>(IEnumerable<T> items, Func<T, TKey> key, Func<T, Type> type)
        where TKey : notnull
    {
        return items
            .GroupBy(key)
            .Where(x => x.Count() > 1)
            .Select(x => $"{x.Key}: {string.Join(", ", x.Select(y => type(y).FullName))}")
            .ToArray();
    }

    [AvaloniaFact]
    public async Task ASecondEditorInTheSameProcessGetsThePluginsAfresh()
    {
        // App.Restart() replaces the editor's services: the plugins' process-wide registrations (the property grid's template
        // providers) must follow, or the plugins cannot join the next session
        using (var first = await ShellSessionFixture.OpenAsync(("Sheet", new SpriteSheetAsset())))
        {
            Assert.Equal(AssetsPlugin.RegisteredPlugins.Count, first.Plugins.SessionPlugins.Count);
        }

        using var second = await ShellSessionFixture.OpenAsync(("Sheet", new SpriteSheetAsset()));

        Assert.Equal(AssetsPlugin.RegisteredPlugins.Count, second.Plugins.SessionPlugins.Count);
        Assert.DoesNotContain(second.Session.AssetLog.FilteredMessages, x => x.IsAtLeast(LogMessageType.Warning));
        Assert.Equal(typeof(SpriteSheetEditorView), second.Plugins.GetEditorViewType(typeof(SpriteSheetEditorViewModel)));
    }

    [AvaloniaFact]
    public async Task TheSessionInitializationRegistersTheCommandsAndUpdaters()
    {
        using var fixture = await ShellSessionFixture.OpenAsync(("Sheet", new SpriteSheetAsset()));
        var graphService = fixture.Session.AssetViewProperties.ServiceProvider.Get<GraphViewModelService>();

        // The main view model initialized the plugins' session once the session was open: the property grid got their
        // commands and updaters (the ones of the assets editor plugin among them)
        Assert.Contains(graphService.AvailableCommands, x => x.GetType().Name == "FetchEntityCommand");
        Assert.Contains(graphService.AvailableUpdaters, x => x.GetType().Name == "AnimationAssetNodeUpdater");
    }
}
