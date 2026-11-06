// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Services;
using Stride.Core.Diagnostics;
using Stride.Input;
using Stride.Assets.Editor.AssetEditors.EntityHierarchyEditor.Tools;
using Stride.Assets.Editor.AssetEditors.Gizmos;

namespace Stride.Assets.Editor.EditorGame.Tools;

/// <summary>
/// The tools of one view and the arbitration of its input between the active tool, the selection and the camera (game
/// thread).
/// </summary>
/// <remarks>
/// <para>
/// The arbitration, every frame:
/// <list type="number">
/// <item>While no button is down and no other service has the mouse, the shortcut of a tool activates it, and the switch
/// key goes to the next transformation, or back to the last one from another tool.</item>
/// <item>The active tool updates (a gizmo starts or follows its drag). An update of a plugin's tool that takes longer than
/// <see cref="UpdateTimeout"/> is canceled, and the tool is turned off; the editor's own tools are waited for, as a large
/// Ctrl+drag duplication can take minutes.</item>
/// <item>A press while the active tool uses the mouse (its gizmo is under the cursor) belongs to the tool.</item>
/// <item>A release less than <see cref="TransformationGizmo.TransformationStartPixelThreshold"/> pixels away from its
/// press is a click; the moves of the frame of the press don't count, as the cursor reaches the press position in it. A
/// drag, of the camera or of a gizmo, is never a click, and neither is a press or a release while another service has
/// the mouse (the camera takes it on the press of a button it drags with, the view cube while it is under the
/// cursor).</item>
/// <item>A left click selects when the tool keeps <see cref="EditorViewDefaultInputs.Selection"/>, the press didn't belong
/// to the tool and the right button is up. Every click then goes to the tool.</item>
/// <item>A right release under the threshold opens the context menu of the view, even though the camera had the mouse for
/// its right drag, unless another button went down meanwhile, the camera keys moved the camera, or another service (the
/// view cube) had the mouse.</item>
/// </list>
/// The camera asks <see cref="IsDefaultInputOn"/> for its drags.
/// </para>
/// <para>
/// A tool that throws is turned off: the view goes back to the default tool. The tool of a plugin runs through the
/// contribution guard of the plugin host, which logs the error with the plugin, records it in the plugin's state, and keeps
/// the tool off in every view; the other tools are logged with their assembly and turned off in this view.
/// </para>
/// </remarks>
internal sealed class EditorViewToolSet : IDisposable
{
    /// <summary>
    /// The time an update of a tool can take before the view stops waiting for it.
    /// </summary>
    public static readonly TimeSpan DefaultUpdateTimeout = TimeSpan.FromSeconds(30);

    private static readonly MouseButton[] ClickButtons = [MouseButton.Left, MouseButton.Middle, MouseButton.Right];

    // Replaced, never changed: the toolbars read it on the UI thread
    private volatile IEditorViewTool[] tools;
    private readonly List<IEditorViewTool> switchCycle;
    private readonly Keys switchKey;
    private readonly ILogger logger;
    private readonly IPluginContributionGuard? guard;
    private readonly Func<IEditorViewTool, object?> ownerOf;
    private readonly HashSet<IEditorViewTool> failedTools = [];
    private readonly Dictionary<MouseButton, Press> presses = [];
    private readonly CancellationTokenSource disposal = new();
    private IEditorViewTool? lastCycleTool;
    private IEditorViewToolContext? context;
    private bool isInitialized;

    /// <summary>
    /// Creates the tools of a view.
    /// </summary>
    /// <param name="tools">The tools, in toolbar order. The first one is the default tool.</param>
    /// <param name="switchCycle">The tools the switch key goes through (translate, rotate and scale). The first one is active
    /// first, as the translation in the WPF scene editor.</param>
    /// <param name="switchKey">The switch key.</param>
    /// <param name="logger">The logger of the errors of the tools that no plugin owns.</param>
    /// <param name="guard">The contribution guard that runs the tools of the plugins, or null to run every tool as the
    /// editor's own.</param>
    /// <param name="ownerOf">The plugin that registered a tool, or null for a tool of the editor.</param>
    public EditorViewToolSet(IEnumerable<IEditorViewTool> tools, IEnumerable<IEditorViewTool> switchCycle, Keys switchKey, ILogger logger,
        IPluginContributionGuard? guard = null, Func<IEditorViewTool, object?>? ownerOf = null)
    {
        this.tools = tools.ToArray();
        if (this.tools.Length == 0)
            throw new ArgumentException("A view needs at least one tool.", nameof(tools));
        this.switchCycle = switchCycle.ToList();
        this.switchKey = switchKey;
        this.logger = logger;
        this.guard = guard;
        this.ownerOf = ownerOf ?? (_ => null);
        DefaultTool = this.tools[0];
        ActiveTool = this.switchCycle.FirstOrDefault(this.tools.Contains) ?? DefaultTool;
        if (this.switchCycle.Contains(ActiveTool))
            lastCycleTool = ActiveTool;
    }

