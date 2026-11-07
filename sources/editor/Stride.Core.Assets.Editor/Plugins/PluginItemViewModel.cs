// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Extensions;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.ViewModels;
using Stride.Core.Translation;

namespace Stride.Core.Assets.Editor.Plugins;

/// <summary>
/// The groups of the Plugins panel, in the order it lists them: what needs attention first.
/// </summary>
[Experimental("STREXP002")]
public enum PluginGroup
{
    /// <summary>
    /// Failed, incompatible, or joined with errors.
    /// </summary>
    NeedsAttention,

    /// <summary>
    /// Off: switched off for the project, or left out in safe mode.
    /// </summary>
    Off,

    /// <summary>
    /// The plugins of the project (third-party), on.
    /// </summary>
    Project,

    /// <summary>
    /// The plugins of the editor and of the engine, on.
    /// </summary>
    BuiltIn,
}

/// <summary>
/// A plugin package in the Plugins panel (<see cref="PluginsPanelViewModel"/>): what it is, where it comes from, what it
/// contributes, its state and errors, and its switch.
/// </summary>
[Experimental("STREXP002")]
public sealed class PluginItemViewModel : ViewModelBase
{
    private readonly PluginsPanelViewModel owner;
    private PluginInfo info;
    private bool isFirstOfGroup;

    internal PluginItemViewModel(PluginsPanelViewModel owner, PluginInfo info)
        : base(owner.SafeArgument().ServiceProvider)
    {
        this.owner = owner;
        this.info = info ?? throw new ArgumentNullException(nameof(info));
        ToggleCommand = new AnonymousTaskCommand(ServiceProvider, () => SetOnAsync(!IsOn), () => CanSwitch);
        ClearEntryCommand = new AnonymousCommand(ServiceProvider, () => ClearEntry());
        CopyErrorsCommand = new AnonymousTaskCommand(ServiceProvider, CopyErrorsAsync);
        CopyErrorCommand = new AnonymousTaskCommand<PluginErrorViewModel>(ServiceProvider, CopyErrorAsync);
        Errors = CreateErrors(info);
    }

    /// <summary>
    /// What the editor knows of the plugin, as last read.
    /// </summary>
    public PluginInfo Info => info;

    /// <summary>
    /// The package id.
    /// </summary>
    public string Id => info.Id;

    /// <summary>
    /// The name shown.
    /// </summary>
    public string DisplayName => info.DisplayName;

    /// <summary>
    /// The version, or an empty string when unknown.
    /// </summary>
    public string VersionText => info.Version?.ToString() ?? string.Empty;

    /// <summary>
    /// Where it comes from.
    /// </summary>
    public string SourceText => info.Source switch
    {
        PluginSource.BuiltIn => Tr._p("Plugins", "Built into Game Studio"),
        PluginSource.Package => Tr._p("Plugins", "Package"),
        PluginSource.SolutionProject => Tr._p("Plugins", "Project of the solution"),
        PluginSource.DevRedirect => Tr._p("Plugins", "Development build (dev redirect)"),
        _ => info.Source.ToString(),
    };

    /// <summary>
    /// What the package carries and for which toolkit.
    /// </summary>
    public string KindText => info.Toolkit is { } toolkit ? $"{info.Kind}, {toolkit}" : $"{info.Kind}, {Tr._p("Plugins", "any toolkit")}";

    /// <summary>
    /// The authors, from the <c>.nuspec</c>.
    /// </summary>
    public string? Authors => info.Authors;

    /// <summary>
    /// The description, from the <c>.nuspec</c>.
    /// </summary>
    public string? Description => info.Description;

    /// <summary>
    /// The full path of the icon, when the <c>.nuspec</c> names one.
    /// </summary>
    public string? IconPath => info.IconPath;

    /// <summary>
    /// The project's web page.
    /// </summary>
    public string? ProjectUrl => info.ProjectUrl;

    /// <summary>
    /// The license.
    /// </summary>
    public string? License => info.License;

    /// <summary>
    /// The editor version the plugin was built for, as major.minor, or an empty string when it was not read.
    /// </summary>
    public string EditorApiText => info.EditorApiVersion?.ToString(2) ?? string.Empty;

    /// <summary>
    /// What its plugins registered and still have in place, one line per kind with its count, sorted by kind.
    /// </summary>
    public IReadOnlyList<string> Contributions => info.Contributions.OrderBy(x => x.Key, StringComparer.Ordinal).Select(x => $"{x.Key}: {x.Value}").ToList();

    /// <summary>
    /// The state of the package.
    /// </summary>
    public PluginState State => info.State;

