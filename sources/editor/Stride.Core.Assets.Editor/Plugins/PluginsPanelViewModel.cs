// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Stride.Core.Assets.Editor.Panels;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.IO;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.Windows;
using Stride.Core.Translation;

namespace Stride.Core.Assets.Editor.Plugins;

/// <summary>
/// The Plugins panel: the plugins of the editor and of the session (<see cref="PluginCatalog"/>), what needs attention first,
/// with their state, errors and switch, and "Restart to apply" once a switch changed. It follows the plugin host's
/// <see cref="EditorPluginHost.PluginStatusChanged"/>, and empties when the session closes (the panel is destroyed).
/// </summary>
/// <remarks>
/// Toolkit-neutral: the cross-platform Game Studio shows it; the WPF one has no Plugins panel.
/// </remarks>
[Experimental("STREXP002")]
public sealed class PluginsPanelViewModel : PanelViewModel
{
    private readonly EditorPluginHost host;
    private readonly Func<IReadOnlyList<PluginInfo>> collect;
    private readonly PluginSwitchService? switchService;
    private readonly UFile? sessionFile;
    private readonly IEditorRestartService? restart;
    private readonly IDispatcherService? dispatcher;
    private readonly string baseTitle;
    private PluginItemViewModel? selectedPlugin;
    private int refreshQueued;
    private bool destroyed;
    private string summary = string.Empty;
    private int attentionCount;

    /// <summary>
    /// Creates the panel of a session.
    /// </summary>
    /// <param name="context">The panel's context: its session and the editor's services.</param>
    /// <param name="host">The plugin host of the editor.</param>
    /// <param name="sessionFile">The file the session was opened from: its plugins are switched off under it. Null when the session has none (nothing can be switched).</param>
    /// <param name="restart">Restarts the editor into the session, or null when the editor can't (no "Restart to apply").</param>
    public PluginsPanelViewModel(PanelContext context, EditorPluginHost host, UFile? sessionFile, IEditorRestartService? restart)
        : this(context, host, () => PluginCatalog.Collect(host, context.EditorSession.PackageSession), sessionFile, restart)
    {
    }

    /// <summary>
    /// Creates the panel with the plugins another source lists (tests).
    /// </summary>
    internal PluginsPanelViewModel(PanelContext context, EditorPluginHost host, Func<IReadOnlyList<PluginInfo>> collect, UFile? sessionFile, IEditorRestartService? restart)
        : base(context)
    {
        this.host = host ?? throw new ArgumentNullException(nameof(host));
        this.collect = collect ?? throw new ArgumentNullException(nameof(collect));
        this.sessionFile = sessionFile;
        this.restart = restart;
        switchService = host.DisabledCompanions is { } store ? new PluginSwitchService(store) : null;
        dispatcher = ServiceProvider.TryGet<IDispatcherService>();
        baseTitle = Title;
        RestartCommand = new AnonymousTaskCommand(ServiceProvider, () => RestartAsync(), () => restart is not null);
        RefreshCommand = new AnonymousCommand(ServiceProvider, Refresh);
        host.PluginStatusChanged += PluginStatusChanged;
        Refresh();
    }

    /// <summary>
    /// The plugins, grouped: those that need attention, those that are off, the project's, then the built-in ones.
    /// </summary>
    public ObservableList<PluginItemViewModel> Plugins { get; } = [];

    /// <summary>
    /// The plugin whose details the panel shows.
    /// </summary>
    public PluginItemViewModel? SelectedPlugin { get => selectedPlugin; set => SetValue(ref selectedPlugin, value); }

    /// <summary>
    /// How many plugins there are, and how many failed, are incompatible or are off.
    /// </summary>
    public string Summary { get => summary; private set => SetValue(ref summary, value); }

    /// <summary>
    /// How many plugins need attention (failed, incompatible, or with errors); the title shows it.
    /// </summary>
    public int AttentionCount { get => attentionCount; private set => SetValue(ref attentionCount, value); }