    /// <summary>
    /// Raised when another tool becomes active (game thread).
    /// </summary>
    public event EventHandler? ActiveToolChanged;

    /// <summary>
    /// Raised when a tool is turned off after an error (game thread).
    /// </summary>
    public event EventHandler<IEditorViewTool>? ToolFailed;

    /// <summary>
    /// Raised when tools join or leave the view (game thread).
    /// </summary>
    public event EventHandler? ToolsChanged;

    /// <summary>
    /// Gets the tools of the view, in toolbar order. Any thread: the list is replaced when it changes, never changed.
    /// </summary>
    public IReadOnlyList<IEditorViewTool> Tools => tools;

    /// <summary>
    /// Gets the tool the view goes back to when the active one fails.
    /// </summary>
    public IEditorViewTool DefaultTool { get; private set; }

    /// <summary>
    /// Gets the active tool.
    /// </summary>
    public IEditorViewTool ActiveTool { get; private set; }

    /// <summary>
    /// Gets or sets the time an update of a plugin's tool can take before the view stops waiting for it and turns the tool off.
    /// The editor's own tools have no time limit.
    /// </summary>
    public TimeSpan UpdateTimeout { get; set; } = DefaultUpdateTimeout;

    /// <summary>
    /// Gets whether the active tool uses the mouse.
    /// </summary>
    public bool IsUsingMouse => Call(ActiveTool, x => x.IsUsingMouse, false);

    /// <summary>
    /// Indicates whether a tool was turned off after an error.
    /// </summary>
    public bool IsFailed(IEditorViewTool tool) => failedTools.Contains(tool);

    /// <summary>
    /// Indicates whether the active tool keeps the given default inputs on.
    /// </summary>
    public bool IsDefaultInputOn(EditorViewDefaultInputs inputs) => (Call(ActiveTool, x => x.DefaultInputs, EditorViewDefaultInputs.All) & inputs) == inputs;

    /// <summary>
    /// Initializes the tools, then activates the default one.
    /// </summary>
    public void Initialize(IEditorViewToolContext toolContext)
    {
        context = toolContext;
        foreach (var tool in tools)
        {
            Call(tool, x => x.Initialize(toolContext));
        }
        isInitialized = true;
        Call(ActiveTool, x => x.Activate());
    }

