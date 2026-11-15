// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.Controls;

namespace Stride.GameStudio.Avalonia.Docking;

/// <summary>
/// Where the view of a panel or an editor shows in Dock's controls. The view itself lives as long as its dockable, and moves
/// to the new host when Dock shows the dockable elsewhere (another group, a floating window), so it keeps its state.
/// </summary>
/// <remarks>
/// Dock's own recycling cannot move a view out of its deferred content presenter, which lets the view go only on a later frame:
/// a host can always give its view away at once.
/// </remarks>
internal sealed class DockableViewHost : Decorator
{
    private readonly IDockableView dockable;

    public DockableViewHost(IDockableView dockable, Control view)
    {
        this.dockable = dockable;
        Take(view);
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        // A host Dock kept from a former place, shown again: the view comes back from where it went
        if (dockable.View is { } view && Child != view)
            Take(view);
    }

    private void Take(Control view)
    {
        if (view.Parent is DockableViewHost previous && previous != this)
            previous.Child = null;
        Child = view;
    }
}

/// <summary>
/// A dockable whose view is kept while it lives.
/// </summary>
internal interface IDockableView
{
    /// <summary>
    /// The view, once built; null when the dockable is gone.
    /// </summary>
    Control? View { get; set; }
}
