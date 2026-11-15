// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel;

namespace Stride.Core.Assets.Editor.Panels;

/// <summary>
/// The two layouts of a session, as in the WPF editor: one while no editor is open, where the asset view takes the centre,
/// and one while editors are open.
/// </summary>
public enum PanelLayoutKind
{
    /// <summary>
    /// The layout while no editor is open.
    /// </summary>
    Normal,

    /// <summary>
    /// The layout while editors are open: it has the editors' area (<see cref="PanelDocuments"/>).
    /// </summary>
    Editors,
}

/// <summary>
/// A side of the main window.
/// </summary>
public enum PanelSide
{
    Left,
    Right,
    Top,
    Bottom,
}

/// <summary>
/// How a <see cref="PanelSplit"/> lays its children out.
/// </summary>
public enum PanelOrientation
{
    /// <summary>
    /// Side by side.
    /// </summary>
    Horizontal,

    /// <summary>
    /// One above the other.
    /// </summary>
    Vertical,
}

/// <summary>
/// The layout of the panels of the main window, independent of any docking library: the shell builds its docking model
/// from it and writes it back from that model. It is saved in the internal settings, per session (see
/// <see cref="SessionPanelLayoutStore"/>).
/// </summary>
[DataContract("PanelLayout")]
public sealed class PanelLayout
{
    /// <summary>
    /// The version of the format. A saved layout of another version is replaced by the default one.
    /// </summary>
    /// <remarks>Increase it when a change of the default layouts or of the format must reach the saved layouts.</remarks>
    public const int CurrentVersion = 1;

    /// <summary>
    /// The version the layout was saved with.
    /// </summary>
    [DataMember(10)]
    public int Version { get; set; } = CurrentVersion;

    /// <summary>
    /// The panels docked in the main window.
    /// </summary>
    [DataMember(20)]
    public PanelLayoutNode? Root { get; set; }

    /// <summary>
    /// The floating windows.
    /// </summary>
    [DataMember(30)]
    public List<FloatingPanelWindow> Floating { get; set; } = [];

    /// <summary>
    /// The panels pinned to a side of the main window (auto-hidden).
    /// </summary>
    [DataMember(40)]
    public List<PinnedPanelGroup> Pinned { get; set; } = [];

    /// <summary>
    /// The identifiers of the single panels that are hidden. They keep their place in the layout, where they come back
    /// when shown.
    /// </summary>
    [DataMember(50)]
    public List<string> Hidden { get; set; } = [];

    /// <summary>
    /// A deep copy of the layout.
    /// </summary>
    public PanelLayout Clone()
    {
        return new PanelLayout
        {
            Version = Version,
            Root = Root?.Clone(),
            Floating = Floating.Select(x => x.Clone()).ToList(),
            Pinned = Pinned.Select(x => x.Clone()).ToList(),
            Hidden = [.. Hidden],
        };
    }

    /// <summary>
    /// Every node of the layout: the docked ones, then those of the floating windows.
    /// </summary>
    public IEnumerable<PanelLayoutNode> EnumerateNodes()
    {
        var roots = new List<PanelLayoutNode>();
        if (Root is not null)
            roots.Add(Root);
        roots.AddRange(Floating.Where(x => x.Root is not null).Select(x => x.Root!));
        return roots.SelectMany(x => x.EnumerateSelfAndDescendants());
    }

    /// <summary>
    /// Every panel the layout places: in a group, docked or floating, or pinned. Hidden ones included.
    /// </summary>
    public IEnumerable<PanelRef> EnumeratePanels()
    {
        return EnumerateNodes().OfType<PanelGroup>().SelectMany(x => x.Panels).Concat(Pinned.SelectMany(x => x.Panels));
    }

    /// <summary>
    /// The group that holds a panel, docked or floating, or null.
    /// </summary>
    public PanelGroup? FindGroupOf(PanelRef panel) => EnumerateNodes().OfType<PanelGroup>().FirstOrDefault(x => x.Panels.Contains(panel));

    /// <summary>
    /// The group with the given identifier, docked or floating, or null.
    /// </summary>
    public PanelGroup? FindGroup(string groupId) => EnumerateNodes().OfType<PanelGroup>().FirstOrDefault(x => x.Id == groupId);
}

/// <summary>
/// A node of a <see cref="PanelLayout"/>: a <see cref="PanelSplit"/>, a <see cref="PanelGroup"/> or the editors' area.
/// </summary>
[DataContract(Inherited = true)]
public abstract class PanelLayoutNode
{
    /// <summary>
    /// A deep copy of the node.
    /// </summary>
    public abstract PanelLayoutNode Clone();

