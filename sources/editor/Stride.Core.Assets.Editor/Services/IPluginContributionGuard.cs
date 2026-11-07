// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;

#nullable enable

namespace Stride.Core.Assets.Editor.Services;

/// <summary>
/// Runs the code of the plugins' contributions (a panel's view model, a view) so that a failure stays contained: the error
/// is logged with the plugin's name and recorded in its status (<see cref="EditorPluginHost.GetPluginStatus"/>), that
/// contribution is turned off for the session, and the rest of the plugin stays. The plugin host implements it.
/// </summary>
/// <remarks>
/// The owner of a contribution is the plugin that registered it. Code of the editor itself (any other owner, or null) is run
/// the same way, but its failure is only logged: nothing is turned off.
/// </remarks>
public interface IPluginContributionGuard
{
    /// <summary>
    /// Whether a contribution of a plugin is turned off for the session, after it failed.
    /// </summary>
    /// <param name="owner">The plugin that registered the contribution.</param>
    /// <param name="contribution">The contribution (see <see cref="PluginContributions"/>).</param>
    bool IsTurnedOff(object? owner, string contribution);

    /// <summary>
    /// Runs the code of a contribution, unless it is turned off.
    /// </summary>
    /// <param name="owner">The plugin that registered the contribution.</param>
    /// <param name="contribution">The contribution (see <see cref="PluginContributions"/>).</param>
    /// <param name="action">The code.</param>
    /// <returns>True if the code ran without error; false if it failed or the contribution is turned off.</returns>
    bool TryRun(object? owner, string contribution, Action action);

    /// <summary>
    /// Runs the code of a contribution that gives a result, unless it is turned off.
    /// </summary>
    /// <param name="owner">The plugin that registered the contribution.</param>
    /// <param name="contribution">The contribution (see <see cref="PluginContributions"/>).</param>
    /// <param name="function">The code.</param>
    /// <param name="result">Its result, or the default value when it failed.</param>
    /// <returns>True if the code ran without error; false if it failed or the contribution is turned off.</returns>
    bool TryRun<T>(object? owner, string contribution, Func<T> function, [MaybeNullWhen(false)] out T result);

    /// <summary>
    /// Runs asynchronous code of a contribution, unless it is turned off: a failure before or after its first await is
    /// contained the same way.
    /// </summary>
    /// <param name="owner">The plugin that registered the contribution.</param>
    /// <param name="contribution">The contribution (see <see cref="PluginContributions"/>).</param>
    /// <param name="action">The code.</param>
    /// <returns>True if the code ran without error; false if it failed or the contribution is turned off.</returns>
    Task<bool> TryRunAsync(object? owner, string contribution, Func<Task> action);
}

/// <summary>
/// The names of what can fail in a plugin, as <see cref="PluginError.Contribution"/> and the guard (<see cref="IPluginContributionGuard"/>) give them.
/// </summary>
public static class PluginContributions
{
    /// <summary>
    /// The plugin joining the session (<see cref="AssetsPlugin.Initialize(IEditorPluginContext)"/> and the scans of its assemblies).
    /// </summary>
    public const string Join = "Join";

    /// <summary>
    /// The plugin's assembly loading: the editor's check of the editor version it was built for (<see cref="EditorApiCheck"/>).
    /// </summary>
    public const string Load = "Load";

    /// <summary>
    /// The plugin initializing the session (<see cref="AssetsPlugin.InitializeSession(ISessionPluginContext)"/>).
    /// </summary>
    public const string InitializeSession = "InitializeSession";

    /// <summary>
    /// A panel of the plugin: its view models, created and destroyed.
    /// </summary>
    public static string Panel(string panelId) => $"Panel {panelId}";

    /// <summary>
    /// The view of a view model, from the view map of the plugin (<see cref="IEditorPluginContext.ViewTypes"/>).
    /// </summary>
    public static string View(Type viewModelType) => $"View {viewModelType.FullName}";

    /// <summary>
    /// A command of the plugin (<see cref="IEditorPluginContext.Commands"/>): its title in a context, whether it can run, and
    /// running it.
    /// </summary>
    public static string Command(string commandId) => $"Command {commandId}";

    /// <summary>
    /// A toolbar widget of the plugin (<see cref="IEditorPluginContext.ToolbarWidgets"/>): its view model, created and destroyed.
    /// </summary>
    public static string ToolbarWidget(string widgetId) => $"ToolbarWidget {widgetId}";
}
