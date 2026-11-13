// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.ViewModels;
using Stride.Core.Translation;

namespace Stride.Core.Assets.Editor.Panels;

/// <summary>
/// The kind of an entry of the View menu.
/// </summary>
public enum ViewMenuItemKind
{
    /// <summary>
    /// A single panel: checked while it is visible, and shown or hidden when clicked.
    /// </summary>
    Toggle,

    /// <summary>
    /// "New &lt;title&gt;": opens an instance of a panel that can be opened several times.
    /// </summary>
    NewInstance,

    /// <summary>
    /// An open instance of a panel that can be opened several times: brings it forward.
    /// </summary>
    Instance,

    /// <summary>
    /// A separator between two sections.
    /// </summary>
    Separator,

    /// <summary>
    /// "Reset layout".
    /// </summary>
    ResetLayout,
}

/// <summary>
/// An entry of the View menu.
/// </summary>
public sealed class ViewMenuItemViewModel : ViewModelBase
{
    private readonly IPanelService? panels;
    private bool isChecked;

    internal ViewMenuItemViewModel(IViewModelServiceProvider serviceProvider, ViewMenuItemKind kind, string header, string? panelId, IPanelService? panels, ICommandBase? command)
        : base(serviceProvider)
    {
        Kind = kind;
        Header = header;
        PanelId = panelId;
        this.panels = panels;
        Command = command;
        if (kind == ViewMenuItemKind.Toggle && panelId is not null && panels is not null)
            isChecked = panels.IsVisible(panelId);
    }

    public ViewMenuItemKind Kind { get; }

    /// <summary>
    /// The text of the entry; "-" for a separator, which a menu shows as a line.
    /// </summary>
    public string Header { get; }

    /// <summary>
    /// The panel of the entry, or null for a separator and "Reset layout".
    /// </summary>
    public string? PanelId { get; }

    public bool IsSeparator => Kind == ViewMenuItemKind.Separator;

    public bool IsCheckable => Kind == ViewMenuItemKind.Toggle;

    /// <summary>
    /// For a single panel, whether it is visible: setting it shows or hides the panel.
    /// </summary>
    public bool IsChecked
    {
        get => isChecked;
        set
        {
            if (Kind != ViewMenuItemKind.Toggle || PanelId is null || panels is null || !SetValue(ref isChecked, value))
                return;

            if (value)
                panels.Show(PanelId);
            else
                panels.Hide(PanelId);
        }
    }

    /// <summary>
    /// What a click does, for the entries that are not toggles.
    /// </summary>
    public ICommandBase? Command { get; }

    // The panel was shown or hidden elsewhere (its close button, a reset)
    internal void Refresh()
    {
        if (Kind == ViewMenuItemKind.Toggle && PanelId is not null && panels is not null)
            SetValue(ref isChecked, panels.IsVisible(PanelId), nameof(IsChecked));
    }
}

/// <summary>
/// The View menu, made from the registered panels: one entry per single panel, checked while it is visible; a "New" entry
/// per panel that can be opened several times, followed by its open instances; the sections of
/// <see cref="PanelDescriptor.MenuGroup"/> between separators; and "Reset layout" at the end. It follows the panels that
/// plugins register and unregister.
/// </summary>
public sealed class ViewMenuViewModel : ViewModelBase
{
    private readonly IPanelService panels;

    public ViewMenuViewModel(IViewModelServiceProvider serviceProvider, IPanelService panels)
        : base(serviceProvider)
    {
        this.panels = panels ?? throw new ArgumentNullException(nameof(panels));
        ResetLayoutCommand = new AnonymousTaskCommand(ServiceProvider, panels.ResetLayoutAsync);
        panels.Registry.CollectionChanged += RegistryChanged;
        panels.PanelChanged += PanelChanged;
        Rebuild();
    }

    /// <summary>
    /// The entries of the menu.
    /// </summary>
    public ObservableCollection<ViewMenuItemViewModel> Items { get; } = [];

    /// <summary>
    /// Puts back the default layouts of the session.
    /// </summary>
    public ICommandBase ResetLayoutCommand { get; }

    /// <inheritdoc />
    public override void Destroy()
    {
        panels.Registry.CollectionChanged -= RegistryChanged;
        panels.PanelChanged -= PanelChanged;
        Items.Clear();
        base.Destroy();
    }

    private void RegistryChanged(object? sender, NotifyCollectionChangedEventArgs e) => Rebuild();

    private void PanelChanged(object? sender, PanelChangedEventArgs e)
    {
        // An instance opened or closed: the list of instances changes
        if (panels.Registry.Find(e.PanelId) is { AllowsMultipleInstances: true })
        {
            Rebuild();
            return;
        }
        foreach (var item in Items.Where(x => x.PanelId == e.PanelId))
            item.Refresh();
    }

    private void Rebuild()
    {
        var items = new List<ViewMenuItemViewModel>();
        // The sections in the order of their first panel; a panel without a section is a section of its own
        var sections = panels.Registry.Panels
            .GroupBy(x => x.MenuGroup ?? x.Id)
            .Select(x => x.OrderBy(p => p.Order).ToList())
            .OrderBy(x => x[0].Order);
        foreach (var section in sections)
        {
            if (items.Count > 0)
                items.Add(Separator());
            foreach (var descriptor in section.Where(x => !x.AllowsMultipleInstances))
                items.Add(new ViewMenuItemViewModel(ServiceProvider, ViewMenuItemKind.Toggle, descriptor.Title(), descriptor.Id, panels, null));
            foreach (var descriptor in section.Where(x => x.AllowsMultipleInstances))
            {
                var id = descriptor.Id;
                var header = string.Format(Tr._p("Menu", "New {0}"), descriptor.Title());
                items.Add(new ViewMenuItemViewModel(ServiceProvider, ViewMenuItemKind.NewInstance, header, id, panels, new AnonymousTaskCommand(ServiceProvider, () => panels.OpenInstanceAsync(id))));
                foreach (var instance in panels.GetInstances(id))
                    items.Add(new ViewMenuItemViewModel(ServiceProvider, ViewMenuItemKind.Instance, instance.Title, id, panels, new AnonymousCommand(ServiceProvider, () => panels.Activate(instance))));
            }
        }
        if (items.Count > 0)
            items.Add(Separator());
        items.Add(new ViewMenuItemViewModel(ServiceProvider, ViewMenuItemKind.ResetLayout, Tr._p("Menu", "Reset layout"), null, null, ResetLayoutCommand));

        Items.Clear();
        foreach (var item in items)
            Items.Add(item);
    }

    private ViewMenuItemViewModel Separator() => new(ServiceProvider, ViewMenuItemKind.Separator, "-", null, null, null);
}