    /// <summary>
    /// Changes the tools of the view, as plugins join and leave: a new tool is initialized, a tool that leaves is
    /// deactivated (the view goes back to the default tool) and disposed.
    /// </summary>
    /// <param name="newTools">The tools, in toolbar order. The first one is the default tool.</param>
    public void SetTools(IReadOnlyList<IEditorViewTool> newTools)
    {
        ArgumentNullException.ThrowIfNull(newTools);
        if (newTools.Count == 0)
            throw new ArgumentException("A view needs at least one tool.", nameof(newTools));
        var previous = tools;
        if (previous.SequenceEqual(newTools))
            return;

        var removed = previous.Except(newTools).ToList();
        var added = newTools.Except(previous).ToList();
        tools = newTools.ToArray();
        if (!tools.Contains(DefaultTool) || failedTools.Contains(DefaultTool))
            DefaultTool = tools.FirstOrDefault(x => !failedTools.Contains(x)) ?? tools[0];
        if (removed.Contains(ActiveTool))
        {
            Call(ActiveTool, x => x.Deactivate());
            ActiveTool = DefaultTool;
            if (isInitialized)
                Call(ActiveTool, x => x.Activate());
            ActiveToolChanged?.Invoke(this, EventArgs.Empty);
        }
        foreach (var tool in removed)
        {
            DisposeTool(tool);
            failedTools.Remove(tool);
            if (lastCycleTool == tool)
                lastCycleTool = null;
        }
        if (isInitialized)
        {
            foreach (var tool in added)
                Call(tool, x => x.Initialize(context!));
        }
        ToolsChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Makes a tool the active one.
    /// </summary>
    /// <returns><c>false</c> when the tool is not a tool of the view or was turned off.</returns>
    public bool Activate(IEditorViewTool tool)
    {
        if (!tools.Contains(tool) || failedTools.Contains(tool))
            return false;
        if (tool == ActiveTool)
            return true;

        var previous = ActiveTool;
        Call(previous, x => x.Deactivate());
        ActiveTool = tool;
        if (switchCycle.Contains(tool))
            lastCycleTool = tool;
        Call(tool, x => x.Activate());
        // The tool may have failed on its activation
        if (ActiveTool == tool)
            ActiveToolChanged?.Invoke(this, EventArgs.Empty);
        return ActiveTool == tool;
    }

    /// <summary>
    /// Runs the arbitration of a frame.
    /// </summary>
    /// <param name="frame">The input of the frame.</param>
    /// <returns>The clicks of the frame, for the selection, the active tool and the context menu.</returns>
    public async Task<IReadOnlyList<EditorViewToolClick>> Update(EditorViewInputFrame frame)
    {
        if (frame.IsMouseAvailable && frame.DownButtons.Count == 0)
            UpdateShortcuts(frame);

        var tool = ActiveTool;
        await CallAsync(tool, UpdateTool);
        var isUsingMouse = IsUsingMouse;

        List<EditorViewToolClick>? clicks = null;
        foreach (var button in ClickButtons)
        {
            if (frame.PressedButtons.Contains(button))
            {
                // Another button pressed while this one is down: its release opens no context menu
                foreach (var other in presses.Values)
                    other.OtherButtonPressed = true;
                presses[button] = new Press
                {
                    OnTool = isUsingMouse,
                    IsMouseAvailable = frame.IsMouseAvailable,
                    IsMouseAvailableToContextMenu = frame.IsMouseAvailableToContextMenu,
                    HasCameraMoved = frame.HasCameraMoved,
                };
                // The cursor can reach the press position in the frame of the press
                if (!frame.ReleasedButtons.Contains(button))
                    continue;
            }
            else if (presses.TryGetValue(button, out var press))
            {
                press.Distance += frame.MouseDelta.Length();
                press.HasCameraMoved |= frame.HasCameraMoved;
            }

            if (!frame.ReleasedButtons.Contains(button) || !presses.Remove(button, out var released))
                continue;
            if (released.Distance >= TransformationGizmo.TransformationStartPixelThreshold)
                continue;

            var toTool = released.IsMouseAvailable && frame.IsMouseAvailable;
            var opensContextMenu = button == MouseButton.Right && released.IsMouseAvailableToContextMenu && frame.IsMouseAvailableToContextMenu
                && !released.OtherButtonPressed && !released.HasCameraMoved;
            if (!toTool && !opensContextMenu)
                continue;

            var toSelection = toTool && button == MouseButton.Left && !released.OnTool && !frame.DownButtons.Contains(MouseButton.Right)
                && IsDefaultInputOn(EditorViewDefaultInputs.Selection);
            (clicks ??= []).Add(new EditorViewToolClick(button, toSelection) { ToTool = toTool, OpensContextMenu = opensContextMenu });
        }
        return clicks ?? (IReadOnlyList<EditorViewToolClick>)[];
    }

    /// <summary>
    /// Gives a click to the active tool.
    /// </summary>
    public void Click(EditorViewClick click)
    {
        Call(ActiveTool, x => x.OnClick(click));
    }

    /// <summary>
    /// Asks the active tool what it adds to the context menu of a right click.
    /// </summary>
    /// <returns>The active tool and its entries (null when it has none, or failed).</returns>
    public (IEditorViewTool Tool, EditorViewContextMenu? Menu) GetContextMenu(EditorViewClick click)
    {
        var tool = ActiveTool;
        return (tool, Call(tool, x => x.GetContextMenu(click), null));
    }

    /// <summary>
    /// Runs an entry that a tool gave for the context menu.
    /// </summary>
    /// <returns><c>false</c> when the tool left the view, was turned off, or failed in the entry (it is then turned off).</returns>
    public bool RunMenuEntry(IEditorViewTool tool, EditorViewMenuEntry entry)
    {
        ArgumentNullException.ThrowIfNull(tool);
        ArgumentNullException.ThrowIfNull(entry);
        if (!tools.Contains(tool) || failedTools.Contains(tool))
            return false;
        return Call(tool, _ =>
        {
            entry.Execute();
            return true;
        }, false);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        // An update still awaited stops waiting
        disposal.Cancel();
        Call(ActiveTool, x => x.Deactivate());
        foreach (var tool in tools)
        {
            DisposeTool(tool);
        }
        disposal.Dispose();
    }

    private void DisposeTool(IEditorViewTool tool)
    {
        try
        {
            tool.Dispose();
        }
        catch (Exception e)
        {
            logger.Error($"The tool {Describe(tool)} could not be disposed.", e);
        }
    }

    private void UpdateShortcuts(EditorViewInputFrame frame)
    {
        if (frame.PressedKeys.Contains(switchKey) && switchCycle.Count > 0)
        {
            var index = switchCycle.IndexOf(ActiveTool);
            var next = index >= 0 ? switchCycle[(index + 1) % switchCycle.Count] : lastCycleTool ?? switchCycle[0];
            Activate(next);
            return;
        }

        foreach (var tool in tools)
        {
            if (tool.Shortcut != Keys.None && frame.PressedKeys.Contains(tool.Shortcut) && Activate(tool))
                return;
        }
    }

    /// <summary>
    /// Updates a tool, and stops waiting for it when the view closes, or for a plugin's tool after <see cref="UpdateTimeout"/>
    /// (it then fails).
    /// </summary>
    private async Task UpdateTool(IEditorViewTool tool)
    {
        using var timeout = new CancellationTokenSource(ownerOf(tool) is null ? Timeout.InfiniteTimeSpan : UpdateTimeout);
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(timeout.Token, disposal.Token);
        var task = tool.Update(cancellation.Token);
        try
        {
            await task.WaitAsync(cancellation.Token);
        }
        catch (OperationCanceledException) when (timeout.IsCancellationRequested)
        {
            Observe(task);
            throw new TimeoutException($"The update of the tool did not complete within {UpdateTimeout.TotalSeconds:0.###} seconds.");
        }
        catch (OperationCanceledException) when (disposal.IsCancellationRequested)
        {
            // The view closes: nothing failed
            Observe(task);
        }
    }

    // The task of an update that the view stopped waiting for may fail later: its error is not raised again
    private static void Observe(Task task)
    {
        task.ContinueWith(x => _ = x.Exception, CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
    }

    private T Call<T>(IEditorViewTool tool, Func<IEditorViewTool, T> call, T fallback)
    {
        if (guard is not null && ownerOf(tool) is AssetsPlugin owner)
        {
            if (guard.TryRun(owner, ScenePluginContributions.SceneViewTool(tool.GetType()), () => call(tool), out var result))
                return result;
            // The guard logged the error, or the tool failed in another view
            TurnOff(tool, null);
            return fallback;
        }

        try
        {
            return call(tool);
        }
        catch (Exception e)
        {
            TurnOff(tool, e);
            return fallback;
        }
    }

    private void Call(IEditorViewTool tool, Action<IEditorViewTool> call)
    {
        Call(tool, x =>
        {
            call(x);
            return true;
        }, false);
    }

    private async Task CallAsync(IEditorViewTool tool, Func<IEditorViewTool, Task> call)
    {
        if (guard is not null && ownerOf(tool) is AssetsPlugin owner)
        {
            if (!await guard.TryRunAsync(owner, ScenePluginContributions.SceneViewTool(tool.GetType()), () => call(tool)))
                TurnOff(tool, null);
            return;
        }

        try
        {
            await call(tool);
        }
        catch (Exception e)
        {
            TurnOff(tool, e);
        }
    }

    /// <summary>
    /// Turns a tool off after an error: the view goes back to the default tool, or to the next working one when the
    /// default tool failed.
    /// </summary>
    /// <param name="tool">The tool.</param>
    /// <param name="e">The error to log, or null when the guard logged it.</param>
    private void TurnOff(IEditorViewTool tool, Exception? e)
    {
        if (!tools.Contains(tool) || !failedTools.Add(tool))
            return;

        if (e is not null)
            logger.Error($"The tool {Describe(tool)} failed and is turned off.", e);
        if (tool == DefaultTool)
            DefaultTool = tools.FirstOrDefault(x => !failedTools.Contains(x)) ?? DefaultTool;
        ToolFailed?.Invoke(this, tool);

        if (tool != ActiveTool || DefaultTool == tool)
            return;

        ActiveTool = DefaultTool;
        if (!isInitialized)
            return;
        Call(ActiveTool, x => x.Activate());
        ActiveToolChanged?.Invoke(this, EventArgs.Empty);
    }

    private static string Describe(IEditorViewTool tool)
    {
        string name;
        try
        {
            name = tool.Name;
        }
        catch (Exception)
        {
            name = tool.GetType().Name;
        }
        return $"'{name}' ({tool.GetType().FullName}, {tool.GetType().Assembly.GetName().Name})";
    }

    private sealed class Press
    {
        public float Distance;
        public bool OnTool;
        public bool IsMouseAvailable;
        public bool IsMouseAvailableToContextMenu;
        public bool OtherButtonPressed;
        public bool HasCameraMoved;
    }
}

/// <summary>
/// A click of a view, as the arbitration of its tools decides it.
/// </summary>
/// <param name="Button">The clicked button.</param>
/// <param name="ToSelection">Whether the click selects (a left click the active tool lets the selection have).</param>
internal sealed record EditorViewToolClick(MouseButton Button, bool ToSelection)
{
    /// <summary>
    /// Gets whether the click goes to the active tool: false for a right click while the camera had the mouse, which only
    /// opens the context menu.
    /// </summary>
    public bool ToTool { get; init; } = true;

    /// <summary>
    /// Gets whether the click opens the context menu of the view: a right click without drag.
    /// </summary>
    public bool OpensContextMenu { get; init; }
}
