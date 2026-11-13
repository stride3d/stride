// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core.Assets.Editor.Panels;

/// <summary>
/// Why <see cref="PanelLayoutResolver.Resolve"/> gave the default layout instead of the saved one.
/// </summary>
public enum PanelLayoutFallback
{
    /// <summary>
    /// The saved layout was used.
    /// </summary>
    None,

    /// <summary>
    /// The session has no saved layout of this kind.
    /// </summary>
    NotSaved,

    /// <summary>
    /// The saved layout has another <see cref="PanelLayout.Version"/>.
    /// </summary>
    OtherVersion,

    /// <summary>
    /// The saved layout cannot be used (no editors' area in the layout with editors, for instance).
    /// </summary>
    Invalid,
}

/// <summary>
/// A layout ready to show, and whether it is the saved one.
/// </summary>
/// <param name="Layout">The layout to show.</param>
/// <param name="Fallback">Why the default layout replaced the saved one, if it did.</param>
public sealed record PanelLayoutResolution(PanelLayout Layout, PanelLayoutFallback Fallback);

/// <summary>
/// A rectangle of the desktop, in pixels.
/// </summary>
public readonly record struct PanelBounds(double X, double Y, double Width, double Height);

/// <summary>
/// The rules that turn a saved layout into the one the shell shows, given the panels registered now, and back.
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item>A saved layout of another version, or one that cannot be used, gives the default layout.</item>
/// <item>A saved panel that is no longer registered (its plugin is off) is left out of the layout to show; the shell keeps
/// it in the layout it saves (<see cref="KeepUnavailablePanels"/>), so that it gets its place back when it returns.</item>
/// <item>A registered panel missing from the layout joins the group the default layout gives it, or the group named by
/// <see cref="PanelDescriptor.DefaultGroup"/>, or a new group at <see cref="PanelDescriptor.DefaultPlacement"/>; hidden
/// if it is not <see cref="PanelDescriptor.VisibleByDefault"/>.</item>
/// <item>When a session opens, a panel that is <see cref="PanelDescriptor.HiddenWhenSessionOpens"/> is hidden, in its place.</item>
/// <item>A group left empty is dropped, and a split left with one node is replaced by that node.</item>
/// </list>
/// </remarks>
public static class PanelLayoutResolver
{
    /// <summary>
    /// The size of a floating window that a panel opens in when it has no place.
    /// </summary>
    public static readonly PanelBounds DefaultFloatingBounds = new(100, 100, 400, 300);

    /// <summary>
    /// The layout to show for a session: its saved layout of this kind, with the registered panels, or the default one.
    /// </summary>
    /// <param name="saved">The saved layout, or null.</param>
    /// <param name="kind">Which of the two layouts of the session.</param>
    /// <param name="registry">The registered panels.</param>
    /// <param name="sessionOpening">Whether the layout is the first one of its kind shown since the session opened: the panels
    /// that are <see cref="PanelDescriptor.HiddenWhenSessionOpens"/> are then hidden.</param>
    public static PanelLayoutResolution Resolve(PanelLayout? saved, PanelLayoutKind kind, PanelRegistry registry, bool sessionOpening = false)
    {
        ArgumentNullException.ThrowIfNull(registry);

        var fallback = saved is null ? PanelLayoutFallback.NotSaved
            : saved.Version != PanelLayout.CurrentVersion ? PanelLayoutFallback.OtherVersion
            : !IsValid(saved, kind) ? PanelLayoutFallback.Invalid
            : PanelLayoutFallback.None;

        var defaults = DefaultPanelLayouts.Create(kind);
        var layout = fallback == PanelLayoutFallback.None ? saved!.Clone() : defaults.Clone();
        layout.Version = PanelLayout.CurrentVersion;
        Prune(layout, registry);
        AddMissingPanels(layout, defaults, kind, registry);
        if (sessionOpening)
        {
            foreach (var descriptor in registry.Panels.Where(x => x.HiddenWhenSessionOpens && !x.AllowsMultipleInstances))
            {
                if (!layout.Hidden.Contains(descriptor.Id))
                    layout.Hidden.Add(descriptor.Id);
            }
        }
        return new PanelLayoutResolution(layout, fallback);
    }

