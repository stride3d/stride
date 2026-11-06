// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Specialized;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Recycling;
using Avalonia.Threading;
using Dock.Model.Controls;
using Dock.Model.Core;
using Dock.Model.Core.Events;
using Dock.Model.Inpc.Controls;
using Stride.Core.Assets.Editor.Panels;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Diagnostics;
using Stride.Core.IO;
using Stride.Core.Translation;
using Stride.Core.Presentation.ViewModels;

namespace Stride.GameStudio.Avalonia.Docking;

/// <summary>
/// The panels and editors of the main window over Dock: it builds Dock's model from the neutral layout of the session
/// (<see cref="PanelLayout"/>), writes it back when it saves, and keeps WPF's two layouts per session: one without editors,
/// where the asset view takes the centre, and one with them, switched when the first editor opens and the last one closes.
/// </summary>
/// <remarks>
/// Every Dock type stays in this shell: the rest of the editor only knows <see cref="IPanelService"/> and the neutral layout.
/// </remarks>
public sealed class DockPanelService : ViewModelBase, IPanelService, IDisposable
{
    private static readonly Logger Log = GlobalLogger.GetLogger("Panels");

    private readonly SessionPanelLayoutStore? store;
    private readonly Dictionary<PanelRef, PanelDockable> dockables = [];
    private readonly Dictionary<AssetEditorViewModel, EditorDocument> documents = [];
    private readonly HashSet<AssetEditorViewModel> closingEditors = [];
    private readonly Dictionary<PanelLayoutKind, PanelLayout> baselines = [];
    // The kinds of layout shown since the session opened: the first time each one shows, some panels are hidden (HiddenWhenSessionOpens)
    private readonly HashSet<PanelLayoutKind> shownSinceSessionOpened = [];
    // The tool docks built from a group of the layout, with the group's identifier
    private readonly Dictionary<IDock, string> groupIds = [];
    private SessionViewModel? session;
    private UFile? sessionPath;
    private IRootDock? layout;
    private IDocumentDock? documentDock;
    private PanelLayoutKind kind;
    private bool frozen;
    private bool syncingActiveEditor;
    private bool shownUpdateQueued;
    private bool exitingWindows;

    /// <param name="serviceProvider">The services of the editor.</param>
    /// <param name="registry">The panels: the editor's and the plugins'.</param>
    /// <param name="store">Where the layouts of the sessions are saved, or null to save nothing.</param>
    public DockPanelService(IViewModelServiceProvider serviceProvider, PanelRegistry registry, SessionPanelLayoutStore? store)
        : base(serviceProvider)
    {
        Registry = registry ?? throw new ArgumentNullException(nameof(registry));
        this.store = store;
        Factory = new StrideDockFactory();
        Factory.ActiveDockableChanged += ActiveDockableChanged;
        Factory.DockableHidden += (_, e) => DockableVisibilityChanged(e.Dockable);
        Factory.DockableRestored += (_, e) => DockableVisibilityChanged(e.Dockable);
        Factory.DockableAdded += (_, _) => QueueShownUpdate();
        Factory.DockableRemoved += (_, _) => QueueShownUpdate();
        Factory.DockableMoved += (_, _) => QueueShownUpdate();
        Factory.DockablePinned += (_, _) => QueueShownUpdate();
        Factory.DockableUnpinned += (_, _) => QueueShownUpdate();
        Factory.DockableDocked += (_, _) => QueueShownUpdate();
        Factory.DockableUndocked += (_, _) => QueueShownUpdate();
        Factory.WindowOpened += (_, _) => QueueShownUpdate();
        Factory.WindowClosed += (_, _) => QueueShownUpdate();
        Registry.CollectionChanged += RegistryChanged;
    }

    /// <inheritdoc />
    public PanelRegistry Registry { get; }

    /// <summary>
    /// Dock's factory of the main window.
    /// </summary>
    public StrideDockFactory Factory { get; }

    /// <summary>
    /// Dock's cache of the controls it shows for the panels and editors, shared by the main window and the floating windows.
    /// </summary>
    /// <remarks>
    /// The views themselves are kept by their dockable while it lives (see <see cref="DockViewLocator"/>): a panel moved to
    /// another group or a floating window, or a tab selected again, shows the same view (its tree expansion, its scrolling), as
    /// with WPF's docking. A view out of the visual tree still detaches, so an editor's game stops rendering while its tab is
    /// not selected.
    /// </remarks>
    public ControlRecycling Views { get; } = new() { TryToUseIdAsKey = true };

    /// <summary>
    /// The layout the main window shows, or null without a session.
    /// </summary>
    public IRootDock? Layout
    {
        get => layout;
        private set => SetValue(ref layout, value);
    }

    /// <summary>
    /// Which of the two layouts of the session is shown.
    /// </summary>
    public PanelLayoutKind LayoutKind => kind;

    /// <inheritdoc />
    public event EventHandler<PanelChangedEventArgs>? PanelChanged;

