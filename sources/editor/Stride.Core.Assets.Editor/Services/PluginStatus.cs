// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;

#nullable enable

namespace Stride.Core.Assets.Editor.Services;

/// <summary>
/// The state of a plugin in the editor's session.
/// </summary>
[Experimental("STREXP002")]
public enum PluginState
{
    /// <summary>
    /// The plugin joined the session. Some of its contributions may have failed and been turned off (<see cref="PluginStatus.Errors"/>).
    /// </summary>
    Joined,

    /// <summary>
    /// The plugin could not join the session: what it registered before failing was removed.
    /// </summary>
    Failed,

    /// <summary>
    /// The plugin's package was not loaded: the user switched it off, or the editor runs in safe mode
    /// (<see cref="PluginStatus.OffReason"/>). It did not join the session.
    /// </summary>
    Off,

    /// <summary>
    /// The plugin was built for a newer version of the editor: its assembly was not loaded, and it did not join the session
    /// (<see cref="EditorApiCheck"/>, <see cref="IncompatiblePluginException"/>).
    /// </summary>
    Incompatible,
}

/// <summary>
/// Why a plugin is <see cref="PluginState.Off"/>.
/// </summary>
[Experimental("STREXP002")]
public enum PluginOffReason
{
    /// <summary>
    /// The user switched the plugin's package off for this session (<see cref="PluginSwitchService"/>), or an extension off
    /// everywhere (<see cref="EditorExtensionService.SetOff"/>).
    /// </summary>
    SwitchedOff,

    /// <summary>
    /// The editor runs in safe mode: only the editor's own plugins and the engine's (<see cref="BuiltInPlugins"/>) load.
    /// </summary>
    SafeMode,

    /// <summary>
    /// An extension installed after the session opened: it loads with the next session (a restart).
    /// </summary>
    AddedSinceStart,

    /// <summary>
    /// An extension that a package of the session also brings (the project uses the same plugin): the session's own version
    /// loads, not the extension.
    /// </summary>
    InProject,
}

/// <summary>
/// An error of a plugin: it could not join or initialize the session, or one of its contributions failed.
/// </summary>
[Experimental("STREXP002")]
public sealed class PluginError
{
    /// <param name="contribution">What failed (see <see cref="PluginContributions"/>).</param>
    /// <param name="exception">The error.</param>
    public PluginError(string contribution, Exception exception)
    {
        Contribution = contribution ?? throw new ArgumentNullException(nameof(contribution));
        Exception = exception ?? throw new ArgumentNullException(nameof(exception));
        Time = DateTimeOffset.Now;
    }

    /// <summary>
    /// What failed: joining, initializing the session, or a contribution (see <see cref="PluginContributions"/>).
    /// </summary>
    public string Contribution { get; }

    /// <summary>
    /// The error.
    /// </summary>
    public Exception Exception { get; }

    /// <summary>
    /// When it happened.
    /// </summary>
    public DateTimeOffset Time { get; }

    /// <inheritdoc />
    public override string ToString() => $"{Contribution}: {Exception.Message}";
}

/// <summary>
/// The state of a plugin and its errors, as the plugin host knew them when it was read (<see cref="EditorPluginHost.GetPluginStatus"/>).
/// </summary>
[Experimental("STREXP002")]
public sealed class PluginStatus
{
    internal PluginStatus(AssetsPlugin? plugin, string name, PluginState state, IReadOnlyList<PluginError> errors, IReadOnlyCollection<string> turnedOff, PluginOffReason? offReason = null)
    {
        Plugin = plugin;
        Name = name;
        State = state;
        Errors = errors;
        TurnedOffContributions = turnedOff;
        OffReason = offReason;
    }

    /// <summary>
    /// The plugin, or null when its assembly was not loaded (<see cref="PluginState.Incompatible"/>, <see cref="PluginState.Off"/>).
    /// </summary>
    public AssetsPlugin? Plugin { get; }

    /// <summary>
    /// The name of the plugin: the full name of its type; the name of its assembly when the assembly was refused
    /// (<see cref="PluginState.Incompatible"/>); the name of its package when the package was not loaded (<see cref="PluginState.Off"/>).
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Its state.
    /// </summary>
    public PluginState State { get; }

    /// <summary>
    /// Why the plugin is <see cref="PluginState.Off"/>, or null in the other states.
    /// </summary>
    public PluginOffReason? OffReason { get; }

    /// <summary>
    /// Its errors, oldest first.
    /// </summary>
    public IReadOnlyList<PluginError> Errors { get; }

    /// <summary>
    /// The contributions turned off for the session after they failed.
    /// </summary>
    public IReadOnlyCollection<string> TurnedOffContributions { get; }

    /// <inheritdoc />
    public override string ToString() => OffReason is { } reason ? $"{Name}: {State}, {reason}" : $"{Name}: {State} ({Errors.Count} errors)";
}

/// <summary>
/// The plugin a <see cref="EditorPluginHost.PluginStatusChanged"/> event is about.
/// </summary>
[Experimental("STREXP002")]
public sealed class PluginStatusChangedEventArgs(AssetsPlugin? plugin, string name, PluginStatus? status) : EventArgs
{
    /// <summary>
    /// The plugin, or null when its assembly was not loaded (<see cref="PluginState.Incompatible"/>, <see cref="PluginState.Off"/>).
    /// </summary>
    public AssetsPlugin? Plugin { get; } = plugin;

    /// <summary>
    /// The name of the plugin (<see cref="PluginStatus.Name"/>).
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    /// Its new status, or null when it left the session.
    /// </summary>
    public PluginStatus? Status { get; } = status;
}