    /// <summary>
    /// The default layout of the given kind with the registered panels.
    /// </summary>
    public static PanelLayout CreateDefault(PanelLayoutKind kind, PanelRegistry registry) => Resolve(null, kind, registry).Layout;

    /// <summary>
    /// Whether a saved layout can be used: no missing node, and the editors' area exactly once in the main window for the
    /// layout with editors, never in the layout without them nor in a floating window.
    /// </summary>
    public static bool IsValid(PanelLayout layout, PanelLayoutKind kind)
    {
        ArgumentNullException.ThrowIfNull(layout);
        if (layout.Floating is null || layout.Pinned is null || layout.Hidden is null)
            return false;
        if (layout.Root is not null && !IsWellFormed(layout.Root))
            return false;
        if (layout.Floating.Any(x => x is null || x.Root is null || !IsWellFormed(x.Root) || x.Root.EnumerateSelfAndDescendants().OfType<PanelDocuments>().Any()))
            return false;
        if (layout.Pinned.Any(x => x?.Panels is null || x.Panels.Any(p => p is null)))
            return false;

        var documents = layout.Root?.EnumerateSelfAndDescendants().OfType<PanelDocuments>().Count() ?? 0;
        return kind == PanelLayoutKind.Editors ? documents == 1 : documents == 0;
    }

    /// <summary>
    /// Puts back, in a layout the shell is about to save, the panels of the previously saved layout that are not registered
    /// now: in the group where one of their former neighbours is, or with their pinned side. A panel whose group is gone is
    /// dropped, and goes to its default place when it returns.
    /// </summary>
    /// <param name="current">The layout of the shell, with the registered panels only.</param>
    /// <param name="previous">The layout saved before, or null.</param>
    /// <param name="registry">The registered panels.</param>
    /// <returns>A copy of <paramref name="current"/> with the unavailable panels.</returns>
    public static PanelLayout KeepUnavailablePanels(PanelLayout current, PanelLayout? previous, PanelRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(current);
        ArgumentNullException.ThrowIfNull(registry);

        var result = current.Clone();
        if (previous is null || !IsValidForMerge(previous))
            return result;

        var placed = result.EnumeratePanels().ToHashSet();
        foreach (var group in previous.EnumerateNodes().OfType<PanelGroup>())
        {
            for (var i = 0; i < group.Panels.Count; ++i)
            {
                var panel = group.Panels[i];
                if (registry.Contains(panel.PanelId) || !placed.Add(panel))
                    continue;

                var target = group.Panels.Where(x => x != panel).Select(result.FindGroupOf).FirstOrDefault(x => x is not null)
                    ?? (group.Id is not null ? result.FindGroup(group.Id) : null);
                if (target is null)
                {
                    placed.Remove(panel);
                    continue;
                }
                target.Panels.Insert(Math.Min(i, target.Panels.Count), panel);
            }
        }
        foreach (var pinned in previous.Pinned)
        {
            foreach (var panel in pinned.Panels.Where(x => !registry.Contains(x.PanelId) && placed.Add(x)))
            {
                var target = result.Pinned.FirstOrDefault(x => x.Side == pinned.Side);
                if (target is null)
                    result.Pinned.Add(target = new PinnedPanelGroup { Side = pinned.Side });
                target.Panels.Add(panel);
            }
        }
        foreach (var hidden in previous.Hidden.Where(x => !registry.Contains(x) && !result.Hidden.Contains(x)))
            result.Hidden.Add(hidden);
        return result;
    }