    /// <summary>
    /// Shows the panels and editors of a session in its saved layout.
    /// </summary>
    /// <param name="openedSession">The session.</param>
    /// <param name="path">The file its layouts are kept under (the solution).</param>
    public void OpenSession(SessionViewModel openedSession, UFile path)
    {
        ArgumentNullException.ThrowIfNull(openedSession);
        ArgumentNullException.ThrowIfNull(path);
        if (session is not null)
            throw new InvalidOperationException("A session is already open.");

        session = openedSession;
        sessionPath = path;
        frozen = false;
        shownSinceSessionOpened.Clear();
        foreach (var descriptor in Registry.Panels.Where(x => !x.AllowsMultipleInstances))
            GetOrCreateDockable(new PanelRef(descriptor.Id));
        ((INotifyCollectionChanged)session.EditorCollection.Editors).CollectionChanged += EditorsChanged;
        session.EditorCollection.PropertyChanged += EditorCollectionPropertyChanged;
        kind = session.EditorCollection.Editors.Count > 0 ? PanelLayoutKind.Editors : PanelLayoutKind.Normal;
        ApplySavedLayout();
    }

    /// <summary>
    /// Saves the layout shown, and stops switching layouts: the session is about to close its editors, which must not bring
    /// the layout without editors back. <see cref="CloseSession"/> ends it, <see cref="CancelClose"/> resumes.
    /// </summary>
    public void BeginClose()
    {
        SaveLayout();
        frozen = true;
    }

    /// <summary>
    /// The session stays open after all (the user kept an editor): the layouts switch again.
    /// </summary>
    public void CancelClose()
    {
        frozen = false;
        if (session is null)
            return;
        var wanted = session.EditorCollection.Editors.Count > 0 ? PanelLayoutKind.Editors : PanelLayoutKind.Normal;
        if (wanted != kind)
            SwitchTo(wanted);
    }

    /// <summary>
    /// Forgets the session: its panels go and their view models are destroyed. The layout is not saved: see <see cref="BeginClose"/>.
    /// </summary>
    public void CloseSession()
    {
        if (session is null)
            return;

        ((INotifyCollectionChanged)session.EditorCollection.Editors).CollectionChanged -= EditorsChanged;
        session.EditorCollection.PropertyChanged -= EditorCollectionPropertyChanged;
        CloseWindows(layout);
        Layout = null;
        documentDock = null;
        groupIds.Clear();
        foreach (var document in documents.Values)
            document.Dispose();
        documents.Clear();
        foreach (var dockable in dockables.Values.ToList())
            DestroyDockable(dockable);
        Views.Clear();
        baselines.Clear();
        session = null;
        sessionPath = null;
        RaiseAllPanelsChanged();
    }

    /// <summary>
    /// Saves the layout shown, under the session and its kind.
    /// </summary>
    public void SaveLayout()
    {
        if (layout is null || sessionPath is null || frozen)
            return;

        var saved = PanelLayoutResolver.KeepUnavailablePanels(Snapshot(), baselines.GetValueOrDefault(kind), Registry);
        baselines[kind] = saved;
        try
        {
            store?.Save(sessionPath, kind, saved);
        }
        catch (Exception e)
        {
            Log.Warning("The layout of the panels could not be saved.", e);
        }
    }

    /// <summary>
    /// The layout shown, in the neutral format: the panels docked in the main window, the floating windows (without their
    /// editors), the pinned panels and the hidden ones.
    /// </summary>
    public PanelLayout Snapshot()
    {
        var result = new PanelLayout();
        if (layout is null)
            return result;

        var hidden = layout.HiddenDockables?.OfType<PanelDockable>().Where(x => !x.AllowsMultipleInstances).ToList() ?? [];
        result.Root = layout.ActiveDockable is { } main ? SnapshotNode(main, hidden, true) : null;
        foreach (var window in layout.Windows ?? [])
        {
            if (window.Layout?.ActiveDockable is not { } content || SnapshotNode(content, hidden, false) is not { } node)
                continue;
            result.Floating.Add(new FloatingPanelWindow { Root = node, X = window.X, Y = window.Y, Width = window.Width, Height = window.Height });
        }
        AddPinned(result, PanelSide.Left, layout.LeftPinnedDockables);
        AddPinned(result, PanelSide.Right, layout.RightPinnedDockables);
        AddPinned(result, PanelSide.Top, layout.TopPinnedDockables);
        AddPinned(result, PanelSide.Bottom, layout.BottomPinnedDockables);
        result.Hidden = hidden.Select(x => x.Ref.PanelId).Distinct().ToList();
        return result;

        static void AddPinned(PanelLayout result, PanelSide side, IList<IDockable>? pinned)
        {
            var panels = pinned?.OfType<PanelDockable>().Select(x => x.Ref).ToList() ?? [];
            if (panels.Count > 0)
                result.Pinned.Add(new PinnedPanelGroup { Side = side, Panels = panels });
        }
    }