    /// <summary>
    /// Why it is off, or null.
    /// </summary>
    public PluginOffReason? OffReason => info.OffReason;

    /// <summary>
    /// The state, in words.
    /// </summary>
    public string StateText => (info.State, info.OffReason) switch
    {
        (PluginState.Joined, _) when Errors.Count > 0 => Tr._p("Plugins", "On, with errors"),
        (PluginState.Joined, _) => Tr._p("Plugins", "On"),
        (PluginState.Failed, _) => Tr._p("Plugins", "Failed"),
        (PluginState.Incompatible, _) => Tr._p("Plugins", "Incompatible"),
        (PluginState.Off, PluginOffReason.SafeMode) => Tr._p("Plugins", "Off: safe mode"),
        (PluginState.Off, _) => Tr._p("Plugins", "Off: switched off"),
        _ => info.State.ToString(),
    };

    /// <summary>
    /// Why the plugin is in its state, in a sentence, or an empty string when it is on without errors.
    /// </summary>
    public string StateDetail => (info.State, info.OffReason) switch
    {
        (PluginState.Joined, _) when Errors.Count > 0 => Tr._p("Plugins", "Some of its contributions failed and were turned off for this session; the others work."),
        (PluginState.Failed, _) => Tr._p("Plugins", "It could not join the project: what it registered before failing was removed."),
        (PluginState.Incompatible, _) => Errors.Select(x => x.Error.Exception).OfType<IncompatiblePluginException>().FirstOrDefault()?.Message
            ?? Tr._p("Plugins", "It was built for a newer Game Studio and was not loaded."),
        (PluginState.Off, PluginOffReason.SafeMode) => Tr._p("Plugins", "Game Studio runs in safe mode: only the plugins of Stride load."),
        (PluginState.Off, _) => Tr._p("Plugins", "It is switched off for this project: it was not loaded."),
        _ => string.Empty,
    };

    /// <summary>
    /// The group the panel lists it in.
    /// </summary>
    public PluginGroup Group => info.State switch
    {
        PluginState.Failed or PluginState.Incompatible => PluginGroup.NeedsAttention,
        PluginState.Joined when Errors.Count > 0 => PluginGroup.NeedsAttention,
        PluginState.Off => PluginGroup.Off,
        _ when info.IsBuiltIn => PluginGroup.BuiltIn,
        _ => PluginGroup.Project,
    };

    /// <summary>
    /// The title of its group.
    /// </summary>
    public string GroupTitle => Group switch
    {
        PluginGroup.NeedsAttention => Tr._p("Plugins", "Needs attention"),
        PluginGroup.Off => Tr._p("Plugins", "Off"),
        PluginGroup.Project => Tr._p("Plugins", "Plugins of the project"),
        PluginGroup.BuiltIn => Tr._p("Plugins", "Built into Game Studio"),
        _ => Group.ToString(),
    };

    /// <summary>
    /// Whether it is the first of its group in the list: the panel shows the group's title above it.
    /// </summary>
    public bool IsFirstOfGroup { get => isFirstOfGroup; internal set => SetValue(ref isFirstOfGroup, value); }

    /// <summary>
    /// Whether it needs attention: failed, incompatible, or with errors.
    /// </summary>
    public bool NeedsAttention => Group == PluginGroup.NeedsAttention;

    /// <summary>
    /// Its errors, oldest first: joining, loading, and the contributions the guard turned off.
    /// </summary>
    public IReadOnlyList<PluginErrorViewModel> Errors { get; private set; }

    /// <summary>
    /// Whether it has errors.
    /// </summary>
    public bool HasErrors => Errors.Count > 0;

    /// <summary>
    /// Whether it is built in: it has no switch.
    /// </summary>
    public bool IsBuiltIn => info.IsBuiltIn;

    /// <summary>
    /// Whether the user can switch it on or off here: a third-party package, in a session with a file.
    /// </summary>
    public bool CanSwitch => info.CanSwitchOff && owner.CanSwitch;

    /// <summary>
    /// Whether it is on from the next opening of the project: not in the project's list of switched-off packages.
    /// </summary>
    public bool IsOn => !owner.IsSwitchedOff(info.Id);

    /// <summary>
    /// Whether it is loaded now: not off.
    /// </summary>
    public bool IsLoaded => info.State != PluginState.Off;

    /// <summary>
    /// Whether its switch changed since the session opened: it applies when Game Studio restarts. In safe mode, what is
    /// switched applies when it restarts normally, so nothing is pending here.
    /// </summary>
    public bool IsPending => CanSwitch && !owner.SafeMode && IsOn != IsLoaded;