    /// <summary>
    /// Moves a floating window that no screen shows any more (a screen was unplugged) to the centre of the given screen,
    /// smaller if it does not fit.
    /// </summary>
    /// <param name="layout">The layout, changed in place.</param>
    /// <param name="screens">The working areas of the screens.</param>
    /// <param name="fallback">The working area of the screen of the main window.</param>
    /// <returns>True if a window moved.</returns>
    public static bool FitFloatingWindows(PanelLayout layout, IReadOnlyList<PanelBounds> screens, PanelBounds fallback)
    {
        ArgumentNullException.ThrowIfNull(layout);
        ArgumentNullException.ThrowIfNull(screens);

        // Enough of the window to grab its title bar
        const double minVisibleWidth = 50;
        const double minVisibleHeight = 30;
        var moved = false;
        foreach (var window in layout.Floating)
        {
            if (window.Width <= 0 || window.Height <= 0 || double.IsNaN(window.Width) || double.IsNaN(window.Height))
            {
                window.Width = DefaultFloatingBounds.Width;
                window.Height = DefaultFloatingBounds.Height;
                moved = true;
            }
            var visible = screens.Any(screen =>
            {
                var width = Math.Min(window.X + window.Width, screen.X + screen.Width) - Math.Max(window.X, screen.X);
                var height = Math.Min(window.Y + window.Height, screen.Y + screen.Height) - Math.Max(window.Y, screen.Y);
                // The top of the window must be on the screen, where the title bar is
                return width >= Math.Min(minVisibleWidth, window.Width) && height >= Math.Min(minVisibleHeight, window.Height)
                    && window.Y >= screen.Y && window.Y < screen.Y + screen.Height;
            });
            if (visible)
                continue;

            window.Width = Math.Min(window.Width, fallback.Width);
            window.Height = Math.Min(window.Height, fallback.Height);
            window.X = fallback.X + (fallback.Width - window.Width) / 2;
            window.Y = fallback.Y + (fallback.Height - window.Height) / 2;
            moved = true;
        }
        return moved;
    }

    private static bool IsWellFormed(PanelLayoutNode node)
    {
        return node switch
        {
            PanelSplit split => split.Children is not null && split.Proportions is not null && split.Children.All(x => x is not null && IsWellFormed(x)),
            PanelGroup group => group.Panels is not null && group.Panels.All(x => x?.PanelId is not null && x.InstanceId is not null),
            PanelDocuments => true,
            _ => false,
        };
    }

    private static bool IsValidForMerge(PanelLayout layout)
    {
        return layout.Floating is not null && layout.Pinned is not null && layout.Hidden is not null
            && (layout.Root is null || IsWellFormed(layout.Root))
            && layout.Floating.All(x => x?.Root is not null && IsWellFormed(x.Root))
            && layout.Pinned.All(x => x?.Panels is not null && x.Panels.All(p => p is not null));
    }

    // Leaves out the panels that are not registered, the instances that do not match their panel, and the panels placed twice
    private static void Prune(PanelLayout layout, PanelRegistry registry)
    {
        var placed = new HashSet<PanelRef>();
        bool Keep(PanelRef panel) => registry.Find(panel.PanelId) is { } descriptor
                                     && descriptor.AllowsMultipleInstances == (panel.InstanceId.Length > 0)
                                     && placed.Add(panel);

        layout.Root = layout.Root is null ? null : Prune(layout.Root, Keep);
        foreach (var window in layout.Floating)
            window.Root = window.Root is null ? null : Prune(window.Root, Keep);
        layout.Floating.RemoveAll(x => x.Root is null);
        foreach (var pinned in layout.Pinned)
            pinned.Panels.RemoveAll(x => !Keep(x));
        layout.Pinned.RemoveAll(x => x.Panels.Count == 0);
        layout.Hidden = layout.Hidden.Distinct().Where(x => registry.Find(x) is { AllowsMultipleInstances: false }).ToList();
    }

