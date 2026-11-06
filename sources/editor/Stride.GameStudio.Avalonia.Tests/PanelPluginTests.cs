// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Headless.XUnit;
using Stride.Assets.Sprite;
using Stride.Core.Assets.Editor.Panels;
using Stride.Core.Assets.Editor.Services;
using Stride.GameStudio.Avalonia.Docking;
using Stride.GameStudio.Avalonia.Settings;
using Stride.GameStudio.Avalonia.Tests.Helpers;
using Stride.GameStudio.Avalonia.Views;

namespace Stride.GameStudio.Avalonia.Tests;

/// <summary>
/// The panel of a plugin, end to end: <see cref="NotebookTestPlugin"/> registers a panel that can be opened several times, as a
/// plugin of Phase 4 will.
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
}

/// <summary>
/// A plugin of the test assembly with a panel that can be opened several times, registered with the plugin as its owner.
/// </summary>
public sealed class NotebookTestPlugin : AssetsPlugin
{
    public const string PanelId = "Tests.Notebook";

    public override void Initialize(IEditorPluginContext context)
    {
        base.Initialize(context);

        // Phase 4 gives the plugins their own registration (decision D6); until then the panels are the host's
        if (context.Session.ServiceProvider.TryGet<IAssetsPluginService>() is EditorPluginHost host)
        {
            host.Panels.Register(new PanelDescriptor
            {
                Id = PanelId,
                Title = () => "Notebook",
                DefaultPlacement = PanelPlacement.Right,
                DefaultGroup = DefaultPanelLayouts.RightBottomGroup,
                // After the editor's panels in the View menu
                Order = 1000,
                AllowsMultipleInstances = true,
                CreateViewModel = context => new NotebookPanelViewModel(context),
            }, this);
        }
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