    /// <summary>
    /// Whether the editor runs in safe mode: the project's plugins are off, and what is switched applies when it restarts
    /// normally.
    /// </summary>
    public bool SafeMode => host.SafeMode;

    /// <summary>
    /// Whether the plugins can be switched: a session with a file, and the editor keeps the list.
    /// </summary>
    public bool CanSwitch => switchService is not null && sessionFile is not null;

    /// <summary>
    /// Whether a switch changed since the session opened: it applies when Game Studio restarts (<see cref="RestartCommand"/>).
    /// </summary>
    public bool NeedsRestart => Plugins.Any(x => x.IsPending);

    /// <summary>
    /// Whether the restart bar shows: a switch is waiting for a restart, or the editor runs in safe mode with plugins that can
    /// be switched.
    /// </summary>
    public bool ShowsRestart => restart is not null && (NeedsRestart || (SafeMode && Plugins.Any(x => x.CanSwitch)));

    /// <summary>
    /// What the restart bar says.
    /// </summary>
    public string RestartMessage => SafeMode
        ? Tr._p("Plugins", "Game Studio runs in safe mode: the plugins of the project are off. What you switch applies when it restarts normally.")
        : Tr._p("Plugins", "The plugins you switched change when Game Studio opens this project again.");

    /// <summary>
    /// The text of the restart button.
    /// </summary>
    public string RestartText => SafeMode ? Tr._p("Plugins", "Restart normally") : Tr._p("Plugins", "Restart to apply");

    /// <summary>
    /// Restarts the editor into the same session, normally (out of safe mode): the switches apply.
    /// </summary>
    public ICommandBase RestartCommand { get; }

    /// <summary>
    /// Reads the plugins again.
    /// </summary>
    public ICommandBase RefreshCommand { get; }

    /// <summary>
    /// Restarts the editor into the same session, normally (out of safe mode), after asking whether to save it.
    /// </summary>
    /// <returns>True when the editor restarts; false when the user kept the session, or the editor can't restart.</returns>
    public Task<bool> RestartAsync() => restart?.RestartAsync(safeMode: false) ?? Task.FromResult(false);

    /// <summary>
    /// Reads the plugins and their states again, keeping the selection.
    /// </summary>
    public void Refresh()
    {
        if (destroyed)
            return;

        var infos = collect();
        var byId = Plugins.ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);
        var items = new List<PluginItemViewModel>(infos.Count);
        foreach (var info in infos)
        {
            if (byId.Remove(info.Id, out var existing))
            {
                existing.Update(info);
                items.Add(existing);
            }
            else
            {
                items.Add(new PluginItemViewModel(this, info));
            }
        }
        items = [.. items.OrderBy(x => x.Group).ThenBy(x => x.DisplayName, StringComparer.CurrentCultureIgnoreCase).ThenBy(x => x.Id, StringComparer.OrdinalIgnoreCase)];
        for (var i = 0; i < items.Count; ++i)
            items[i].IsFirstOfGroup = i == 0 || items[i - 1].Group != items[i].Group;

