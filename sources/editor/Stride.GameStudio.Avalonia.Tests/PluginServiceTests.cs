// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Headless.XUnit;
using Stride.Assets.Editor;
using Stride.Assets.Editor.Avalonia;
using Stride.Assets.Editor.Avalonia.Views;
using Stride.Assets.Editor.ViewModels;
using Stride.Assets.Presentation;
using Stride.Assets.Presentation.ViewModels;
using Stride.Assets.Sprite;
using Stride.Core.Assets.Editor;
using Stride.Core.Assets.Editor.Avalonia;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Diagnostics;
using Stride.Core.Presentation.Quantum;
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
