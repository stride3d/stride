// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using Stride.Assets.Sprite;
using Stride.Core.Assets.Editor.Panels;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Diagnostics;
using Stride.GameStudio.Avalonia.Docking;
using Stride.GameStudio.Avalonia.Settings;
using Stride.GameStudio.Avalonia.Tests.Helpers;
using Stride.GameStudio.Avalonia.Views;

namespace Stride.GameStudio.Avalonia.Tests;

/// <summary>
/// The panel of a plugin, end to end: <see cref="NotebookTestPlugin"/> registers a panel that can be opened several times and
/// its view through its plugin context.
/// </summary>
public sealed class PanelPluginTests
{
    [AvaloniaFact]
    public async Task ThePanelOfAPluginOpensInstancesThatComeBackWithTheSessionAndGoWithThePlugin()
    {
        using var fixture = await ShellSessionFixture.OpenAsync(("Sheet", new SpriteSheetAsset()));
        var window = new MainWindow { DataContext = fixture.Main };
        await window.ShowAsync();
        DockPanelService? reopened = null;
        try
        {
            var panels = fixture.Main.Panels;
            var plugin = Assert.Single(fixture.Plugins.SessionPlugins.OfType<NotebookTestPlugin>());
            // Registered with its plugin as owner, and in the View menu as "New notebook", without an instance yet
            Assert.Same(plugin, panels.Registry.GetOwner(NotebookTestPlugin.PanelId));
            Assert.Empty(panels.GetInstances(NotebookTestPlugin.PanelId));
            var create = Assert.Single(fixture.Main.ViewMenu.Items, x => x.Kind == ViewMenuItemKind.NewInstance);
            Assert.Equal("New Notebook", create.Header);

            // Two instances from the View menu, each listed under it
            create.Command!.Execute(null);
            await WindowTestHelper.PumpAsync();
            fixture.Main.ViewMenu.Items.Single(x => x.Kind == ViewMenuItemKind.NewInstance).Command!.Execute(null);
            await WindowTestHelper.PumpAsync();
            var instances = panels.GetInstances(NotebookTestPlugin.PanelId);
            Assert.Equal(["Notebook 1", "Notebook 2"], instances.Select(x => x.Title));
            Assert.Equal(["Notebook 1", "Notebook 2"], fixture.Main.ViewMenu.Items.Where(x => x.Kind == ViewMenuItemKind.Instance).Select(x => x.Header));
            Assert.True(panels.IsVisible(NotebookTestPlugin.PanelId));
            Assert.True(instances[1].IsShown);
            // Shown with the view its plugin registered for this editor
            var view = Assert.Single(window.GetVisualDescendants().OfType<NotebookPanelView>());
            Assert.Same(instances[1], view.DataContext);

            // The session opens again with its instances
            panels.SaveLayout();
            reopened = new DockPanelService(fixture.ServiceProvider, panels.Registry, GameStudioInternalSettings.CreateSessionPanelLayouts());
            reopened.OpenSession(fixture.Session, fixture.PackagePath);
            Assert.Equal(["Notebook 1", "Notebook 2"], reopened.GetInstances(NotebookTestPlugin.PanelId).Select(x => x.Title));
            reopened.Dispose();
            reopened = null;

            // The plugin leaves: its instances close and its entries go; the saved layout keeps them for its return
            var first = (NotebookPanelViewModel)instances[0];
            fixture.Plugins.PluginsRemoved([plugin]);
            await WindowTestHelper.PumpAsync();
            Assert.False(panels.Registry.Contains(NotebookTestPlugin.PanelId));
            Assert.Empty(panels.GetInstances(NotebookTestPlugin.PanelId));
            Assert.True(first.WasDestroyed);
            Assert.DoesNotContain(fixture.Main.ViewMenu.Items, x => x.PanelId == NotebookTestPlugin.PanelId);
            panels.SaveLayout();
            var saved = GameStudioInternalSettings.CreateSessionPanelLayouts().Load(fixture.PackagePath, PanelLayoutKind.Normal)!;
            Assert.Equal([new PanelRef(NotebookTestPlugin.PanelId, "1"), new PanelRef(NotebookTestPlugin.PanelId, "2")], saved.EnumeratePanels().Where(x => x.PanelId == NotebookTestPlugin.PanelId));

            // It comes back: its panel is in the View menu again
            fixture.Plugins.PluginsDiscovered([plugin]);
            await WindowTestHelper.PumpAsync();
            Assert.Same(plugin, panels.Registry.GetOwner(NotebookTestPlugin.PanelId));
            Assert.Contains(fixture.Main.ViewMenu.Items, x => x.Kind == ViewMenuItemKind.NewInstance && x.PanelId == NotebookTestPlugin.PanelId);
        }
        finally
        {
            reopened?.Dispose();
            await window.CloseAsync();
        }
    }

