// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core.Assets.Editor.Panels;

/// <summary>
/// Shows, hides and opens the panels of the main window. Each editor's shell implements it over its docking library.
/// </summary>
public interface IPanelService
{
    /// <summary>
    /// The panels the shell can show.
    /// </summary>
    PanelRegistry Registry { get; }

    /// <summary>
    /// Whether a single panel is visible (in a group or a floating window, selected or not), or whether a panel that can be
    /// opened several times has an open instance.
    /// </summary>
    bool IsVisible(string panelId);

    /// <summary>
    /// Shows a single panel where it was last, or at its default place.
    /// </summary>
    /// <param name="panelId">The panel.</param>
    /// <param name="activate">Whether to bring it forward too (select its tab), as the Output does on an error.</param>
    void Show(string panelId, bool activate = true);

    /// <summary>
    /// Hides a single panel, which keeps its place for when it is shown again.
    /// </summary>
    void Hide(string panelId);

    /// <summary>
    /// The open instances of a panel that can be opened several times, in the order they were opened.
    /// </summary>
    IReadOnlyList<IPanelViewModel> GetInstances(string panelId);

    /// <summary>
    /// Opens a new instance of a panel that can be opened several times, at its default place, and brings it forward.
    /// </summary>
    Task<IPanelViewModel> OpenInstanceAsync(string panelId);

    /// <summary>
    /// Brings an open instance forward.
    /// </summary>
    void Activate(IPanelViewModel instance);

    /// <summary>
    /// Closes an open instance, once it agrees (<see cref="IPanelViewModel.CanCloseAsync"/>).
    /// </summary>
    /// <returns>True if the instance closed.</returns>
    Task<bool> CloseInstanceAsync(IPanelViewModel instance);

    /// <summary>
    /// Puts back the default layouts of the session: every single panel visible and in its default place, the panels of
    /// floating windows docked again. The editors stay open.
    /// </summary>
    Task ResetLayoutAsync();

    /// <summary>
    /// Raised when a panel is shown or hidden, or an instance opens or closes.
    /// </summary>
    event EventHandler<PanelChangedEventArgs>? PanelChanged;
}

/// <summary>
/// The panel that a <see cref="IPanelService.PanelChanged"/> event is about.
/// </summary>
public sealed class PanelChangedEventArgs(string panelId) : EventArgs
{
    /// <summary>
    /// The identifier of the panel.
    /// </summary>
    public string PanelId { get; } = panelId;
}