    /// <summary>
    /// The panel of a dockable of the layout, if it is one.
    /// </summary>
    public PanelDockable? FindDockable(string panelId, string instanceId = "") => dockables.GetValueOrDefault(new PanelRef(panelId, instanceId));

    /// <summary>
    /// The document of an editor, if it is open.
    /// </summary>
    public EditorDocument? FindDocument(AssetEditorViewModel editor) => documents.GetValueOrDefault(editor);

    /// <inheritdoc />
    public bool IsVisible(string panelId)
    {
        if (Registry.Find(panelId) is not { } descriptor)
            return false;
        if (descriptor.AllowsMultipleInstances)
            return GetInstances(panelId).Count > 0;
        return dockables.TryGetValue(new PanelRef(panelId), out var dockable) && !IsHidden(dockable) && IsInLayout(dockable);
    }

    /// <inheritdoc />
    public void Show(string panelId, bool activate = true)
    {
        if (layout is null || !dockables.TryGetValue(new PanelRef(panelId), out var dockable))
            return;

        if (IsHidden(dockable))
        {
            if (dockable.OriginalOwner is IDock owner && IsInLayout(owner))
            {
                Factory.RestoreDockable(dockable);
            }
            else
            {
                // Its group closed meanwhile
                layout.HiddenDockables?.Remove(dockable);
                AddToDefaultPlace(dockable);
            }
        }
        else if (!IsInLayout(dockable))
        {
            AddToDefaultPlace(dockable);
        }

        if (activate)
            Factory.SetActiveDockable(dockable);
        RaisePanelChanged(panelId);
    }

    /// <inheritdoc />
    public void Hide(string panelId)
    {
        if (layout is null || !dockables.TryGetValue(new PanelRef(panelId), out var dockable) || IsHidden(dockable) || !IsInLayout(dockable))
            return;

        Factory.HideDockable(dockable);
        RaisePanelChanged(panelId);
    }

    /// <inheritdoc />
    public IPanelViewModel? GetPanel(string panelId)
    {
        return dockables.TryGetValue(new PanelRef(panelId), out var dockable) && !dockable.AllowsMultipleInstances ? dockable.ViewModel : null;
    }

    /// <inheritdoc />
    public IReadOnlyList<IPanelViewModel> GetInstances(string panelId)
    {
        return dockables.Values.Where(x => x.Ref.PanelId == panelId && x.AllowsMultipleInstances).Select(x => x.ViewModel).ToList();
    }

    /// <inheritdoc />
    public Task<IPanelViewModel> OpenInstanceAsync(string panelId)
    {
        if (Registry.Find(panelId) is not { AllowsMultipleInstances: true })
            throw new ArgumentException($"The panel [{panelId}] cannot be opened several times.", nameof(panelId));
        if (session is null || layout is null)
            throw new InvalidOperationException("No session is open.");

        var number = 1;
        while (dockables.ContainsKey(new PanelRef(panelId, number.ToString())))
            ++number;
        var dockable = GetOrCreateDockable(new PanelRef(panelId, number.ToString()))
            ?? throw new InvalidOperationException($"The panel [{panelId}] could not be created.");
        AddToDefaultPlace(dockable);
        Factory.SetActiveDockable(dockable);
        RaisePanelChanged(panelId);
        return Task.FromResult(dockable.ViewModel);
    }

    /// <inheritdoc />
    public void Activate(IPanelViewModel instance)
    {
        if (dockables.TryGetValue(new PanelRef(instance.PanelId, instance.InstanceId), out var dockable) && IsInLayout(dockable))
            Factory.SetActiveDockable(dockable);
    }

    /// <inheritdoc />
    public async Task<bool> CloseInstanceAsync(IPanelViewModel instance)
    {
        if (!dockables.TryGetValue(new PanelRef(instance.PanelId, instance.InstanceId), out var dockable) || !dockable.AllowsMultipleInstances)
            return false;
        if (!await instance.CanCloseAsync())
            return false;
        // Closed meanwhile
        if (!dockables.ContainsKey(dockable.Ref))
            return true;

        if (dockable.Owner is IDock)
            Factory.RemoveDockable(dockable, true);
        DestroyDockable(dockable);
        RaisePanelChanged(instance.PanelId);
        return true;
    }

    /// <inheritdoc />
    public Task ResetLayoutAsync()
    {
        if (session is null || sessionPath is null)
            return Task.CompletedTask;

        try
        {
            store?.Reset(sessionPath);
        }
        catch (Exception e)
        {
            Log.Warning("The layouts of the panels could not be reset.", e);
        }
        baselines.Clear();
        var reset = PanelLayoutResolver.CreateDefault(kind, Registry);
        // Every single panel is visible again, as WPF's reset, except the panels hidden by default (the Plugins panel, and the
        // curve editor, which WPF creates on demand, outside its layout)
        reset.Hidden.RemoveAll(id => Registry.Find(id)?.VisibleByDefault != false);
        Apply(reset);
        // The open instances are kept, at their default place
        foreach (var instance in dockables.Values.Where(x => x.AllowsMultipleInstances && !IsInLayout(x)).ToList())
            AddToDefaultPlace(instance);
        RaiseAllPanelsChanged();
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        CloseSession();
        Registry.CollectionChanged -= RegistryChanged;
    }

