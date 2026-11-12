// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Stride.Core.Diagnostics;

#nullable enable

namespace Stride.Core.Assets.Editor.Services;

/// <summary>
/// The editor's choice of the Editor companions a session loads: every one but those the user switched off for the session,
/// or in safe mode only the built-in ones (<see cref="BuiltInPlugins"/>). A built-in package loads unless the user switched
/// it off and it is one of <see cref="BuiltInPlugins.SwitchablePackageNames"/> (its toolkit companions go with it); in safe
/// mode every built-in package loads, whatever the user switched off. An extension (a companion the editor adds, with no
/// declaring package) loads unless the user switched it off, or in safe mode.
/// </summary>
[Experimental("STREXP002")]
public sealed class EditorCompanionFilter : ICompanionPackageFilter
{
    private readonly HashSet<string> switchedOff;
    private readonly HashSet<string> switchedOffExtensions;
    private readonly Action<string, PluginOffReason>? skipped;

    /// <param name="switchedOff">The names of the packages the user switched off for the session.</param>
    /// <param name="safeMode">Whether the editor runs in safe mode: only the built-in packages load, all of them.</param>
    /// <param name="skipped">Called with the name of each package left out and why, after it is logged.</param>
    /// <param name="switchedOffExtensions">The names of the extensions the user switched off (in every session).</param>
    public EditorCompanionFilter(IEnumerable<string> switchedOff, bool safeMode, Action<string, PluginOffReason>? skipped = null, IEnumerable<string>? switchedOffExtensions = null)
    {
        ArgumentNullException.ThrowIfNull(switchedOff);
        this.switchedOff = new HashSet<string>(switchedOff, StringComparer.OrdinalIgnoreCase);
        this.switchedOffExtensions = new HashSet<string>(switchedOffExtensions ?? [], StringComparer.OrdinalIgnoreCase);
        SafeMode = safeMode;
        this.skipped = skipped;
    }

    /// <summary>
    /// Whether only the built-in packages load, all of them (those the user switched off too).
    /// </summary>
    public bool SafeMode { get; }

    /// <summary>
    /// The names of the packages the user switched off for the session.
    /// </summary>
    public IReadOnlyCollection<string> SwitchedOff => switchedOff;

    /// <summary>
    /// The names of the extensions the user switched off (in every session).
    /// </summary>
    public IReadOnlyCollection<string> SwitchedOffExtensions => switchedOffExtensions;

    /// <inheritdoc />
    /// <remarks>A companion with no declaring package is an extension (<see cref="EditorExtensionService"/>).</remarks>
    public bool ShouldLoad(Package? declaringPackage, CompanionPackage companion, ILogger log)
    {
        ArgumentNullException.ThrowIfNull(companion);
        ArgumentNullException.ThrowIfNull(log);
        var name = companion.Name;
        if (name is null)
            return true;
        if (declaringPackage is null)
            return ShouldLoadExtension(name, log);

        var version = companion.Version ?? declaringPackage.Meta.Version;
        if (BuiltInPlugins.IsBuiltIn(name, version))
            return ShouldLoadBuiltIn(name, declaringPackage, log);

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

    /// <summary>
    /// Whether a built-in package is switched off for the session: itself, or the package it is switched with (a toolkit
    /// companion goes with its Editor package). Safe mode is not taken into account.
    /// </summary>
    /// <param name="name">The name of a built-in package.</param>
    public bool IsBuiltInSwitchedOff(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        return BuiltInPlugins.GetSwitchedWith(name) is { } switchedWith && switchedOff.Contains(switchedWith);
    }

    private bool ShouldLoadBuiltIn(string name, Package declaringPackage, ILogger log)
    {
        if (!IsBuiltInSwitchedOff(name))
        {
            // A toolkit companion named on its own, written by hand: it is switched with its Editor package only
            if (switchedOff.Contains(name))
                log.Warning($"[{name}] is switched off for this session, but it is switched with [{BuiltInPlugins.GetSwitchedWith(name) ?? name}], not on its own: it is loaded.");
            return true;
        }
        if (SafeMode)
        {
            // Safe mode loads Stride as shipped: the user's switch applies again on a normal start
            log.Info($"[{name}], declared by [{declaringPackage.Meta.Name}], is switched off for this session, but it is loaded: Game Studio runs in safe mode.");
            return true;
        }
        log.Info($"[{name}], declared by [{declaringPackage.Meta.Name}], is not loaded: it is switched off for this session.");
        skipped?.Invoke(name, PluginOffReason.SwitchedOff);
        return false;
    }

    private bool ShouldLoadExtension(string name, ILogger log)
    {
        PluginOffReason reason;
        if (SafeMode)
        {
            reason = PluginOffReason.SafeMode;
            log.Info($"The extension [{name}] is not loaded: Game Studio runs in safe mode.");
        }
        else if (switchedOffExtensions.Contains(name))
        {
            reason = PluginOffReason.SwitchedOff;
            log.Info($"The extension [{name}] is not loaded: it is switched off.");
        }
        else
        {
            return true;
        }
        skipped?.Invoke(name, reason);
        return false;
    }
}
