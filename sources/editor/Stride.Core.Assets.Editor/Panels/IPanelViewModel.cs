// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core.Assets.Editor.Panels;

/// <summary>
/// The view model of a panel, or of one instance of a panel that can be opened several times.
/// </summary>
public interface IPanelViewModel : IDestroyable
{
    /// <summary>
    /// The identifier of the panel (<see cref="PanelDescriptor.Id"/>).
    /// </summary>
    string PanelId { get; }

    /// <summary>
    /// The identifier of the instance: empty for a single panel.
    /// </summary>
    string InstanceId { get; }

    /// <summary>
    /// The title of the panel, which can change ("Asset errors (3)", "Output*").
    /// </summary>
    string Title { get; }

    /// <summary>
    /// Whether the panel can be seen: visible, and the selected tab of its group. The editor's shell sets it; a panel can
    /// stop work nobody sees while it is false (the asset preview stops rendering, for instance).
    /// </summary>
    bool IsShown { get; set; }

    /// <summary>
    /// Asks an instance of a panel that can be opened several times whether it can close (a single panel hides instead).
    /// </summary>
    /// <returns>False to keep it open.</returns>
    Task<bool> CanCloseAsync();
}