    private void ApplySavedLayout()
    {
        PanelLayout? saved = null;
        try
        {
            saved = sessionPath is null ? null : store?.Load(sessionPath, kind);
        }
        catch (Exception e)
        {
            Log.Warning("The layout of the panels could not be read.", e);
        }

        var resolution = PanelLayoutResolver.Resolve(saved, kind, Registry, sessionOpening: shownSinceSessionOpened.Add(kind));
        if (resolution.Fallback is PanelLayoutFallback.OtherVersion or PanelLayoutFallback.Invalid)
            Log.Warning($"The saved layout of the panels ({kind}) cannot be used ({resolution.Fallback}): the default layout replaces it.");
        if (saved is not null && resolution.Fallback == PanelLayoutFallback.None)
            baselines[kind] = saved;
        Apply(resolution.Layout);
    }

    private void SwitchTo(PanelLayoutKind newKind)
    {
        SaveLayout();
        kind = newKind;
        ApplySavedLayout();
    }

    // Replaces Dock's model with one built from the layout
    private void Apply(PanelLayout panelLayout)
    {
        // The floating windows of the previous model close first: a window that closes takes its panels out of their docks
        CloseWindows(layout);
        // The panels leave their docks of the previous model before they join the new one
        foreach (var dockable in dockables.Values)
            dockable.Owner = null;
        foreach (var document in documents.Values)
            document.Owner = null;
        groupIds.Clear();
        documentDock = null;

        var root = Factory.CreateRootDock();
        root.Id = "Root";
        root.IsCollapsable = false;
        var main = panelLayout.Root is null ? null : BuildNode(panelLayout.Root, Alignment.Left);
        main ??= new ProportionalDock { Orientation = Orientation.Horizontal, VisibleDockables = Factory.CreateList<IDockable>() };
        root.VisibleDockables = Factory.CreateList(main);
        root.ActiveDockable = main;
        root.DefaultDockable = main;
        root.HiddenDockables = Factory.CreateList<IDockable>();
        root.LeftPinnedDockables = Factory.CreateList<IDockable>();
        root.RightPinnedDockables = Factory.CreateList<IDockable>();
        root.TopPinnedDockables = Factory.CreateList<IDockable>();
        root.BottomPinnedDockables = Factory.CreateList<IDockable>();
        root.Windows = Factory.CreateList<IDockWindow>();

        // The editors' area, with the open editors
        if (documentDock is not null && session is not null)
        {
            foreach (var editor in session.EditorCollection.Editors)
                AddDocument(editor, false);
            if (session.EditorCollection.ActiveEditor is { } active && documents.TryGetValue(active, out var activeDocument))
                documentDock.ActiveDockable = activeDocument;
            else
                documentDock.ActiveDockable = documentDock.VisibleDockables?.FirstOrDefault();
        }

        foreach (var window in panelLayout.Floating)
        {
            if (window.Root is null || BuildNode(window.Root, Alignment.Left) is not { } content)
                continue;
            var windowRoot = Factory.CreateRootDock();
            windowRoot.IsCollapsable = false;
            windowRoot.VisibleDockables = Factory.CreateList(content);
            windowRoot.ActiveDockable = content;
            windowRoot.DefaultDockable = content;
            var dockWindow = Factory.CreateDockWindow();
            // As the windows Dock makes when a panel is dragged out (Dock's default title is the name of its interface)
            dockWindow.Title = string.Empty;
            dockWindow.X = window.X;
            dockWindow.Y = window.Y;
            dockWindow.Width = window.Width;
            dockWindow.Height = window.Height;
            dockWindow.Layout = windowRoot;
            dockWindow.Owner = root;
            dockWindow.Factory = Factory;
            root.Windows.Add(dockWindow);
        }

        Factory.InitLayout(root);

        // Pinned and hidden panels go through Dock, which remembers where they come back
        foreach (var pinned in panelLayout.Pinned)
        {
            var list = pinned.Side switch
            {
                PanelSide.Left => root.LeftPinnedDockables,
                PanelSide.Right => root.RightPinnedDockables,
                PanelSide.Top => root.TopPinnedDockables,
                _ => root.BottomPinnedDockables,
            };
            foreach (var panel in pinned.Panels)
            {
                if (GetOrCreateDockable(panel) is not { } dockable || dockable.Owner is not null)
                    continue;
                dockable.Owner = root;
                dockable.OriginalOwner = null;
                list!.Add(dockable);
            }
        }

        Layout = root;
        // The floating windows of the layout open once the main window shows it
        if (root.Windows.Count > 0)
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (layout == root && root.Windows.Any(x => x.Host is null))
                    root.ShowWindows?.Execute(null);
            });
        }

        foreach (var panelId in panelLayout.Hidden)
        {
            if (dockables.TryGetValue(new PanelRef(panelId), out var dockable) && IsInLayout(dockable))
                Factory.HideDockable(dockable);
        }
        QueueShownUpdate();
        // The View menu follows the new layout
        RaiseAllPanelsChanged();
    }

    private IDockable? BuildNode(PanelLayoutNode node, Alignment alignment)
    {
        switch (node)
        {
            case PanelSplit split:
            {
                var children = new List<IDockable>();
                var total = split.Proportions.Sum();
                for (var i = 0; i < split.Children.Count; ++i)
                {
                    var childAlignment = split.Orientation == PanelOrientation.Horizontal
                        ? (i == 0 ? Alignment.Left : i == split.Children.Count - 1 ? Alignment.Right : alignment)
                        : (i == split.Children.Count - 1 && i > 0 ? Alignment.Bottom : alignment);
                    if (BuildNode(split.Children[i], childAlignment) is not { } child)
                        continue;
                    child.Proportion = total > 0 && i < split.Proportions.Count ? split.Proportions[i] / total : double.NaN;
                    if (children.Count > 0)
                        children.Add(new ProportionalDockSplitter());
                    children.Add(child);
                }
                if (children.Count == 0)
                    return null;
                if (children.Count == 1)
                    return children[0];
                return new ProportionalDock
                {
                    Orientation = split.Orientation == PanelOrientation.Horizontal ? Orientation.Horizontal : Orientation.Vertical,
                    VisibleDockables = Factory.CreateList(children.ToArray()),
                };
            }

            case PanelGroup group:
            {
                var tools = group.Panels.Select(GetOrCreateDockable).OfType<PanelDockable>().Where(x => x.Owner is null).ToList();
                if (tools.Count == 0)
                    return null;
                var dock = new ToolDock
                {
                    Id = group.Id ?? string.Empty,
                    Alignment = alignment,
                    VisibleDockables = Factory.CreateList<IDockable>(tools.ToArray()),
                    ActiveDockable = tools.FirstOrDefault(x => x.Ref == group.Active) ?? tools[0],
                };
                foreach (var tool in tools)
                    tool.Owner = dock;
                if (group.Id is not null)
                    groupIds[dock] = group.Id;
                return dock;
            }

            case PanelDocuments:
                documentDock = new DocumentDock
                {
                    Id = "Documents",
                    IsCollapsable = false,
                    CanCreateDocument = false,
                    VisibleDockables = Factory.CreateList<IDockable>(),
                };
                return documentDock;

            default:
                return null;
        }
    }

    private PanelLayoutNode? SnapshotNode(IDockable dockable, List<PanelDockable> hidden, bool main)
    {
        switch (dockable)
        {
            case IRootDock root:
                return root.ActiveDockable is { } active ? SnapshotNode(active, hidden, main) : null;

            case IProportionalDock proportional:
            {
                var split = new PanelSplit { Orientation = proportional.Orientation == Orientation.Vertical ? PanelOrientation.Vertical : PanelOrientation.Horizontal };
                foreach (var child in proportional.VisibleDockables ?? [])
                {
                    if (child is IProportionalDockSplitter || SnapshotNode(child, hidden, main) is not { } node)
                        continue;
                    split.Children.Add(node);
                    split.Proportions.Add(double.IsFinite(child.Proportion) && child.Proportion > 0 ? child.Proportion : 1);
                }
                return split.Children.Count switch
                {
                    0 => null,
                    1 => split.Children[0],
                    _ => split,
                };
            }

            case IDocumentDock:
                // The editors' area of the main window; the editors themselves are not saved (OpenedAssets keeps them), nor
                // those moved to a floating window
                return main && kind == PanelLayoutKind.Editors ? new PanelDocuments() : null;

            case IDock dock:
            {
                var panels = (dock.VisibleDockables ?? []).OfType<PanelDockable>().Select(x => x.Ref).ToList();
                // The hidden panels keep their place in the group they left
                panels.AddRange(hidden.Where(x => x.OriginalOwner == dock).Select(x => x.Ref));
                if (panels.Count == 0)
                    return null;
                return new PanelGroup
                {
                    Id = groupIds.GetValueOrDefault(dock),
                    Panels = panels,
                    Active = (dock.ActiveDockable as PanelDockable)?.Ref,
                };
            }

            default:
                return null;
        }
    }

    private PanelDockable? GetOrCreateDockable(PanelRef panel)
    {
        if (dockables.TryGetValue(panel, out var existing))
            return existing;
        if (session is null || Registry.Find(panel.PanelId) is not { } descriptor || descriptor.AllowsMultipleInstances != (panel.InstanceId.Length > 0))
            return null;

        var context = new PanelContext { Descriptor = descriptor, InstanceId = panel.InstanceId, Session = session, ServiceProvider = ServiceProvider };
        if (!TryRunPanelCode(panel, () => descriptor.CreateViewModel(context), "created", out var viewModel))
            return null;
        var dockable = new PanelDockable(viewModel, descriptor.AllowsMultipleInstances, OnPanelClose, CreatePluginView);
        dockables[panel] = dockable;
        return dockable;
    }

    private void DestroyDockable(PanelDockable dockable)
    {
        dockables.Remove(dockable.Ref);
        layout?.HiddenDockables?.Remove(dockable);
        ForgetView(dockable);
        dockable.ViewModel.IsShown = false;
        dockable.Dispose();
        TryRunPanelCode(dockable.Ref, () =>
        {
            dockable.ViewModel.Destroy();
            return true;
        }, "destroyed", out _);
    }

    // The code of a panel (its view model created or destroyed) goes through the guard of the plugin host: a panel of a plugin
    // that fails is turned off for the session, and leaves the View menu (its place stays in the saved layout)
    private bool TryRunPanelCode<T>(PanelRef panel, Func<T> function, string verb, [System.Diagnostics.CodeAnalysis.MaybeNullWhen(false)] out T result)
    {
        var owner = Registry.GetOwner(panel.PanelId);
        var contribution = PluginContributions.Panel(panel.PanelId);
        if (ServiceProvider.TryGet<IAssetsPluginService>() is IPluginContributionGuard guard)
        {
            if (guard.TryRun(owner, contribution, function, out result))
                return true;
            // The guard logged it
            if (guard.IsTurnedOff(owner, contribution))
                TurnOff(panel.PanelId, owner);
            return false;
        }

        // Without a plugin host (some tests), only logged
        try
        {
            result = function();
            return true;
        }
        catch (Exception e)
        {
            Log.Error($"The panel [{panel}] could not be {verb}.", e);
            result = default;
            return false;
        }
    }

    // Unregistered later, not while the registry raises the change that is creating it
    private void TurnOff(string panelId, object? owner)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (owner is not null && Equals(Registry.GetOwner(panelId), owner))
                Registry.Unregister(panelId);
        });
    }

    /// <summary>
    /// The view of a panel that the shell does not know (a panel of a plugin): the view its plugin registered for its view
    /// model type in this editor (<see cref="IEditorPluginContext.ViewTypes"/>), then a data template of the application, else
    /// "no view for this editor", logged.
    /// </summary>
    internal Control CreatePluginView(IPanelViewModel viewModel)
    {
        var viewModelType = viewModel.GetType();
        var plugins = ServiceProvider.TryGet<IAssetsPluginService>();
        AssetsPlugin? viewOwner = null;
        var viewType = plugins is EditorPluginHost host ? host.GetViewType(viewModelType, out viewOwner) : plugins?.GetViewType(viewModelType);
        if (viewType is not null)
        {
            // The plugin that registered the view (a toolkit companion) answers for it, or else the panel's
            var owner = (object?)viewOwner ?? Registry.GetOwner(viewModel.PanelId);
            Control? view;
            if (plugins is IPluginContributionGuard guard)
            {
                guard.TryRun(owner, PluginContributions.View(viewModelType), () => (Control)Activator.CreateInstance(viewType)!, out view);
            }
            else
            {
                try
                {
                    view = (Control)Activator.CreateInstance(viewType)!;
                }
                catch (Exception e)
                {
                    Log.Error($"The view of the panel [{viewModel.PanelId}] could not be created.", e);
                    view = null;
                }
            }
            if (view is null)
                return CreatePlaceholder(Tr._p("Message", "The view of this panel failed. See the log."));
            view.DataContext = viewModel;
            return view;
        }

        if (Application.Current?.DataTemplates.Any(x => x is not DockViewLocator && x.Match(viewModel)) == true)
            return new ContentControl { Content = viewModel, DataContext = viewModel };

        var ownerName = Registry.GetOwner(viewModel.PanelId) is AssetsPlugin plugin ? plugin.GetType().FullName : "the editor";
        Log.Warning($"The panel [{viewModel.PanelId}] of [{ownerName}] has no view for this editor: no view is registered for [{viewModelType.FullName}].");
        return CreatePlaceholder(NoViewForThisEditor);
    }

    /// <summary>
    /// What a panel shows when its plugin gives no view for this editor.
    /// </summary>
    public static string NoViewForThisEditor => Tr._p("Message", "No view for this editor");

    private static TextBlock CreatePlaceholder(string text)
    {
        return new TextBlock
        {
            Text = text,
            HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Center,
            VerticalAlignment = global::Avalonia.Layout.VerticalAlignment.Center,
            TextWrapping = global::Avalonia.Media.TextWrapping.Wrap,
            Opacity = 0.5,
        };
    }

    // A panel without a place in the layout goes where the default layout puts it, or in a new group on the right
    private void AddToDefaultPlace(PanelDockable dockable)
    {
        if (layout?.ActiveDockable is not { } main)
            return;

        var descriptor = Registry.Find(dockable.Ref.PanelId);
        var groupId = PanelLayoutResolver.CreateDefault(kind, Registry).FindGroupOf(new PanelRef(dockable.Ref.PanelId))?.Id ?? descriptor?.DefaultGroup;
        var target = groupId is null ? null : groupIds.Where(x => x.Value == groupId && IsInLayout(x.Key)).Select(x => x.Key).FirstOrDefault();
        if (target is not null)
        {
            dockable.Owner = null;
            Factory.AddDockable(target, dockable);
            return;
        }

        var dock = new ToolDock
        {
            Id = groupId ?? string.Empty,
            Alignment = descriptor?.DefaultPlacement switch { PanelPlacement.Left => Alignment.Left, PanelPlacement.Bottom => Alignment.Bottom, _ => Alignment.Right },
            Proportion = 0.25,
            VisibleDockables = Factory.CreateList<IDockable>(),
        };
        if (groupId is not null)
            groupIds[dock] = groupId;
        if (main is IProportionalDock proportional)
        {
            Factory.AddDockable(proportional, new ProportionalDockSplitter());
            Factory.AddDockable(proportional, dock);
        }
        else if (main is IDock mainDock and not IDocumentDock)
        {
            dockable.Owner = null;
            Factory.AddDockable(mainDock, dockable);
            return;
        }
        dockable.Owner = null;
        Factory.AddDockable(dock, dockable);
    }

    private bool OnPanelClose(PanelDockable dockable)
    {
        // A floating window the service closes itself (the layout changes) leaves its panels to the new layout
        if (exitingWindows)
            return false;
        // Dock asks synchronously: the panel hides, or the instance closes once it agrees, after this call
        if (dockable.AllowsMultipleInstances)
            Dispatcher.UIThread.Post(() => _ = CloseInstanceAsync(dockable.ViewModel));
        else
            Dispatcher.UIThread.Post(() => Hide(dockable.Ref.PanelId));
        return false;
    }

    private bool OnDocumentClose(EditorDocument document)
    {
        if (exitingWindows)
            return false;
        // The editor closes asynchronously (it can ask to save) and the collection then removes its document. A second click
        // on the close button, or a floating window closing with several editors, does not ask twice
        if (session is not null && closingEditors.Add(document.Editor))
        {
            var editors = session.EditorCollection;
            Dispatcher.UIThread.Post(async () =>
            {
                try
                {
                    await editors.CloseAssetEditorAsync(document.Editor.Asset, null);
                }
                catch (Exception e)
                {
                    Log.Error($"The editor of [{document.Editor.Asset.Url}] could not close.", e);
                }
                finally
                {
                    closingEditors.Remove(document.Editor);
                }
            });
        }
        return false;
    }

    private void EditorsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (session is null)
            return;

        foreach (var editor in e.OldItems?.OfType<AssetEditorViewModel>() ?? [])
            RemoveDocument(editor);
        if (e.Action == NotifyCollectionChangedAction.Reset)
        {
            foreach (var editor in documents.Keys.Except(session.EditorCollection.Editors).ToList())
                RemoveDocument(editor);
        }

        if (frozen)
            return;
        var count = session.EditorCollection.Editors.Count;
        if (count > 0 && kind == PanelLayoutKind.Normal)
        {
            // The first editor brings the layout with editors, which adds them all
            SwitchTo(PanelLayoutKind.Editors);
            return;
        }
        if (count == 0 && kind == PanelLayoutKind.Editors)
        {
            SwitchTo(PanelLayoutKind.Normal);
            return;
        }
        foreach (var editor in e.NewItems?.OfType<AssetEditorViewModel>() ?? [])
            AddDocument(editor, true);
    }

    private void AddDocument(AssetEditorViewModel editor, bool activate)
    {
        if (session is null || documentDock is null)
            return;
        if (!documents.TryGetValue(editor, out var document))
            documents[editor] = document = new EditorDocument(editor, session, OnDocumentClose);
        if (document.Owner is not null)
            return;

        if (layout is null)
        {
            documentDock.VisibleDockables!.Add(document);
            document.Owner = documentDock;
            return;
        }
        Factory.AddDockable(documentDock, document);
        if (activate || editor == session.EditorCollection.ActiveEditor)
            Factory.SetActiveDockable(document);
    }

    private void RemoveDocument(AssetEditorViewModel editor)
    {
        if (!documents.Remove(editor, out var document))
            return;
        if (document.Owner is IDock)
            Factory.RemoveDockable(document, false);
        document.Owner = null;
        ForgetView(document);
        document.Dispose();
    }

    // The view of a panel or an editor that is gone: the next one with the same id (the same asset opened again) gets a new view
    private void ForgetView(IDockable dockable)
    {
        if (dockable is IDockableView { View: { } view } owner)
        {
            if (view.Parent is DockableViewHost host)
                host.Child = null;
            owner.View = null;
        }
        Views.Remove(dockable);
        if (dockable.Id is { } id)
            Views.Remove(id);
    }

    private void EditorCollectionPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(EditorCollectionViewModel.ActiveEditor) || syncingActiveEditor || session?.EditorCollection.ActiveEditor is not { } editor)
            return;
        if (documents.TryGetValue(editor, out var document) && document.Owner is IDock)
            Factory.SetActiveDockable(document);
    }

    private void ActiveDockableChanged(object? sender, ActiveDockableChangedEventArgs e)
    {
        if (e.Dockable is EditorDocument document && session is not null && session.EditorCollection.ActiveEditor != document.Editor)
        {
            syncingActiveEditor = true;
            try
            {
                session.EditorCollection.ActiveEditor = document.Editor;
            }
            finally
            {
                syncingActiveEditor = false;
            }
        }
        QueueShownUpdate();
    }

    private void DockableVisibilityChanged(IDockable? dockable)
    {
        if (dockable is PanelDockable panel)
            RaisePanelChanged(panel.Ref.PanelId);
        QueueShownUpdate();
    }

    private void RegistryChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (session is null)
            return;

        // The layout keeps the place of a panel whose plugin leaves, for its return
        if (e.OldItems?.Count > 0 && layout is not null && !frozen)
            baselines[kind] = PanelLayoutResolver.KeepUnavailablePanels(Snapshot(), baselines.GetValueOrDefault(kind), Registry);
        foreach (var descriptor in e.OldItems?.OfType<PanelDescriptor>() ?? [])
        {
            foreach (var dockable in dockables.Values.Where(x => x.Ref.PanelId == descriptor.Id).ToList())
            {
                if (dockable.Owner is IDock && !IsHidden(dockable))
                    Factory.RemoveDockable(dockable, true);
                DestroyDockable(dockable);
            }
        }
        foreach (var descriptor in e.NewItems?.OfType<PanelDescriptor>() ?? [])
        {
            if (descriptor.AllowsMultipleInstances || GetOrCreateDockable(new PanelRef(descriptor.Id)) is not { } dockable)
                continue;
            if (descriptor.VisibleByDefault)
                AddToDefaultPlace(dockable);
        }
        QueueShownUpdate();
    }

    private bool IsHidden(PanelDockable dockable) => layout?.HiddenDockables?.Contains(dockable) == true;

    // Whether a dockable is in the main window or one of its floating windows (not hidden, not left out)
    private bool IsInLayout(IDockable dockable)
    {
        if (layout is null)
            return false;
        if (layout.LeftPinnedDockables?.Contains(dockable) == true || layout.RightPinnedDockables?.Contains(dockable) == true
            || layout.TopPinnedDockables?.Contains(dockable) == true || layout.BottomPinnedDockables?.Contains(dockable) == true)
            return true;

        var current = dockable;
        for (var depth = 0; current is not null && depth < 64; ++depth)
        {
            if (current == layout)
                return true;
            if (current is IRootDock windowRoot && layout.Windows?.Any(x => x.Layout == windowRoot) == true)
                return true;
            if (current.Owner is IDock owner && current is not IRootDock && owner.VisibleDockables?.Contains(current) != true)
                return false;
            current = current.Owner;
        }
        return false;
    }

    private void QueueShownUpdate()
    {
        if (shownUpdateQueued)
            return;
        shownUpdateQueued = true;
        Dispatcher.UIThread.Post(UpdateShown, DispatcherPriority.Background);
    }

    // A panel is shown while it is the selected tab of a group of the main window or of a floating window
    private void UpdateShown()
    {
        shownUpdateQueued = false;
        foreach (var dockable in dockables.Values.ToList())
        {
            dockable.ViewModel.IsShown = layout is not null && !IsHidden(dockable) && dockable.Owner is IDock owner && owner is not IRootDock
                                         && owner.ActiveDockable == dockable && IsInLayout(dockable);
        }
    }

    private void RaisePanelChanged(string panelId) => PanelChanged?.Invoke(this, new PanelChangedEventArgs(panelId));

    private void RaiseAllPanelsChanged()
    {
        foreach (var descriptor in Registry.Panels.ToList())
            RaisePanelChanged(descriptor.Id);
    }

    // Closes the floating windows of a layout that goes. Closing a window closes its panels and editors, as when the user closes
    // it: those of a layout that goes stay as they are
    private void CloseWindows(IRootDock? root)
    {
        exitingWindows = true;
        try
        {
            foreach (var window in root?.Windows?.ToList() ?? [])
            {
                try
                {
                    window.Exit();
                }
                catch (Exception e)
                {
                    Log.Warning("A floating window could not close.", e);
                }
            }
        }
        finally
        {
            exitingWindows = false;
        }
    }
}
