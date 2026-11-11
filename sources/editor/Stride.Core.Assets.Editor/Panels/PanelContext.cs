// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.Panels;

/// <summary>
/// What a panel view model is created with: its descriptor, its instance, and the session it shows.
/// </summary>
public sealed class PanelContext
{
    /// <summary>
    /// The descriptor of the panel.
    /// </summary>
    public required PanelDescriptor Descriptor { get; init; }

    /// <summary>
    /// The identifier of the instance, saved in the layouts: empty for a panel that has a single instance.
    /// </summary>
    public string InstanceId { get; init; } = string.Empty;

    /// <summary>
    /// The open session, as the plugin contexts give it.
    /// </summary>
    public required ISessionViewModel Session { get; init; }

    /// <summary>
    /// The open session with its editor plumbing, for the built-in panels.
    /// </summary>
    internal SessionViewModel EditorSession => (SessionViewModel)Session;

    /// <summary>
    /// The services of the editor.
    /// </summary>
    public required IViewModelServiceProvider ServiceProvider { get; init; }
}
