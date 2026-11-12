// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Stride.Core.Assets.Editor.Services;
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
    /// The services of the session, which also give those of the editor.
    /// </summary>
    public required IViewModelServiceProvider ServiceProvider { get; init; }

    /// <summary>
    /// Creates <typeparamref name="T"/>, such as the view model of the panel, by constructor injection: the longest public
    /// constructor that can be called gets the given arguments, this context if it asks for it, and the services of the
    /// session (and of the editor) it asks for.
    /// </summary>
    /// <example><c>CreateViewModel = context => context.CreateInstance&lt;MyPanelViewModel&gt;()</c></example>
    /// <param name="arguments">Arguments that are not services.</param>
    /// <exception cref="InvalidOperationException">No constructor can be called with these arguments and services.</exception>
    [Experimental("STREXP006")]
    public T CreateInstance<T>(params object[] arguments) => ContextInjection.CreateInstance<T>(ServiceProvider, this, arguments);
}