    private static PanelLayoutNode? Prune(PanelLayoutNode node, Func<PanelRef, bool> keep)
    {
        switch (node)
        {
            case PanelGroup group:
                group.Panels.RemoveAll(x => !keep(x));
                if (group.Active is not null && !group.Panels.Contains(group.Active))
                    group.Active = null;
                return group.Panels.Count > 0 ? group : null;

            case PanelSplit split:
                var children = new List<PanelLayoutNode>();
                var proportions = new List<double>();
                var proportionsValid = split.Proportions.Count == split.Children.Count && split.Proportions.All(x => x > 0 && double.IsFinite(x));
                for (var i = 0; i < split.Children.Count; ++i)
                {
                    if (Prune(split.Children[i], keep) is not { } child)
                        continue;
                    children.Add(child);
                    proportions.Add(proportionsValid ? split.Proportions[i] : 1);
                }
                if (children.Count <= 1)
                    return children.FirstOrDefault();
                split.Children = children;
                split.Proportions = proportions;
                return split;

            default:
                return node;
        }
    }

    private static void AddMissingPanels(PanelLayout layout, PanelLayout defaults, PanelLayoutKind kind, PanelRegistry registry)
    {
        var placed = layout.EnumeratePanels().ToHashSet();
        foreach (var descriptor in registry.Panels.Where(x => !x.AllowsMultipleInstances).OrderBy(x => x.Order))
        {
            var panel = new PanelRef(descriptor.Id);
            if (placed.Contains(panel))
                continue;

            var group = (defaults.FindGroupOf(panel)?.Id is { } defaultGroupId ? layout.FindGroup(defaultGroupId) : null)
                ?? (descriptor.DefaultGroup is not null ? layout.FindGroup(descriptor.DefaultGroup) : null);
            group ??= AddGroup(layout, descriptor, kind);
            group.Panels.Add(panel);
            placed.Add(panel);
            if (!descriptor.VisibleByDefault && !layout.Hidden.Contains(descriptor.Id))
                layout.Hidden.Add(descriptor.Id);
        }
    }

    // The group a panel without a place joins: a new one at its placement, or the centre group for a Document panel
    private static PanelGroup AddGroup(PanelLayout layout, PanelDescriptor descriptor, PanelLayoutKind kind)
    {
        var placement = descriptor.DefaultPlacement;
        if (placement == PanelPlacement.Document)
        {
            // With the asset view: in the centre without editors, at the bottom with them
            var centre = layout.FindGroup(kind == PanelLayoutKind.Normal ? DefaultPanelLayouts.CenterGroup : DefaultPanelLayouts.BottomRightGroup);
            if (centre is not null)
                return centre;
            placement = PanelPlacement.Bottom;
        }

        var group = new PanelGroup { Id = descriptor.DefaultGroup };
        if (placement == PanelPlacement.Floating)
        {
            var bounds = DefaultFloatingBounds;
            layout.Floating.Add(new FloatingPanelWindow { Root = group, X = bounds.X, Y = bounds.Y, Width = bounds.Width, Height = bounds.Height });
            return group;
        }

        if (layout.Root is null)
        {
            layout.Root = group;
            return group;
        }

        var orientation = placement == PanelPlacement.Bottom ? PanelOrientation.Vertical : PanelOrientation.Horizontal;
        var first = placement == PanelPlacement.Left;
        if (layout.Root is PanelSplit split && split.Orientation == orientation)
        {
            // A quarter of the space
            var share = split.Proportions.Sum() / 3;
            split.Children.Insert(first ? 0 : split.Children.Count, group);
            split.Proportions.Insert(first ? 0 : split.Proportions.Count, share);
            return group;
        }
        layout.Root = new PanelSplit
        {
            Orientation = orientation,
            Children = first ? [group, layout.Root] : [layout.Root, group],
            Proportions = first ? [1, 3] : [3, 1],
        };
        return group;
    }
}
