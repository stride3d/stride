// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Stride.Assets.Editor.AssetEditors.EntityHierarchyEditor.Widgets;
using Stride.Core;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Diagnostics;
using Stride.Core.Presentation.ViewModels;
using Stride.Core.Settings;

namespace Stride.Assets.Editor.ViewModels;

/// <summary>
/// The scene view widgets key of the package user settings: for each scene or prefab, the widgets the user showed or hid
/// (by <see cref="SceneViewWidgetDescriptor.Id"/>). A widget that is not listed has its default visibility. It is a key of
/// its own, next to <see cref="PackageSceneSettings.SceneSettings"/>, which both editors share: the WPF Game Studio, which
/// shows no widget, keeps it as it is.
/// </summary>
public static class PackageSceneViewWidgets
{
    public static readonly SettingsKey<Dictionary<AssetId, Dictionary<string, bool>>> Visibility = new("Package/SceneViewWidgets",
        PackageUserSettings.SettingsContainer, () => new Dictionary<AssetId, Dictionary<string, bool>>());
}

/// <summary>
/// The widgets of the view of a scene or prefab editor (<see cref="SceneViewWidgetDescriptor"/>): one set per editor, which
/// follows the plugins as they join and leave, creates the view model of a widget the first time it shows, and remembers
/// which widgets the user showed or hid in this scene.
/// </summary>
/// <remarks>
/// The view of the editor creates it (<see cref="EntityHierarchyEditorViewModel.GetOrCreateSceneViewWidgets"/>), so an editor
/// that shows no widget runs no code of them. The code of the plugins runs through the contribution guard of the host.
/// </remarks>
public sealed class SceneViewWidgetsViewModel : DispatcherViewModel
{
    private static readonly Logger Log = GlobalLogger.GetLogger(nameof(SceneViewWidgetsViewModel));

    private readonly EntityHierarchyEditorViewModel editor;
    private readonly EditorPluginHost? host;
    private readonly ContributionRegistry<SceneViewWidgetDescriptor>? registry;
    private readonly ObservableCollection<SceneViewWidgetViewModel> widgets = [];
    private Dictionary<string, bool> visibility;

    internal SceneViewWidgetsViewModel(EntityHierarchyEditorViewModel editor, EditorPluginHost? host)
        : base(editor.ServiceProvider)
    {
        this.editor = editor;
        this.host = host;
        registry = host?.GetDeclaredRegistry<SceneViewWidgetDescriptor>();
        Widgets = new ReadOnlyObservableCollection<SceneViewWidgetViewModel>(widgets);
        visibility = ReadVisibility();
        if (registry is not null)
            registry.CollectionChanged += RegistryChanged;
        Refresh();
    }

    /// <summary>
    /// The widgets that the plugins register, by edge, then by order: shown or hidden.
    /// </summary>
    public ReadOnlyObservableCollection<SceneViewWidgetViewModel> Widgets { get; }

    /// <summary>
    /// Whether a plugin registers a widget: the Widgets list shows then.
    /// </summary>
    public bool HasWidgets => widgets.Count > 0;

    /// <summary>
    /// Raised when a widget comes, goes, shows or hides (UI thread).
    /// </summary>
    public event EventHandler? WidgetsChanged;

    /// <summary>
    /// The widgets shown on an edge, in order, with their view model.
    /// </summary>
    public IEnumerable<SceneViewWidgetViewModel> GetShownWidgets(SceneViewEdge edge) => widgets.Where(x => x.Edge == edge && x.IsVisible && x.Content is not null);

    /// <summary>
    /// The owner of a widget, for the contribution guard, or null.
    /// </summary>
    public object? GetOwner(SceneViewWidgetDescriptor descriptor) => registry?.GetOwner(descriptor);

    /// <inheritdoc />
    public override void Destroy()
    {
        EnsureNotDestroyed(nameof(SceneViewWidgetsViewModel));
        if (registry is not null)
            registry.CollectionChanged -= RegistryChanged;
        foreach (var widget in widgets)
            widget.DestroyContent();
        widgets.Clear();
        base.Destroy();
    }

