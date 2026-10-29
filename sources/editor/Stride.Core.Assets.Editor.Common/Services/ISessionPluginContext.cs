// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Diagnostics;
using Stride.Core.Presentation.Quantum.Presenters;

#nullable enable

namespace Stride.Core.Assets.Editor.Services;

/// <summary>
/// What a plugin adds to a session whose editor is initialized, given to <see cref="AssetsPlugin.InitializeSession(ISessionPluginContext)"/>.
/// Everything registered here is removed when the plugin's assembly is unloaded, and when the editor closes the session.
/// </summary>
public interface ISessionPluginContext
{
    /// <summary>
    /// The plugin being initialized.
    /// </summary>
    AssetsPlugin Plugin { get; }

    /// <summary>
    /// The session whose editor is initialized.
    /// </summary>
    ISessionViewModel Session { get; }

    /// <summary>
    /// The logger of the session.
    /// </summary>
    ILogger Logger { get; }

    /// <summary>
    /// The services the plugin adds to the session's service provider.
    /// </summary>
    IPluginRegistry<object> Services { get; }

    /// <summary>
    /// The commands of the property grid of the session.
    /// </summary>
    IPluginRegistry<INodePresenterCommand> NodePresenterCommands { get; }

    /// <summary>
    /// The updaters of the property grid of the session.
    /// </summary>
    IPluginRegistry<INodePresenterUpdater> NodePresenterUpdaters { get; }

    /// <summary>
    /// Runs <paramref name="action"/> when the plugin leaves the session: its assembly is unloaded or the editor closes the
    /// session. The actions run newest first, interleaved with the removal of the other registrations.
    /// </summary>
    void OnClose(Action action);
}
