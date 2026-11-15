// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Panels;
using Stride.Core.Presentation.ViewModels;
using static Stride.Core.Assets.Editor.Tests.Panels.PanelTestHelpers;

namespace Stride.Core.Assets.Editor.Tests.Panels;

public sealed class TestViewMenu
{
    /// <summary>
    /// A shell that only records what is shown, without view models.
    /// </summary>
    private sealed class FakePanelService(PanelRegistry registry) : IPanelService
    {
        private readonly HashSet<string> hidden = [];
        private readonly Dictionary<string, List<IPanelViewModel>> instances = [];

        public PanelRegistry Registry { get; } = registry;

        public int Resets { get; private set; }

        public event EventHandler<PanelChangedEventArgs>? PanelChanged;

        public bool IsVisible(string panelId) => Registry.Find(panelId) is { AllowsMultipleInstances: false } ? !hidden.Contains(panelId) : GetInstances(panelId).Count > 0;

        public void Show(string panelId, bool activate = true) => Change(panelId, () => hidden.Remove(panelId));

        public void Hide(string panelId) => Change(panelId, () => hidden.Add(panelId));

        public IPanelViewModel? GetPanel(string panelId) => null;

        public IReadOnlyList<IPanelViewModel> GetInstances(string panelId) => instances.TryGetValue(panelId, out var list) ? list : [];

        public Task<IPanelViewModel> OpenInstanceAsync(string panelId)
        {
            var list = instances.TryGetValue(panelId, out var existing) ? existing : instances[panelId] = [];
            var instance = new FakeInstance(panelId, (list.Count + 1).ToString());
            Change(panelId, () => list.Add(instance));
            return Task.FromResult<IPanelViewModel>(instance);
        }

        public IPanelViewModel? Activated { get; private set; }

        public void Activate(IPanelViewModel instance) => Activated = instance;

        public Task<bool> CloseInstanceAsync(IPanelViewModel instance)
        {
            var closed = instances.TryGetValue(instance.PanelId, out var list) && list.Remove(instance);
            if (closed)
                PanelChanged?.Invoke(this, new PanelChangedEventArgs(instance.PanelId));
            return Task.FromResult(closed);
        }

        public Task ResetLayoutAsync()
        {
            ++Resets;
            foreach (var panelId in hidden.ToList())
                Show(panelId);
            return Task.CompletedTask;
        }

        private void Change(string panelId, Action change)
        {
            change();
            PanelChanged?.Invoke(this, new PanelChangedEventArgs(panelId));
        }
    }

    private sealed class FakeInstance(string panelId, string instanceId) : IPanelViewModel
    {
        public string PanelId { get; } = panelId;
        public string InstanceId { get; } = instanceId;
        public string Title => $"{PanelId} {InstanceId}";
        public bool IsShown { get; set; }
        public Task<bool> CanCloseAsync() => Task.FromResult(true);
        public void Destroy() { }
    }

    private static List<string> Headers(ViewMenuViewModel menu) => menu.Items.Select(x => x.IsCheckable ? $"{x.Header}{(x.IsChecked ? " [x]" : " [ ]")}" : x.Header).ToList();

    [Fact]
    public void TheMenuListsTheBuiltInPanelsInTheSectionsOfWpf()
    {
        var registry = CreateBuiltInRegistry();
        var menu = new ViewMenuViewModel(ViewModelServiceProvider.NullServiceProvider, new FakePanelService(registry));

        Assert.Equal(
        [
            "Solution explorer [x]", "Asset view [x]", "References [x]", "-",
            "Asset preview [x]", "-",
            "Property grid [x]", "Edit history [x]", "-",
            "Asset errors [x]", "Output [x]", "-",
            "Reset layout",
        ], Headers(menu));
        Assert.Equal(ViewMenuItemKind.ResetLayout, menu.Items[^1].Kind);
        Assert.True(menu.Items[3].IsSeparator);
        menu.Destroy();
    }

    [Fact]
    public void AToggleShowsAndHidesItsPanelAndFollowsIt()
    {
        var registry = CreateBuiltInRegistry();
        var panels = new FakePanelService(registry);
        var menu = new ViewMenuViewModel(ViewModelServiceProvider.NullServiceProvider, panels);
        var references = menu.Items.Single(x => x.PanelId == BuiltInPanels.References);

        references.IsChecked = false;
        Assert.False(panels.IsVisible(BuiltInPanels.References));

        // Shown again from elsewhere (the reset)
        menu.ResetLayoutCommand.Execute();
        Assert.Equal(1, panels.Resets);
        Assert.True(references.IsChecked);

        // Hidden from elsewhere (its close button)
        panels.Hide(BuiltInPanels.References);
        Assert.False(references.IsChecked);
        menu.Destroy();
    }

    [Fact]
    public async Task ThePanelsOfAPluginComeAndGoWithIt()
    {
        var registry = CreateBuiltInRegistry();
        var panels = new FakePanelService(registry);
        var menu = new ViewMenuViewModel(ViewModelServiceProvider.NullServiceProvider, panels);
        var plugin = new object();

        // A section of its own, ordered after the logs, and a panel in the section of the explorers
        registry.Register(Panel("Plugin.Tools", menuGroup: "Plugin", order: 900, multiple: true), plugin);
        registry.Register(Panel("Plugin.Graph", menuGroup: "Plugin", order: 910), plugin);
        registry.Register(Panel("Plugin.Explorer", menuGroup: BuiltInPanels.ExploreMenuGroup, order: 115), plugin);

        Assert.Equal(
        [
            "Solution explorer [x]", "Asset view [x]", "Plugin.Explorer [x]", "References [x]", "-",
            "Asset preview [x]", "-",
            "Property grid [x]", "Edit history [x]", "-",
            "Asset errors [x]", "Output [x]", "-",
            "Plugin.Graph [x]", "New Plugin.Tools", "-",
            "Reset layout",
        ], Headers(menu));

        // "New" opens an instance, which is listed after it and brought forward when clicked
        menu.Items.Single(x => x.Kind == ViewMenuItemKind.NewInstance).Command!.Execute();
        menu.Items.Single(x => x.Kind == ViewMenuItemKind.NewInstance).Command!.Execute();
        Assert.Equal(["New Plugin.Tools", "Plugin.Tools 1", "Plugin.Tools 2"], menu.Items.Where(x => x.PanelId == "Plugin.Tools").Select(x => x.Header));
        var second = menu.Items.Single(x => x.Header == "Plugin.Tools 2");
        Assert.Equal(ViewMenuItemKind.Instance, second.Kind);
        second.Command!.Execute();
        Assert.Same(panels.GetInstances("Plugin.Tools")[1], panels.Activated);
        await panels.CloseInstanceAsync(panels.GetInstances("Plugin.Tools")[0]);
        Assert.Equal(["New Plugin.Tools", "Plugin.Tools 2"], menu.Items.Where(x => x.PanelId == "Plugin.Tools").Select(x => x.Header));

        // The plugin leaves
        registry.UnregisterOwner(plugin);
        Assert.DoesNotContain(menu.Items, x => x.PanelId?.StartsWith("Plugin.") == true);
        Assert.Equal(13, menu.Items.Count);
        menu.Destroy();
    }
}