    /// <summary>
    /// The node and every node below it, depth first.
    /// </summary>
    public IEnumerable<PanelLayoutNode> EnumerateSelfAndDescendants()
    {
        yield return this;
        if (this is PanelSplit split)
        {
            foreach (var node in split.Children.SelectMany(x => x.EnumerateSelfAndDescendants()))
                yield return node;
        }
    }
}

/// <summary>
/// Nodes side by side or one above the other, each with its share of the space.
/// </summary>
[DataContract("PanelSplit")]
public sealed class PanelSplit : PanelLayoutNode
{
    [DataMember(10)]
    public PanelOrientation Orientation { get; set; }

    [DataMember(20)]
    public List<PanelLayoutNode> Children { get; set; } = [];

    /// <summary>
    /// The share of each child, in the same order (relative values: only their ratios matter).
    /// </summary>
    [DataMember(30)]
    public List<double> Proportions { get; set; } = [];

    /// <inheritdoc />
    public override PanelLayoutNode Clone()
    {
        return new PanelSplit { Orientation = Orientation, Children = Children.Select(x => x.Clone()).ToList(), Proportions = [.. Proportions] };
    }
}

/// <summary>
/// Panels shown as the tabs of one group.
/// </summary>
[DataContract("PanelGroup")]
public sealed class PanelGroup : PanelLayoutNode
{
    /// <summary>
    /// The identifier of the group in the default layouts ("Stride.RightBottom"), which panels missing from a layout
    /// join; null for a group the user made.
    /// </summary>
    [DataMember(10)]
    [DefaultValue(null)]
    public string? Id { get; set; }

    [DataMember(20)]
    public List<PanelRef> Panels { get; set; } = [];

    /// <summary>
    /// The selected panel, or null for the first one.
    /// </summary>
    [DataMember(30)]
    [DefaultValue(null)]
    public PanelRef? Active { get; set; }

    /// <inheritdoc />
    public override PanelLayoutNode Clone()
    {
        return new PanelGroup { Id = Id, Panels = [.. Panels], Active = Active };
    }
}

/// <summary>
/// The area of the asset editors. The layout with editors has exactly one.
/// </summary>
[DataContract("PanelDocuments")]
public sealed class PanelDocuments : PanelLayoutNode
{
    /// <inheritdoc />
    public override PanelLayoutNode Clone() => new PanelDocuments();
}

/// <summary>
/// A panel in a layout: its identifier, and its instance for a panel that can be opened several times.
/// </summary>
[DataContract("PanelRef")]
public sealed record PanelRef
{
    public PanelRef()
    {
    }

    public PanelRef(string panelId, string instanceId = "")
    {
        PanelId = panelId;
        InstanceId = instanceId;
    }

    [DataMember(10)]
    public string PanelId { get; set; } = string.Empty;

    /// <summary>
    /// The instance: empty for a single panel.
    /// </summary>
    [DataMember(20)]
    [DefaultValue("")]
    public string InstanceId { get; set; } = string.Empty;

    /// <inheritdoc />
    public override string ToString() => InstanceId.Length == 0 ? PanelId : $"{PanelId}::{InstanceId}";
}

/// <summary>
/// A floating window and the panels it holds.
/// </summary>
[DataContract("FloatingPanelWindow")]
public sealed class FloatingPanelWindow
{
    [DataMember(10)]
    public PanelLayoutNode? Root { get; set; }

    /// <summary>
    /// The position and size of the window, in pixels of the desktop.
    /// </summary>
    [DataMember(20)]
    public double X { get; set; }

    [DataMember(30)]
    public double Y { get; set; }

    [DataMember(40)]
    public double Width { get; set; }

    [DataMember(50)]
    public double Height { get; set; }

    /// <summary>
    /// A deep copy of the window.
    /// </summary>
    public FloatingPanelWindow Clone() => new() { Root = Root?.Clone(), X = X, Y = Y, Width = Width, Height = Height };
}

/// <summary>
/// Panels pinned to a side of the main window: they show over the other panels while the mouse is on their tab.
/// </summary>
[DataContract("PinnedPanelGroup")]
public sealed class PinnedPanelGroup
{
    [DataMember(10)]
    public PanelSide Side { get; set; }

    [DataMember(20)]
    public List<PanelRef> Panels { get; set; } = [];

    /// <summary>
    /// A deep copy of the group.
    /// </summary>
    public PinnedPanelGroup Clone() => new() { Side = Side, Panels = [.. Panels] };
}