    private void RegistryChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Several plugins join or leave at once: the set reads the registry once they did
        Dispatcher.InvokeAsync(() =>
        {
            if (!IsDestroyed)
                Refresh();
        });
    }

    /// <summary>
    /// Adds the widgets of the plugins that joined, and removes (destroying their view model) those of the plugins that left.
    /// </summary>
    internal void Refresh()
    {
        var available = new List<SceneViewWidgetDescriptor>();
        foreach (var descriptor in registry?.Items ?? [])
        {
            if (host?.IsTurnedOff(registry!.GetOwner(descriptor), ScenePluginContributions.SceneViewWidget(descriptor.Id)) == true)
                continue;
            available.Add(descriptor);
        }
        var changed = false;
        foreach (var widget in widgets.Where(x => !available.Contains(x.Descriptor)).ToList())
        {
            widgets.Remove(widget);
            widget.DestroyContent();
            changed = true;
        }
        var ordered = available.OrderBy(x => x.Edge).ThenBy(x => x.Order).ThenBy(x => x.Id, StringComparer.Ordinal).ToList();
        for (var i = 0; i < ordered.Count; i++)
        {
            var descriptor = ordered[i];
            var index = IndexOf(descriptor);
            if (index == i)
                continue;
            if (index >= 0)
            {
                widgets.Move(index, i);
            }
            else
            {
                if (TryRun(descriptor, descriptor.Title, out var title))
                {
                    var isVisible = visibility.TryGetValue(descriptor.Id, out var saved) ? saved : descriptor.VisibleByDefault;
                    widgets.Insert(i, new SceneViewWidgetViewModel(this, descriptor, title ?? descriptor.Id, isVisible));
                }
                else
                {
                    // Turned off: the widgets after it keep their place
                    ordered.RemoveAt(i--);
                    continue;
                }
            }
            changed = true;
        }
        foreach (var widget in widgets.Where(x => x.IsVisible).ToList())
            changed |= EnsureContent(widget);
        if (changed)
            OnWidgetsChanged();
        OnPropertyChanged(nameof(HasWidgets));

        int IndexOf(SceneViewWidgetDescriptor descriptor)
        {
            for (var i = 0; i < widgets.Count; i++)
            {
                if (widgets[i].Descriptor == descriptor)
                    return i;
            }
            return -1;
        }
    }

    internal void OnVisibilityChanged(SceneViewWidgetViewModel widget)
    {
        if (widget.IsVisible)
            EnsureContent(widget);
        SaveVisibility(widget.Id, widget.IsVisible);
        OnWidgetsChanged();
    }

    // Creates the view model of a widget that shows, through the guard: a widget whose creation fails is turned off and leaves the list
    private bool EnsureContent(SceneViewWidgetViewModel widget)
    {
        if (widget.Content is not null || !widgets.Contains(widget))
            return false;

        var context = new SceneViewWidgetContext { Descriptor = widget.Descriptor, Editor = editor };
        if (TryRun(widget.Descriptor, () => widget.Descriptor.CreateViewModel(context), out var content) && content is not null)
        {
            widget.Content = content;
            return true;
        }
        widgets.Remove(widget);
        OnPropertyChanged(nameof(HasWidgets));
        return true;
    }

    internal void DestroyContent(SceneViewWidgetDescriptor descriptor, IDestroyable content)
    {
        TryRun(descriptor, () =>
        {
            content.Destroy();
            return true;
        }, out _);
    }

    private bool TryRun<T>(SceneViewWidgetDescriptor descriptor, Func<T> function, out T? result)
    {
        if (host is not null)
            return host.TryRun(registry!.GetOwner(descriptor), ScenePluginContributions.SceneViewWidget(descriptor.Id), function, out result);

        try
        {
            result = function();
            return true;
        }
        catch (Exception e)
        {
            Log.Error($"The scene view widget [{descriptor.Id}] failed and is turned off.", e);
            result = default;
            return false;
        }
    }

    private void OnWidgetsChanged() => WidgetsChanged?.Invoke(this, EventArgs.Empty);

    private Dictionary<string, bool> ReadVisibility()
    {
        try
        {
            var saved = editor.Asset.Directory.Package.UserSettings.GetValue(PackageSceneViewWidgets.Visibility);
            return saved.TryGetValue(editor.Asset.Asset.Id, out var sceneVisibility) ? new Dictionary<string, bool>(sceneVisibility) : [];
        }
        catch (Exception e)
        {
            Log.Warning($"The scene view widgets of {editor.Asset.Url} could not be read.", e);
            return [];
        }
    }

    // Saved at once, in the scene's entry: the other scenes, and the widgets of plugins that are not loaded, keep theirs
    private void SaveVisibility(string id, bool isVisible)
    {
        visibility[id] = isVisible;
        try
        {
            var userSettings = editor.Asset.Directory.Package.UserSettings;
            // A copy: without settings yet, the value is the default dictionary of the key, shared by every package
            var saved = new Dictionary<AssetId, Dictionary<string, bool>>(userSettings.GetValue(PackageSceneViewWidgets.Visibility))
            {
                [editor.Asset.Asset.Id] = new Dictionary<string, bool>(visibility),
            };
            userSettings.SetValue(PackageSceneViewWidgets.Visibility, saved);
            userSettings.Save();
        }
        catch (Exception e)
        {
            Log.Warning($"The scene view widgets of {editor.Asset.Url} could not be saved.", e);
        }
    }
}

/// <summary>
/// A widget of the view of a scene or prefab editor, in the Widgets list and, when it shows, on its edge of the view.
/// </summary>
public sealed class SceneViewWidgetViewModel : DispatcherViewModel
{
    private readonly SceneViewWidgetsViewModel widgets;
    private bool isVisible;

    internal SceneViewWidgetViewModel(SceneViewWidgetsViewModel widgets, SceneViewWidgetDescriptor descriptor, string title, bool isVisible)
        : base(widgets.ServiceProvider)
    {
        this.widgets = widgets;
        Descriptor = descriptor;
        Title = title;
        this.isVisible = isVisible;
    }

    /// <summary>
    /// The descriptor of the widget.
    /// </summary>
    public SceneViewWidgetDescriptor Descriptor { get; }

    /// <summary>
    /// The identifier of the widget.
    /// </summary>
    public string Id => Descriptor.Id;

    /// <summary>
    /// The title of the widget in the Widgets list.
    /// </summary>
    public string Title { get; }

    /// <summary>
    /// The edge of the view the widget docks on.
    /// </summary>
    public SceneViewEdge Edge => Descriptor.Edge;

    /// <summary>
    /// Whether the widget shows; saved for the scene of the editor.
    /// </summary>
    public bool IsVisible { get => isVisible; set => SetValue(ref isVisible, value, () => widgets.OnVisibilityChanged(this)); }

    /// <summary>
    /// The view model that the plugin created for the widget, once it showed.
    /// </summary>
    public IDestroyable? Content { get; internal set; }

    internal void DestroyContent()
    {
        if (Content is not { } content)
            return;
        Content = null;
        widgets.DestroyContent(Descriptor, content);
    }
}
