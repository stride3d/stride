// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Stride.Core.Assets.Editor.Services;

namespace Stride.Core.Assets.Editor.Plugins;

/// <summary>
/// An error of a plugin, as the Plugins panel shows it: what failed (joining, loading, a contribution the guard turned off),
/// when, and the error itself.
/// </summary>
[Experimental("STREXP002")]
public sealed class PluginErrorViewModel
{
    /// <param name="pluginName">The plugin the error is about (<see cref="PluginStatus.Name"/>).</param>
    /// <param name="error">The error.</param>
    /// <param name="turnedOff">Whether its contribution was turned off for the session.</param>
    public PluginErrorViewModel(string pluginName, PluginError error, bool turnedOff)
    {
        ArgumentNullException.ThrowIfNull(error);
        PluginName = pluginName ?? throw new ArgumentNullException(nameof(pluginName));
        Error = error;
        IsTurnedOff = turnedOff;
    }

    /// <summary>
    /// The plugin the error is about: the full name of its type, or the name of its assembly or package when it was not loaded.
    /// </summary>
    public string PluginName { get; }

    /// <summary>
    /// The error.
    /// </summary>
    public PluginError Error { get; }

    /// <summary>
    /// What failed: <c>Join</c>, <c>Load</c>, <c>InitializeSession</c>, or a contribution (<c>Panel ...</c>, <c>Command ...</c>).
    /// </summary>
    public string Contribution => Error.Contribution;

    /// <summary>
    /// Whether the contribution was turned off for the session after it failed.
    /// </summary>
    public bool IsTurnedOff { get; }

    /// <summary>
    /// When it happened.
    /// </summary>
    public DateTimeOffset Time => Error.Time;

    /// <summary>
    /// When it happened, in local time, as the panel shows it.
    /// </summary>
    public string TimeText => Time.ToLocalTime().ToString("HH:mm:ss", CultureInfo.CurrentCulture);

    /// <summary>
    /// The message of the error.
    /// </summary>
    public string Message => Error.Exception.Message;

    /// <summary>
    /// The error with its type, inner errors and stack.
    /// </summary>
    public string Details => Error.Exception.ToString();

    /// <summary>
    /// The text copied for this error: the time, the plugin, the contribution, then the details.
    /// </summary>
    public string ToText()
    {
        var turnedOff = IsTurnedOff ? " (turned off for this session)" : string.Empty;
        return $"[{Time.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)}] {PluginName}, {Contribution}{turnedOff}{Environment.NewLine}{Details}";
    }

    /// <inheritdoc />
    public override string ToString() => $"{PluginName}, {Contribution}: {Message}";
}
