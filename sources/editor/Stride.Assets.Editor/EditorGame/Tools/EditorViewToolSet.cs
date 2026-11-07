// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Tools;
using Stride.Assets.Presentation.AssetEditors.Gizmos;
using Stride.Core.Diagnostics;
using Stride.Input;

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
/// <item>The active tool updates (a gizmo starts or follows its drag).</item>
/// <item>A press while the active tool uses the mouse (its gizmo is under the cursor) belongs to the tool.</item>
/// <item>A release less than <see cref="TransformationGizmo.TransformationStartPixelThreshold"/> pixels away from its
/// press is a click; the moves of the frame of the press don't count, as the cursor reaches the press position in it. A
/// drag, of the camera or of a gizmo, is never a click, and neither is a press or a release while another service has
/// the mouse (the camera takes it on the press of a button it drags with, the view cube while it is under the
/// cursor).</item>
/// <item>A left click selects when the tool keeps <see cref="EditorViewDefaultInputs.Selection"/>, the press didn't belong
/// to the tool and the right button is up. Every click then goes to the tool.</item>
/// </list>
/// The camera asks <see cref="IsDefaultInputOn"/> for its drags.
/// </para>
/// <para>
/// A tool that throws is turned off: the error is logged with the tool and its assembly, and the view goes back to the
/// default tool.
/// </para>
/// </remarks>
internal sealed class EditorViewToolSet : IDisposable
{
    private static readonly MouseButton[] ClickButtons = [MouseButton.Left, MouseButton.Middle, MouseButton.Right];

    private readonly List<IEditorViewTool> tools;
    private readonly List<IEditorViewTool> switchCycle;
    private readonly Keys switchKey;
    private readonly ILogger logger;
    private readonly HashSet<IEditorViewTool> failedTools = [];
    private readonly Dictionary<MouseButton, Press> presses = [];
    private IEditorViewTool? lastCycleTool;
    private bool isInitialized;

    /// <summary>
    /// Creates the tools of a view.
    /// </summary>
    /// <param name="tools">The tools, in toolbar order. The first one is the default tool.</param>
    /// <param name="switchCycle">The tools the switch key goes through (translate, rotate and scale). The first one is active
    /// first, as the translation in the WPF scene editor.</param>
    /// <param name="switchKey">The switch key.</param>
    /// <param name="logger">The logger of the errors of the tools.</param>
    public EditorViewToolSet(IEnumerable<IEditorViewTool> tools, IEnumerable<IEditorViewTool> switchCycle, Keys switchKey, ILogger logger)
    {
        this.tools = tools.ToList();
        if (this.tools.Count == 0)
            throw new ArgumentException("A view needs at least one tool.", nameof(tools));
        this.switchCycle = switchCycle.ToList();
        this.switchKey = switchKey;
        this.logger = logger;
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
    /// Gets the tools of the view, in toolbar order.
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
    public void Initialize(IEditorViewToolContext context)
    {
        foreach (var tool in tools)
        {
            Call(tool, x => x.Initialize(context));
        }
        isInitialized = true;
        Call(ActiveTool, x => x.Activate());
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
    /// <returns>The clicks of the frame, for the selection and the active tool.</returns>
    public async Task<IReadOnlyList<EditorViewToolClick>> Update(EditorViewInputFrame frame)
    {
        if (frame.IsMouseAvailable && frame.DownButtons.Count == 0)
            UpdateShortcuts(frame);

        var tool = ActiveTool;
        await CallAsync(tool, x => x.Update());
        var isUsingMouse = IsUsingMouse;

        List<EditorViewToolClick>? clicks = null;
        foreach (var button in ClickButtons)
        {
            if (frame.PressedButtons.Contains(button))
            {
                presses[button] = new Press { OnTool = isUsingMouse, IsMouseAvailable = frame.IsMouseAvailable };
                // The cursor can reach the press position in the frame of the press
                if (!frame.ReleasedButtons.Contains(button))
                    continue;
            }
            else if (presses.TryGetValue(button, out var press))
            {
                press.Distance += frame.MouseDelta.Length();
            }

            if (!frame.ReleasedButtons.Contains(button) || !presses.Remove(button, out var released))
                continue;
            if (released.Distance >= TransformationGizmo.TransformationStartPixelThreshold || !released.IsMouseAvailable || !frame.IsMouseAvailable)
                continue;

            var toSelection = button == MouseButton.Left && !released.OnTool && !frame.DownButtons.Contains(MouseButton.Right)
                && IsDefaultInputOn(EditorViewDefaultInputs.Selection);
            (clicks ??= []).Add(new EditorViewToolClick(button, toSelection));
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

    /// <inheritdoc />
    public void Dispose()
    {
        Call(ActiveTool, x => x.Deactivate());
        foreach (var tool in tools)
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

    private T Call<T>(IEditorViewTool tool, Func<IEditorViewTool, T> call, T fallback)
    {
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
        try
        {
            call(tool);
        }
        catch (Exception e)
        {
            TurnOff(tool, e);
        }
    }

    private async Task CallAsync(IEditorViewTool tool, Func<IEditorViewTool, Task> call)
    {
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
    private void TurnOff(IEditorViewTool tool, Exception e)
    {
        if (!failedTools.Add(tool))
            return;

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
    }
}

/// <summary>
/// A click of a view, as the arbitration of its tools decides it.
/// </summary>
/// <param name="Button">The clicked button.</param>
/// <param name="ToSelection">Whether the click selects (a left click the active tool lets the selection have).</param>
internal sealed record EditorViewToolClick(MouseButton Button, bool ToSelection);