    [AvaloniaFact]
    public async Task APanelWithoutAViewForThisEditorSaysSoAndIsLogged()
    {
        using var fixture = await ShellSessionFixture.OpenAsync(("Sheet", new SpriteSheetAsset()));
        var window = new MainWindow { DataContext = fixture.Main };
        await window.ShowAsync();
        var messages = new List<ILogMessage>();
        GlobalLogger.GlobalMessageLogged += messages.Add;
        IDisposable? registration = null;
        try
        {
            var plugin = Assert.Single(fixture.Plugins.SessionPlugins.OfType<NotebookTestPlugin>());
            registration = fixture.Main.Panels.Registry.Register(CreatePanel("Tests.NoView", context => new NoViewPanelViewModel(context)), plugin);
            fixture.Main.Panels.Show("Tests.NoView");
            await WindowTestHelper.PumpAsync();

            Assert.True(fixture.Main.Panels.IsVisible("Tests.NoView"));
            Assert.Contains(window.GetVisualDescendants().OfType<TextBlock>(), x => x.Text == DockPanelService.NoViewForThisEditor);
            Assert.Contains(messages, x => x.Type == LogMessageType.Warning && x.Text.Contains("Tests.NoView") && x.Text.Contains(typeof(NotebookTestPlugin).FullName!) && x.Text.Contains("no view for this editor"));
            // Nothing failed: the plugin has no error
            Assert.Empty(fixture.Plugins.GetPluginStatus(plugin)!.Errors);
        }
        finally
        {
            GlobalLogger.GlobalMessageLogged -= messages.Add;
            registration?.Dispose();
            await window.CloseAsync();
        }
    }

    [AvaloniaFact]
    public async Task APanelOfAPluginThatFailsIsTurnedOffAndThePluginStays()
    {
        using var fixture = await ShellSessionFixture.OpenAsync(("Sheet", new SpriteSheetAsset()));
        var window = new MainWindow { DataContext = fixture.Main };
        await window.ShowAsync();
        var registrations = new List<IDisposable>();
        try
        {
            var panels = fixture.Main.Panels;
            var plugin = Assert.Single(fixture.Plugins.SessionPlugins.OfType<NotebookTestPlugin>());

            // Its view model fails: the panel is turned off and leaves the View menu
            registrations.Add(panels.Registry.Register(CreatePanel("Tests.Failing", _ => throw new InvalidOperationException("The panel failed")), plugin));
            await WindowTestHelper.PumpAsync();
            Assert.False(panels.Registry.Contains("Tests.Failing"));
            Assert.DoesNotContain(fixture.Main.ViewMenu.Items, x => x.PanelId == "Tests.Failing");

            // Its view fails: the panel says so, and the view is turned off
            registrations.Add(panels.Registry.Register(CreatePanel("Tests.BrokenView", context => new BrokenViewPanelViewModel(context)), plugin));
            panels.Show("Tests.BrokenView");
            await WindowTestHelper.PumpAsync();
            Assert.Contains(window.GetVisualDescendants().OfType<TextBlock>(), x => x.Text?.Contains("failed", StringComparison.Ordinal) == true);

            var status = fixture.Plugins.GetPluginStatus(plugin)!;
            Assert.Equal(PluginState.Joined, status.State);
            Assert.Equal([PluginContributions.Panel("Tests.Failing"), PluginContributions.View(typeof(BrokenViewPanelViewModel))], status.Errors.Select(x => x.Contribution));
            Assert.Equal(["The panel failed", "The notebook view failed"], status.Errors.Select(x => (x.Exception.InnerException ?? x.Exception).Message));

            // The rest of the plugin stays: its notebook still opens, with its view
            Assert.Contains(plugin, fixture.Plugins.SessionPlugins);
            var notebook = await panels.OpenInstanceAsync(NotebookTestPlugin.PanelId);
            await WindowTestHelper.PumpAsync();
            Assert.Contains(window.GetVisualDescendants().OfType<NotebookPanelView>(), x => x.DataContext == notebook);
            Assert.True(await panels.CloseInstanceAsync(notebook));
        }
        finally
        {
            foreach (var registration in registrations)
                registration.Dispose();
            await window.CloseAsync();
        }
    }

