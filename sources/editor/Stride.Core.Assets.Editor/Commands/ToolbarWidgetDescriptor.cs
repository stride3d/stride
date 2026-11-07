// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.Commands;

/// <summary>
/// Describes a widget of the main toolbar, after the build buttons and the command buttons: a view model that the plugin
/// creates, shown with the view that its toolkit companion registers for it (<see cref="Services.IEditorPluginContext.ViewTypes"/>).
/// A plugin registers it with <see cref="Services.IEditorPluginContext.ToolbarWidgets"/>; it goes when the plugin leaves,
/// and its view model is destroyed.
/// </summary>
/// <remarks>
/// The editor creates the view model and the view through its contribution guard: when one of them throws, the widget is
/// turned off for the session and the error is logged with the plugin's name.
/// </remarks>
[Experimental("STREXP003")]
public sealed class ToolbarWidgetDescriptor
{
    /// <summary>
    /// The identifier of the widget, "&lt;plugin&gt;.&lt;name&gt;". It must never change once released.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// The order of the widget among the widgets of the toolbar.
    /// </summary>
    public int Order { get; init; }

    /// <summary>
    /// Creates the view model of the widget, once per session; the editor destroys it when the session closes or the plugin leaves.
    /// </summary>
    public required Func<ToolbarWidgetContext, IDestroyable> CreateViewModel { get; init; }

    /// <inheritdoc />
    public override string ToString() => Id;
}
