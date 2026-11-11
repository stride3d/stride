// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Reflection;
using Avalonia.Headless.XUnit;
using Stride.Assets.Editor;
using Stride.Assets.Editor.Avalonia;
using Stride.Assets.Editor.Avalonia.Views;
using Stride.Assets.Editor.EditorGame;
using Stride.Assets.Editor.EntityFactories;
using Stride.Assets.Editor.Preview;
using Stride.Assets.Editor.Templates;
using Stride.Assets.Editor.ViewModels;
using Stride.Assets.Editor.ViewModels.Preview;
using Stride.Assets.Materials;
using Stride.Assets.Models;
using Stride.Assets.Presentation;
using Stride.Assets.Presentation.ViewModels;
using Stride.Assets.Rendering;
using Stride.Assets.Scripts;
using Stride.Assets.Sprite;
using Stride.Assets.Textures;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor;
using Stride.Core.Assets.Editor.Annotations;
using Stride.Core.Assets.Editor.Avalonia;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Templates;
using Stride.Core.Diagnostics;
using Stride.Core.Reflection;
using Stride.Core.Presentation.Quantum;
using Stride.Editor;
using Stride.Editor.Annotations;
using Stride.Editor.EditorGame.Game;
using Stride.Editor.Preview;
using Stride.GameStudio.Avalonia.Tests.Helpers;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Assets.Presentation.Annotations;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Editor.Preview.ViewModels;

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

        // The view models that both editors share come from Stride.Assets.Presentation, through the default assets plugin; the
        // assets editor assembly of the other toolkit is not loaded
        Assert.Contains(service.SessionPlugins, x => x is StrideDefaultAssetsPlugin);
        Assert.Equal([typeof(MaterialViewModel).Assembly], service.SessionPlugins.OfType<StrideDefaultAssetsPlugin>().Single().ScannedAssemblies);
        Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), x => x.GetName().Name == "Stride.Assets.Editor.Wpf");

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

        // The previews and their view models come from Stride.Assets.Editor; the view models through the editor plugin, which
        // must be a Stride plugin for the host to register them
        Assert.Equal(typeof(StrideEditorPlugin).Assembly, typeof(ModelPreview).Assembly);
        Assert.Equal(typeof(StrideEditorPlugin).Assembly, typeof(ModelPreviewViewModel).Assembly);
        Assert.IsAssignableFrom<StrideAssetsPlugin>(Assert.Single(service.SessionPlugins.OfType<StrideEditorPlugin>()));

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
        // Both font previews share the font view, which binds to the font view model
        Assert.Equal(typeof(PrecompiledSpriteFontPreviewViewModel), service.GetPreviewViewModelType(typeof(PrecompiledSpriteFontPreview)));
        Assert.True(typeof(SpriteFontPreviewViewModel).IsAssignableFrom(service.GetPreviewViewModelType(typeof(PrecompiledSpriteFontPreview))));
        Assert.Equal(typeof(TexturePreviewViewModel), service.GetPreviewViewModelType(typeof(TexturePreview)));
    }

    [AvaloniaFact]
    public async Task EachEntityFactoryIsInTheAddEntityMenuOnce()
    {
        using var fixture = await ShellSessionFixture.OpenAsync(("Sheet", new SpriteSheetAsset()));

        // The add-entity menus list the factories of the assets assemblies; the shared ones come from Stride.Assets.Editor
        var factories = StrideEditorPlugin.EntityFactoryCategories
            .SelectMany(x => x.Factories.Select(y => (Entry: $"{x.Name}/{y.Name}", Type: y.Factory.GetType())))
            .ToList();
        Assert.Equal(typeof(StrideEditorPlugin).Assembly, typeof(SpriteEntityFactory).Assembly);
        Assert.Contains(factories, x => x.Type == typeof(SpriteEntityFactory));
        Assert.Contains(factories, x => x.Type == typeof(ModelEntityFactory));
        Assert.Contains(factories, x => x.Type == typeof(SkyboxLightEntityFactory));
        Assert.Empty(Duplicates(factories, x => x.Type, x => x.Type));
        Assert.Empty(Duplicates(factories, x => x.Entry, x => x.Type));
    }

    [AvaloniaFact]
    public void EachAssetTemplateHasOneGenerator()
    {
        // The editor's module registered the shared generators (Stride.Assets.Editor) once, with the Avalonia dialogs
        var generators = RegisteredTemplateGenerators();
        var shared = generators.Where(x => x.GetType().Namespace == typeof(AssetTemplateGenerator).Namespace).ToList();
        Assert.All(shared, x => Assert.Equal(typeof(StrideEditorPlugin).Assembly, x.GetType().Assembly));
        Assert.Empty(Duplicates(shared, x => x.GetType(), x => x.GetType()));
        Assert.Equal(10, shared.Count);

        // Each kind of asset template resolves to its own generator, the specific ones before the generic ones
        foreach (var (description, expected) in AssetTemplateCases())
        {
            var generator = TemplateManager.FindTemplateGenerator<AssetTemplateGeneratorParameters>(description);
            Assert.True(expected == generator?.GetType(), $"{description.Name}: {generator?.GetType().FullName ?? "no generator"} instead of {expected.Name}");
        }
    }

    /// <summary>
    /// The template generators registered with the <see cref="TemplateManager"/>, which does not list them.
    /// </summary>
    internal static List<ITemplateGenerator> RegisteredTemplateGenerators()
    {
        const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Static;
        var generators = (List<ITemplateGenerator>)typeof(TemplateManager).GetField("Generators", flags)!.GetValue(null)!;
        lock (typeof(TemplateManager).GetField("ThisLock", flags)!.GetValue(null)!)
        {
            return [.. generators];
        }
    }

    /// <summary>
    /// A description of each kind of asset template of the Stride package, with the generator that must create it.
    /// </summary>
    internal static IEnumerable<(TemplateDescription Description, Type Generator)> AssetTemplateCases()
    {
        static TemplateAssetFactoryDescription Factory(string name, Guid id, string assetTypeName, bool importSource = false)
            => new() { Id = id, Name = name, Scope = TemplateScope.Asset, AssetTypeName = assetTypeName, ImportSource = importSource };

        yield return (Factory("Material", Guid.NewGuid(), nameof(MaterialAsset)), typeof(AssetFactoryTemplateGenerator));
        yield return (Factory("Texture", Guid.NewGuid(), nameof(TextureAsset), importSource: true), typeof(AssetFromFileTemplateGenerator));
        var derived = Factory("Graphics compositor", Guid.NewGuid(), nameof(Stride.Assets.Rendering.GraphicsCompositorAsset));
        derived.DerivedFrom = "GraphicsCompositor";
        yield return (derived, typeof(DerivedAssetTemplateGenerator));
        yield return (Factory("Procedural model", ProceduralModelFactoryTemplateGenerator.TemplateId, nameof(Stride.Assets.Models.ProceduralModelAsset)), typeof(ProceduralModelFactoryTemplateGenerator));
        yield return (Factory("Skybox", SkyboxFactoryTemplateGenerator.TemplateId, nameof(Stride.Assets.Skyboxes.SkyboxAsset)), typeof(SkyboxFactoryTemplateGenerator));
        yield return (Factory("Model", ModelFromFileTemplateGenerator.Id, nameof(ModelAsset), importSource: true), typeof(ModelFromFileTemplateGenerator));
        yield return (Factory("Skeleton", SkeletonFromFileTemplateGenerator.Id, nameof(SkeletonAsset), importSource: true), typeof(SkeletonFromFileTemplateGenerator));
        yield return (Factory("Animation", AnimationFromFileTemplateGenerator.Id, nameof(AnimationAsset), importSource: true), typeof(AnimationFromFileTemplateGenerator));
        yield return (Factory("Sprite sheet", SpriteSheetFromFileTemplateGenerator.SpriteSheetId, nameof(SpriteSheetAsset), importSource: true), typeof(SpriteSheetFromFileTemplateGenerator));
        yield return (Factory("UI sprite sheet", SpriteSheetFromFileTemplateGenerator.UISpriteSheetId, nameof(SpriteSheetAsset), importSource: true), typeof(SpriteSheetFromFileTemplateGenerator));
        yield return (new TemplateAssetDescription { Id = Guid.NewGuid(), Name = "Script", Scope = TemplateScope.Asset, AssetTypeName = "ScriptSourceFileAsset" }, typeof(ScriptTemplateGenerator));
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
    public void EachEditorGameServiceIsDeclaredOnce()
    {
        // A controller creates the services the assets assemblies declare. The shared ones (Stride.Assets.Editor)
        // replaced their copies: no service is declared twice under one name
        var services = AssetRegistry.AssetAssemblies.SelectMany(x => AssemblyRegistry.GetScanTypes(x, typeof(EditorGameServiceAttribute))).Distinct().ToList();
        Assert.Empty(Duplicates(services, x => x.Name, x => x));
        Assert.Contains(typeof(EditorGameCameraOrientationService), services);
        Assert.Equal(typeof(StrideEditorPlugin).Assembly, typeof(EditorGameCameraOrientationService).Assembly);

        // The shared view cube serves the scene and prefab editors of both editors, through their shared controller interface
        var declaration = typeof(EditorGameCameraOrientationService).GetCustomAttribute<EditorGameServiceAttribute>()!;
        Assert.True(declaration.ControllerType.IsAssignableFrom(typeof(EntityHierarchyEditorController)));
        // So do the shared grid and graphics compositor services
        foreach (var serviceType in new[] { typeof(EditorGameViewportGridService), typeof(EditorGameGraphicsCompositorService) })
        {
            Assert.Contains(serviceType, services);
            Assert.Equal(typeof(StrideEditorPlugin).Assembly, serviceType.Assembly);
            Assert.True(serviceType.GetCustomAttribute<EditorGameServiceAttribute>()!.ControllerType.IsAssignableFrom(typeof(EntityHierarchyEditorController)));
        }
    }

    [AvaloniaTheory]
    [InlineData(typeof(SpriteSheetAsset), typeof(SpriteSheetViewModel), typeof(SpriteSheetEditorViewModel), typeof(SpriteSheetEditorView))]
    [InlineData(typeof(GraphicsCompositorAsset), typeof(GraphicsCompositorViewModel), typeof(GraphicsCompositorEditorViewModel), typeof(GraphicsCompositorEditorView))]
    public async Task TheSharedEditorsOpenTheirAssetInOneEditor(Type assetType, Type assetViewModelType, Type editorType, Type viewType)
    {
        using var fixture = await ShellSessionFixture.OpenAsync(("Asset", (Asset)Activator.CreateInstance(assetType)!));

        // The plugin host registers the editors and editor views of the assemblies of the editor plugins. The shared editors
        // (Stride.Assets.Editor) replaced their copies: one editor per asset, one view per editor
        var assemblies = fixture.Plugins.Plugins.OfType<AssetsEditorPlugin>().Select(x => x.GetType().Assembly).Distinct().ToList();
        var editors = assemblies
            .SelectMany(x => AssemblyRegistry.GetScanTypes(x, typeof(IAssetEditorViewModel)))
            .Select(x => (x.GetCustomAttribute<AssetEditorViewModelAttribute>()?.ViewModelType, Editor: x))
            .Where(x => x.ViewModelType is not null)
            .Distinct()
            .ToList();
        Assert.Empty(Duplicates(editors, x => x.ViewModelType, x => x.Editor));
        var views = assemblies
            .SelectMany(x => AssemblyRegistry.GetScanTypes(x, typeof(Stride.Core.Assets.Editor.Editors.IAssetEditorView)))
            .Select(x => (x.GetCustomAttribute<AssetEditorViewAttribute>()?.EditorViewModelType, View: x))
            .Where(x => x.EditorViewModelType is not null)
            .Distinct()
            .ToList();
        Assert.Empty(Duplicates(views, x => x.EditorViewModelType, x => x.View));

        Assert.Equal([editorType], editors.Where(x => x.ViewModelType == assetViewModelType).Select(x => x.Editor));
        Assert.Equal(typeof(StrideEditorPlugin).Assembly, editorType.Assembly);
        Assert.Equal([viewType], views.Where(x => x.EditorViewModelType == editorType).Select(x => x.View));
        Assert.Equal(editorType, fixture.Plugins.GetEditorViewModelType(assetViewModelType));
    }

    [AvaloniaFact]
    public async Task TheVisualScriptHasOneAssetViewModelTheSharedOne()
    {
        using var fixture = await ShellSessionFixture.OpenAsync(("Sheet", new SpriteSheetAsset()));

        // The shared asset view model (Stride.Assets.Presentation) replaced its copy; the editor stays per toolkit
        var assemblies = fixture.Plugins.Plugins.SelectMany(x => x.ScannedAssemblies).Distinct().ToList();
        var viewModels = assemblies
            .SelectMany(x => AssemblyRegistry.GetScanTypes(x, typeof(AssetViewModel)))
            .Select(x => (x.GetCustomAttribute<AssetViewModelAttribute>()?.AssetType, ViewModel: x))
            .Where(x => x.AssetType is not null)
            .Distinct()
            .ToList();
        Assert.Empty(Duplicates(viewModels, x => x.AssetType!, x => x.ViewModel));

        Assert.Equal([typeof(VisualScriptViewModel)], viewModels.Where(x => x.AssetType == typeof(VisualScriptAsset)).Select(x => x.ViewModel));
        Assert.Equal([typeof(VisualScriptViewModel).Assembly], new StrideDefaultAssetsPlugin().ScannedAssemblies);
        Assert.Equal(typeof(VisualScriptViewModel), fixture.Plugins.GetAssetViewModelType(typeof(VisualScriptAsset)));
        Assert.Equal(typeof(VisualScriptEditorViewModel), fixture.Plugins.GetEditorViewModelType(typeof(VisualScriptViewModel)));
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