    private static PanelDescriptor CreatePanel(string id, Func<PanelContext, IPanelViewModel> create) => new()
    {
        Id = id,
        Title = () => id,
        DefaultGroup = DefaultPanelLayouts.RightBottomGroup,
        Order = 1001,
        CreateViewModel = create,
    };
}

/// <summary>
/// A plugin of the test assembly with a panel that can be opened several times, registered through its context
/// (<see cref="IEditorPluginContext.Panels"/>), with its Avalonia view (<see cref="IEditorPluginContext.ViewTypes"/>).
/// </summary>
public sealed class NotebookTestPlugin : AssetsPlugin
{
    public const string PanelId = "Tests.Notebook";

    public override void Initialize(IEditorPluginContext context)
    {
        base.Initialize(context);

        context.Panels.Register(new PanelDescriptor
        {
            Id = PanelId,
            Title = () => "Notebook",
            DefaultPlacement = PanelPlacement.Right,
            DefaultGroup = DefaultPanelLayouts.RightBottomGroup,
            // After the editor's panels in the View menu
            Order = 1000,
            AllowsMultipleInstances = true,
            CreateViewModel = context => new NotebookPanelViewModel(context),
        });
        context.ViewTypes.Register(typeof(NotebookPanelViewModel), typeof(NotebookPanelView));
        // A view that fails, for the guard's test
        context.ViewTypes.Register(typeof(BrokenViewPanelViewModel), typeof(BrokenPanelView));
    }
}

/// <summary>
/// An instance of the notebook panel of <see cref="NotebookTestPlugin"/>.
/// </summary>
public sealed class NotebookPanelViewModel : PanelViewModel
{
    public NotebookPanelViewModel(PanelContext context)
        : base(context)
    {
        Title = $"Notebook {context.InstanceId}";
    }

    public bool WasDestroyed { get; private set; }

    public override void Destroy()
    {
        WasDestroyed = true;
        base.Destroy();
    }
}

/// <summary>
/// The Avalonia view of <see cref="NotebookPanelViewModel"/>.
/// </summary>
public sealed class NotebookPanelView : UserControl
{
    public NotebookPanelView()
    {
        Content = new TextBlock { [!TextBlock.TextProperty] = new global::Avalonia.Data.Binding(nameof(NotebookPanelViewModel.Title)) };
    }
}

/// <summary>
/// The view model of a panel that has no view in this editor (a plugin whose views are for another toolkit only).
/// </summary>
public sealed class NoViewPanelViewModel(PanelContext context) : PanelViewModel(context);

/// <summary>
/// The view model of a panel whose view fails.
/// </summary>
public sealed class BrokenViewPanelViewModel(PanelContext context) : PanelViewModel(context);

/// <summary>
/// A view that fails when it is created.
/// </summary>
public sealed class BrokenPanelView : UserControl
{
    public BrokenPanelView()
    {
        throw new InvalidOperationException("The notebook view failed");
    }
}
