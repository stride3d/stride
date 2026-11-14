// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Stride.Core.Assets.Editor.Services;
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
    /// The services of the session, which also give those of the editor.
    /// </summary>
    public required IViewModelServiceProvider ServiceProvider { get; init; }

    /// <summary>
    /// Creates <typeparamref name="T"/>, such as the view model of the widget, by constructor injection: the longest public
    /// constructor that can be called gets the given arguments, this context if it asks for it, and the services of the
    /// session (and of the editor) it asks for.
    /// </summary>
    /// <param name="arguments">Arguments that are not services.</param>
    /// <exception cref="InvalidOperationException">No constructor can be called with these arguments and services.</exception>
    [Experimental("STREXP006")]
    public T CreateInstance<T>(params object[] arguments) => ContextInjection.CreateInstance<T>(ServiceProvider, this, arguments);
}