        var selected = SelectedPlugin;
        if (!Plugins.SequenceEqual(items))
        {
            Plugins.Clear();
            Plugins.AddRange(items);
        }
        SelectedPlugin = selected is not null && items.Contains(selected) ? selected : null;
        UpdateSummary();
    }

    /// <inheritdoc />
    public override void Destroy()
    {
        if (destroyed)
            return;
        destroyed = true;
        host.PluginStatusChanged -= PluginStatusChanged;
        SelectedPlugin = null;
        Plugins.Clear();
        base.Destroy();
    }

    /// <summary>
    /// Whether the project's list of switched-off packages names a package.
    /// </summary>
    internal bool IsSwitchedOff(string packageName)
    {
        return switchService is not null && sessionFile is not null && switchService.IsSwitchedOff(sessionFile, packageName);
    }

    /// <summary>
    /// Asks whether to switch a plugin off, with what goes.
    /// </summary>
    internal async Task<bool> ConfirmSwitchOffAsync(PluginInfo info)
    {
        var summary = PluginSwitchService.DescribeSwitchOff(info);
        var message = summary.Message + Environment.NewLine + Environment.NewLine
            + Tr._p("Plugins", "It applies when Game Studio opens this project again: use Restart to apply.");
        var buttons = new[]
        {
            new DialogButtonInfo { Content = Tr._p("Button", "Switch off"), Result = 1, IsDefault = true },
            new DialogButtonInfo { Content = Tr._p("Button", "Cancel"), Result = 2, IsCancel = true },
        };
        var result = await ServiceProvider.Get<IDialogService>().MessageBoxAsync(message, buttons, MessageBoxImage.Question);
        return result == 1;
    }

    /// <summary>
    /// Switches a plugin on or off in the project's list, and updates the restart bar.
    /// </summary>
    internal bool Switch(PluginInfo info, bool on)
    {
        if (switchService is null || sessionFile is null)
            return false;
        var changed = on ? switchService.SwitchOn(sessionFile, info.Id) : switchService.SwitchOff(sessionFile, info);
        UpdateSwitches();
        return changed;
    }

    /// <summary>
    /// Removes an entry of the project's list (one that names a built-in plugin).
    /// </summary>
    internal bool ClearEntry(string packageName)
    {
        if (switchService is null || sessionFile is null)
            return false;
        var cleared = switchService.SwitchOn(sessionFile, packageName);
        UpdateSwitches();
        return cleared;
    }

    /// <summary>
    /// Copies a text to the clipboard, when the editor has one.
    /// </summary>
    internal Task CopyAsync(string text) => ServiceProvider.TryGet<IClipboardService>()?.SetTextAsync(text) ?? Task.CompletedTask;

    private void UpdateSwitches()
    {
        foreach (var item in Plugins)
            item.UpdateSwitch();
        OnPropertyChanged(nameof(NeedsRestart), nameof(ShowsRestart));
    }

    private void UpdateSummary()
    {
        var count = Plugins.Count;
        var parts = new List<string> { string.Format(Tr._pn("Plugins", "{0} plugin", "{0} plugins", count), count) };
        void Add(int value, string text)
        {
            if (value > 0)
                parts.Add(string.Format(text, value));
        }
        Add(Plugins.Count(x => x.State == PluginState.Failed), Tr._p("Plugins", "{0} failed"));
        Add(Plugins.Count(x => x.State == PluginState.Incompatible), Tr._p("Plugins", "{0} incompatible"));
        Add(Plugins.Count(x => x.State == PluginState.Joined && x.HasErrors), Tr._p("Plugins", "{0} with errors"));
        Add(Plugins.Count(x => x.State == PluginState.Off), Tr._p("Plugins", "{0} off"));
        Summary = string.Join(", ", parts);
        AttentionCount = Plugins.Count(x => x.NeedsAttention);
        Title = AttentionCount > 0 ? $"{baseTitle} ({AttentionCount})" : baseTitle;
        OnPropertyChanged(nameof(NeedsRestart), nameof(ShowsRestart), nameof(RestartMessage), nameof(RestartText), nameof(SafeMode), nameof(CanSwitch));
    }

    // Raised on the thread that changed the status (a session loading, a contribution failing): read again on the UI
    // thread, once for a burst of changes
    private void PluginStatusChanged(object? sender, PluginStatusChangedEventArgs e)
    {
        if (dispatcher is null || dispatcher.CheckAccess())
        {
            Refresh();
            return;
        }
        if (Interlocked.Exchange(ref refreshQueued, 1) == 1)
            return;
        _ = dispatcher.InvokeAsync(() =>
        {
            Interlocked.Exchange(ref refreshQueued, 0);
            Refresh();
        });
    }
}
