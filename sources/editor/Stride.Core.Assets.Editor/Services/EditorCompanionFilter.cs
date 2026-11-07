// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Stride.Core.Diagnostics;

#nullable enable

namespace Stride.Core.Assets.Editor.Services;

/// <summary>
/// The editor's choice of the Editor companions a session loads: every one but those the user switched off for the session,
/// or in safe mode only the built-in ones (<see cref="BuiltInPlugins"/>). A built-in package always loads.
/// </summary>
[Experimental("STREXP002")]
public sealed class EditorCompanionFilter : ICompanionPackageFilter
{
    private readonly HashSet<string> switchedOff;
    private readonly Action<string, PluginOffReason>? skipped;

    /// <param name="switchedOff">The names of the packages the user switched off for the session.</param>
    /// <param name="safeMode">Whether the editor runs in safe mode: only the built-in packages load.</param>
    /// <param name="skipped">Called with the name of each package left out and why, after it is logged.</param>
    public EditorCompanionFilter(IEnumerable<string> switchedOff, bool safeMode, Action<string, PluginOffReason>? skipped = null)
    {
        ArgumentNullException.ThrowIfNull(switchedOff);
        this.switchedOff = new HashSet<string>(switchedOff, StringComparer.OrdinalIgnoreCase);
        SafeMode = safeMode;
        this.skipped = skipped;
    }

    /// <summary>
    /// Whether only the built-in packages load.
    /// </summary>
    public bool SafeMode { get; }

    /// <summary>
    /// The names of the packages the user switched off for the session.
    /// </summary>
    public IReadOnlyCollection<string> SwitchedOff => switchedOff;

    /// <inheritdoc />
    public bool ShouldLoad(Package declaringPackage, CompanionPackage companion, ILogger log)
    {
        ArgumentNullException.ThrowIfNull(declaringPackage);
        ArgumentNullException.ThrowIfNull(companion);
        ArgumentNullException.ThrowIfNull(log);
        var name = companion.Name;
        if (name is null)
            return true;

        var version = companion.Version ?? declaringPackage.Meta.Version;
        if (BuiltInPlugins.IsBuiltIn(name, version))
        {
            // Written by hand, or by an editor of another version for which the package is not built-in
            if (switchedOff.Contains(name))
                log.Warning($"[{name}] is switched off for this session, but it is a plugin of the engine and can't be: it is loaded.");
            return true;
        }

        PluginOffReason reason;
        if (SafeMode)
        {
            reason = PluginOffReason.SafeMode;
            log.Info($"[{name}], declared by [{declaringPackage.Meta.Name}], is not loaded: Game Studio runs in safe mode.");
        }
        else if (switchedOff.Contains(name))
        {
            reason = PluginOffReason.SwitchedOff;
            log.Info($"[{name}], declared by [{declaringPackage.Meta.Name}], is not loaded: it is switched off for this session.");
        }
        else
        {
            return true;
        }
        skipped?.Invoke(name, reason);
        return false;
    }
}
