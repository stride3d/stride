// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.Commands;

/// <summary>
/// What a toolbar widget's view model is created with.
/// </summary>
[Experimental("STREXP003")]
public sealed class ToolbarWidgetContext
{
    /// <summary>
    /// The descriptor of the widget.
    /// </summary>
    public required ToolbarWidgetDescriptor Descriptor { get; init; }

    /// <summary>
    /// The open session.
    /// </summary>
    public required SessionViewModel Session { get; init; }

    /// <summary>
    /// The services of the editor.
    /// </summary>
    public required IViewModelServiceProvider ServiceProvider { get; init; }
}
