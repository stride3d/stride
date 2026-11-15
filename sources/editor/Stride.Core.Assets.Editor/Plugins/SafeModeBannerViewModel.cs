// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;
using Stride.Core.Translation;

namespace Stride.Core.Assets.Editor.Plugins;

/// <summary>
/// The banner of the main window in safe mode (<c>--safe-mode</c>): how many plugins are off, "Restart normally", and a way to
/// open the Plugins panel. It follows the plugin host's <see cref="EditorPluginHost.PluginStatusChanged"/>: the plugins left out
/// are known while the session loads.
/// </summary>
[Experimental("STREXP002")]
public sealed class SafeModeBannerViewModel : ViewModelBase, IDisposable
{
    private readonly EditorPluginHost host;
    private readonly IEditorRestartService? restart;
    private readonly IDispatcherService? dispatcher;
    private readonly Func<bool>? canOpenPluginsPanel;
    private bool isVisible;
    private int offCount;
    private string message = string.Empty;
    private int updateQueued;
    private bool disposed;

    /// <param name="serviceProvider">The editor's services.</param>
    /// <param name="host">The plugin host of the editor.</param>
    /// <param name="restart">Restarts the editor, or null when it can't ("Restart normally" is then disabled).</param>
    /// <param name="openPluginsPanel">Shows the Plugins panel; null when the editor has none.</param>
    /// <param name="canOpenPluginsPanel">Whether the Plugins panel can be shown now (a session is open).</param>
    public SafeModeBannerViewModel(IViewModelServiceProvider serviceProvider, EditorPluginHost host, IEditorRestartService? restart, Action? openPluginsPanel, Func<bool>? canOpenPluginsPanel = null)
        : base(serviceProvider)
    {
        this.host = host ?? throw new ArgumentNullException(nameof(host));
        this.restart = restart;
        this.canOpenPluginsPanel = canOpenPluginsPanel;
        dispatcher = serviceProvider.TryGet<IDispatcherService>();
        RestartNormallyCommand = new AnonymousTaskCommand(serviceProvider, () => RestartNormallyAsync(), () => restart is not null);
        OpenPluginsPanelCommand = openPluginsPanel is not null
            ? new AnonymousCommand(serviceProvider, openPluginsPanel)
            : DisabledCommand.Instance;
        host.PluginStatusChanged += PluginStatusChanged;
        Update();
    }

    /// <summary>
    /// Whether the banner shows: the editor runs in safe mode.
    /// </summary>
    public bool IsVisible { get => isVisible; private set => SetValue(ref isVisible, value); }

    /// <summary>
    /// How many plugins safe mode left out of the session.
    /// </summary>
    public int OffCount { get => offCount; private set => SetValue(ref offCount, value); }

    /// <summary>
    /// What the banner says.
    /// </summary>
    public string Message { get => message; private set => SetValue(ref message, value); }

    /// <summary>
    /// Restarts the editor into the same session (or the project selection) without safe mode.
    /// </summary>
    public ICommandBase RestartNormallyCommand { get; }

    /// <summary>
    /// Shows the Plugins panel.
    /// </summary>
    public ICommandBase OpenPluginsPanelCommand { get; }

    /// <summary>
    /// Restarts the editor without safe mode, after asking whether to save the session.
    /// </summary>
    /// <returns>True when the editor restarts (or left safe mode); false when the user kept the session, or the editor can't.</returns>
    public Task<bool> RestartNormallyAsync() => restart?.RestartAsync(safeMode: false) ?? Task.FromResult(false);

    /// <summary>
    /// Reads safe mode, the plugins it left out and whether the Plugins panel can open again (the editor turned safe mode on
    /// after the banner was made, for instance after the offer that follows a crash; a session opened or closed).
    /// </summary>
    public void Update()
    {
        if (disposed)
            return;
        IsVisible = host.SafeMode;
        OffCount = host.SafeMode ? host.PluginStatuses.Count(x => x is { State: PluginState.Off, OffReason: PluginOffReason.SafeMode }) : 0;
        Message = OffCount > 0
            ? string.Format(Tr._pn("Plugins", "Safe mode: {0} plugin of this project is off. Only the plugins of Stride are loaded.", "Safe mode: {0} plugins of this project are off. Only the plugins of Stride are loaded.", OffCount), OffCount)
            : Tr._p("Plugins", "Safe mode: only the plugins of Stride are loaded.");
        if (OpenPluginsPanelCommand is CommandBase open)
            open.IsEnabled = canOpenPluginsPanel?.Invoke() ?? true;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        disposed = true;
        host.PluginStatusChanged -= PluginStatusChanged;
    }

    // Raised on the thread that loads the session: read again on the UI thread, once for a burst of changes
    private void PluginStatusChanged(object? sender, PluginStatusChangedEventArgs e)
    {
        if (dispatcher is null || dispatcher.CheckAccess())
        {
            Update();
            return;
        }
        if (Interlocked.Exchange(ref updateQueued, 1) == 1)
            return;
        _ = dispatcher.InvokeAsync(() =>
        {
            Interlocked.Exchange(ref updateQueued, 0);
            Update();
        });
    }
}