    /// <summary>
    /// Whether it is built in but the project's list of switched-off packages names it: the editor loaded it anyway and warned
    /// in the load log. <see cref="ClearEntryCommand"/> removes the entry.
    /// </summary>
    public bool IsListedBuiltIn => !info.CanSwitchOff && owner.CanSwitch && owner.IsSwitchedOff(info.Id);

    /// <summary>
    /// Switches it off (after the confirmation) or on, from the next opening of the project.
    /// </summary>
    public ICommandBase ToggleCommand { get; }

    /// <summary>
    /// Removes the entry that names a built-in plugin from the project's list (<see cref="IsListedBuiltIn"/>).
    /// </summary>
    public ICommandBase ClearEntryCommand { get; }

    /// <summary>
    /// Copies all its errors, with their contribution, time and details.
    /// </summary>
    public ICommandBase CopyErrorsCommand { get; }

    /// <summary>
    /// Copies one of its errors (the parameter).
    /// </summary>
    public ICommandBase CopyErrorCommand { get; }

    /// <summary>
    /// Switches the plugin on or off for the project, from its next opening. Switching off asks first, with what goes
    /// (<see cref="PluginSwitchService.DescribeSwitchOff"/>).
    /// </summary>
    /// <param name="on">Whether to switch it on.</param>
    /// <returns>True when the list changed; false when the user cancelled, or it was already so.</returns>
    public async Task<bool> SetOnAsync(bool on)
    {
        try
        {
            if (!CanSwitch || on == IsOn)
                return false;
            if (!on && !await owner.ConfirmSwitchOffAsync(info))
                return false;
            return owner.Switch(info, on);
        }
        finally
        {
            // A switch in the view shows the new state as soon as it is clicked: it goes back if nothing changed
            OnPropertyChanged(nameof(IsOn), nameof(IsPending));
        }
    }

    /// <summary>
    /// Removes the entry that names this built-in plugin from the project's list.
    /// </summary>
    /// <returns>True when an entry was removed.</returns>
    public bool ClearEntry()
    {
        if (!IsListedBuiltIn)
            return false;
        var cleared = owner.ClearEntry(info.Id);
        OnPropertyChanged(nameof(IsListedBuiltIn));
        return cleared;
    }

    /// <summary>
    /// The text copied for all its errors.
    /// </summary>
    public string GetErrorsText()
    {
        var header = $"{info.DisplayName} ({info.Id} {VersionText}): {StateText}";
        return string.Join(Environment.NewLine + Environment.NewLine, Errors.Select(x => x.ToText()).Prepend(header));
    }

    /// <summary>
    /// Takes the plugin's new description (the panel read it again), and says what changed.
    /// </summary>
    internal void Update(PluginInfo newInfo)
    {
        info = newInfo;
        Errors = CreateErrors(newInfo);
        OnPropertyChanged(nameof(Info), nameof(DisplayName), nameof(VersionText), nameof(SourceText), nameof(KindText), nameof(Authors), nameof(Description),
            nameof(IconPath), nameof(ProjectUrl), nameof(License), nameof(EditorApiText), nameof(Contributions), nameof(State), nameof(OffReason), nameof(StateText),
            nameof(StateDetail), nameof(Group), nameof(GroupTitle), nameof(NeedsAttention), nameof(Errors), nameof(HasErrors), nameof(IsBuiltIn), nameof(CanSwitch),
            nameof(IsOn), nameof(IsLoaded), nameof(IsPending), nameof(IsListedBuiltIn));
    }

    /// <summary>
    /// The switch may have changed elsewhere (another plugin's switch, the list cleared): reads it again.
    /// </summary>
    internal void UpdateSwitch()
    {
        OnPropertyChanged(nameof(CanSwitch), nameof(IsOn), nameof(IsPending), nameof(IsListedBuiltIn));
    }

    /// <inheritdoc />
    public override string ToString() => $"{info.Id}: {StateText}";

    private static List<PluginErrorViewModel> CreateErrors(PluginInfo info)
    {
        return info.Statuses
            .SelectMany(status => status.Errors.Select(error => new PluginErrorViewModel(status.Name, error, status.TurnedOffContributions.Contains(error.Contribution))))
            .OrderBy(x => x.Time)
            .ToList();
    }

    private Task CopyErrorsAsync() => owner.CopyAsync(GetErrorsText());

    private Task CopyErrorAsync(PluginErrorViewModel? error) => error is not null ? owner.CopyAsync(error.ToText()) : Task.CompletedTask;
}
