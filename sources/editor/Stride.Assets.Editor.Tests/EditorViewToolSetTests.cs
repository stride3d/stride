// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Editor.EditorGame.Tools;
using Stride.Core.Diagnostics;
using Stride.Core.Mathematics;
using Stride.Input;
using Stride.Assets.Editor.AssetEditors.EntityHierarchyEditor.Tools;

namespace Stride.Assets.Editor.Tests;

/// <summary>
/// The arbitration of the input of a view between its tools, the selection and the camera: which releases are clicks,
/// which clicks select, which keys switch the tools, and what happens to a tool that throws.
/// </summary>
public sealed class EditorViewToolSetTests
{
    private readonly FakeTool select = new("Select", Keys.Q);
    private readonly FakeTool translate = new("Translate", Keys.W);
    private readonly FakeTool rotate = new("Rotate", Keys.E);
    private readonly FakeTool scale = new("Scale", Keys.R);
    private readonly LoggerResult logger = new();

    [Fact]
    public void TheFirstTransformationIsActiveFirst()
    {
        using var tools = CreateToolSet();

        Assert.Same(translate, tools.ActiveTool);
        Assert.Same(select, tools.DefaultTool);
        Assert.Equal(["Initialize", "Activate"], translate.Calls);
        Assert.Equal(["Initialize"], select.Calls);
    }

    [Fact]
    public async Task AReleaseNearItsPressIsAClickThatSelects()
    {
        using var tools = CreateToolSet();

        // The cursor reaches the press position in the frame of the press: that move doesn't count
        Assert.Empty(await tools.Update(Frame(pressed: [MouseButton.Left], down: [MouseButton.Left], move: 50)));
        Assert.Empty(await tools.Update(Frame(down: [MouseButton.Left], move: 3)));
        var click = Assert.Single(await tools.Update(Frame(released: [MouseButton.Left], move: 3)));

        Assert.Equal(new EditorViewToolClick(MouseButton.Left, ToSelection: true), click);
    }

    [Fact]
    public async Task ADragIsNotAClick()
    {
        using var tools = CreateToolSet();

        await tools.Update(Frame(pressed: [MouseButton.Left], down: [MouseButton.Left]));
        await tools.Update(Frame(down: [MouseButton.Left], move: 5));
        await tools.Update(Frame(down: [MouseButton.Left], move: 5));

        Assert.Empty(await tools.Update(Frame(released: [MouseButton.Left])));
    }

    [Fact]
    public async Task APressOnTheGizmoOfTheToolDoesNotSelect()
    {
        using var tools = CreateToolSet();

        translate.IsUsingMouse = true;
        await tools.Update(Frame(pressed: [MouseButton.Left], down: [MouseButton.Left]));
        // The gizmo moves away from the cursor before the release
        translate.IsUsingMouse = false;
        var click = Assert.Single(await tools.Update(Frame(released: [MouseButton.Left])));

        Assert.False(click.ToSelection);
    }

    [Fact]
    public async Task OnlyALeftClickSelects()
    {
        using var tools = CreateToolSet();

        var rightClick = Assert.Single(await Click(tools, MouseButton.Right));
        Assert.Equal(new EditorViewToolClick(MouseButton.Right, ToSelection: false) { OpensContextMenu = true }, rightClick);

        // A left click while the right button is down (the camera rotates) doesn't select either
        await tools.Update(Frame(pressed: [MouseButton.Left], down: [MouseButton.Left, MouseButton.Right]));
        var leftClick = Assert.Single(await tools.Update(Frame(released: [MouseButton.Left], down: [MouseButton.Right])));
        Assert.False(leftClick.ToSelection);
    }

    [Fact]
    public async Task NoClickHappensWhileAnotherServiceHasTheMouse()
    {
        using var tools = CreateToolSet();

        // The view cube has the mouse on the release
        await tools.Update(Frame(pressed: [MouseButton.Left], down: [MouseButton.Left]));
        Assert.Empty(await tools.Update(Frame(released: [MouseButton.Left], mouseAvailable: false)));

        // The camera takes the mouse on the press of the right button, and gives it back on its release: the click only
        // opens the context menu
        await tools.Update(Frame(pressed: [MouseButton.Right], down: [MouseButton.Right], mouseAvailable: false, mouseToContextMenu: true));
        var click = Assert.Single(await tools.Update(Frame(released: [MouseButton.Right])));
        Assert.Equal(new EditorViewToolClick(MouseButton.Right, ToSelection: false) { ToTool = false, OpensContextMenu = true }, click);
    }

    [Fact]
    public async Task ARightClickWithoutDragOpensTheContextMenu()
    {
        using var tools = CreateToolSet();

        Assert.Empty(await tools.Update(Frame(pressed: [MouseButton.Right], down: [MouseButton.Right], move: 50)));
        Assert.Empty(await tools.Update(Frame(down: [MouseButton.Right], move: 3)));
        var click = Assert.Single(await tools.Update(Frame(released: [MouseButton.Right])));

        Assert.True(click.OpensContextMenu);
        Assert.True(click.ToTool);
        // A left click opens nothing
        Assert.False(Assert.Single(await Click(tools, MouseButton.Left)).OpensContextMenu);
    }

    [Fact]
    public async Task ACameraDragOpensNoContextMenu()
    {
        using var tools = CreateToolSet();

        // The camera rotates with the right button: the cursor is locked, so it doesn't move, but the camera does
        await tools.Update(Frame(pressed: [MouseButton.Right], down: [MouseButton.Right], mouseAvailable: false, mouseToContextMenu: true));
        await tools.Update(Frame(down: [MouseButton.Right], mouseAvailable: false, mouseToContextMenu: true, cameraMoved: true));
        Assert.Empty(await tools.Update(Frame(released: [MouseButton.Right])));

        // A drag of the cursor
        await tools.Update(Frame(pressed: [MouseButton.Right], down: [MouseButton.Right]));
        await tools.Update(Frame(down: [MouseButton.Right], move: 10));
        Assert.Empty(await tools.Update(Frame(released: [MouseButton.Right])));
    }

    [Fact]
    public async Task NoContextMenuOpensWhenAnotherButtonIsPressedOrAnotherServiceHasTheMouse()
    {
        using var tools = CreateToolSet();

        // The left button is pressed while the right one is down
        await tools.Update(Frame(pressed: [MouseButton.Right], down: [MouseButton.Right]));
        await tools.Update(Frame(pressed: [MouseButton.Left], down: [MouseButton.Left, MouseButton.Right]));
        await tools.Update(Frame(released: [MouseButton.Left], down: [MouseButton.Right]));
        var click = Assert.Single(await tools.Update(Frame(released: [MouseButton.Right])));
        Assert.False(click.OpensContextMenu);

        // The view cube has the mouse: neither the tool nor the menu gets the click
        await tools.Update(Frame(pressed: [MouseButton.Right], down: [MouseButton.Right], mouseAvailable: false));
        Assert.Empty(await tools.Update(Frame(released: [MouseButton.Right], mouseAvailable: false)));
    }

    [Fact]
    public void TheContextMenuOfARightClickComesFromTheActiveTool()
    {
        var menu = new EditorViewContextMenu { Entries = [new EditorViewMenuEntry { Title = "Paint here", Execute = () => { } }] };
        var paint = new FakeTool("Paint", Keys.P) { ContextMenu = menu };
        using var tools = CreateToolSet(paint);

        Assert.Null(tools.GetContextMenu(RightClick).Menu);
        tools.Activate(paint);
        var (tool, toolMenu) = tools.GetContextMenu(RightClick);

        Assert.Same(paint, tool);
        Assert.Same(menu, toolMenu);
    }

    [Fact]
    public void AMenuEntryThatThrowsTurnsItsToolOff()
    {
        var runs = 0;
        var entry = new EditorViewMenuEntry { Title = "Paint here", Execute = () => runs++ };
        var broken = new EditorViewMenuEntry { Title = "Fill", Execute = () => throw new InvalidOperationException("No paint.") };
        var paint = new FakeTool("Paint", Keys.P);
        using var tools = CreateToolSet(paint);
        tools.Activate(paint);

        Assert.True(tools.RunMenuEntry(paint, entry));
        Assert.Equal(1, runs);
        Assert.False(tools.RunMenuEntry(paint, broken));

        Assert.True(tools.IsFailed(paint));
        Assert.Same(select, tools.ActiveTool);
        Assert.Contains(logger.Messages, x => x.Type == LogMessageType.Error && x.Text.Contains("'Paint'"));
        // The entries of a tool that is off don't run
        Assert.False(tools.RunMenuEntry(paint, entry));
        Assert.Equal(1, runs);
    }

    [Fact]
    public async Task AToolWhoseUpdateNeverCompletesIsCancelledAndTurnedOff()
    {
        var paint = new FakeTool("Paint", Keys.P) { UpdateNeverCompletes = true };
        using var tools = CreateToolSet(paint);
        tools.UpdateTimeout = TimeSpan.FromMilliseconds(100);
        tools.Activate(paint);

        var update = tools.Update(Frame());

        Assert.Same(update, await Task.WhenAny(update, Task.Delay(TimeSpan.FromSeconds(10))));
        await update;
        Assert.True(paint.UpdateToken.IsCancellationRequested);
        Assert.True(tools.IsFailed(paint));
        Assert.Same(select, tools.ActiveTool);
        var error = Assert.Single(logger.Messages, x => x.Type == LogMessageType.Error);
        Assert.Contains("'Paint'", error.Text);
        Assert.Contains(nameof(TimeoutException), error.ExceptionInfo?.TypeFullName);
    }

    [Fact]
    public async Task ASlowUpdateOfAToolOfTheEditorIsWaitedFor()
    {
        // A large Ctrl+drag duplication can keep the translation busy for minutes
        var duplication = new TaskCompletionSource();
        translate.UpdateTask = duplication.Task;
        using var tools = CreateToolSet();
        tools.UpdateTimeout = TimeSpan.FromMilliseconds(100);
        tools.Activate(translate);

        var update = tools.Update(Frame());
        await Task.Delay(TimeSpan.FromMilliseconds(500));
        Assert.False(update.IsCompleted);
        duplication.SetResult();
        await update;

        Assert.False(translate.UpdateToken.IsCancellationRequested);
        Assert.False(tools.IsFailed(translate));
        Assert.Same(translate, tools.ActiveTool);
        Assert.DoesNotContain(logger.Messages, x => x.Type == LogMessageType.Error);
    }

    [Fact]
    public async Task ClosingTheViewStopsWaitingForAnUpdateWithoutAnError()
    {
        var paint = new FakeTool("Paint", Keys.P) { UpdateNeverCompletes = true };
        var tools = CreateToolSet(paint);
        tools.Activate(paint);

        var update = tools.Update(Frame());
        Assert.False(update.IsCompleted);
        tools.Dispose();

        Assert.Same(update, await Task.WhenAny(update, Task.Delay(TimeSpan.FromSeconds(10))));
        await update;
        Assert.True(paint.UpdateToken.IsCancellationRequested);
        Assert.DoesNotContain(logger.Messages, x => x.Type == LogMessageType.Error);
    }

    [Fact]
    public void TheToolsOfAViewChangeAsPluginsJoinAndLeave()
    {
        var paint = new FakeTool("Paint", Keys.P);
        var measure = new FakeTool("Measure", Keys.M);
        using var tools = CreateToolSet(paint);
        var toolsChanges = 0;
        var activeChanges = 0;
        tools.ToolsChanged += (_, _) => toolsChanges++;
        tools.ActiveToolChanged += (_, _) => activeChanges++;
        tools.Activate(paint);
        activeChanges = 0;

        // A plugin joins: its tool is initialized
        tools.SetTools([select, translate, rotate, scale, paint, measure]);
        Assert.Equal(["Initialize"], measure.Calls);
        Assert.Same(paint, tools.ActiveTool);
        Assert.Equal(1, toolsChanges);

        // The plugin of the active tool leaves: the view goes back to the default tool, and the tool is disposed
        tools.SetTools([select, translate, rotate, scale, measure]);
        Assert.Equal(["Initialize", "Activate", "Deactivate", "Dispose"], paint.Calls);
        Assert.Same(select, tools.ActiveTool);
        Assert.Equal(1, activeChanges);
        Assert.Equal(2, toolsChanges);
        Assert.False(tools.Activate(paint));

        // The same tools: nothing changes
        tools.SetTools([select, translate, rotate, scale, measure]);
        Assert.Equal(2, toolsChanges);
    }

    [Fact]
    public async Task AToolThatTakesTheDefaultInputsGetsTheClicksAlone()
    {
        var paint = new FakeTool("Paint", Keys.P, EditorViewDefaultInputs.CameraMiddleDrag);
        using var tools = CreateToolSet(paint);
        Assert.True(tools.IsDefaultInputOn(EditorViewDefaultInputs.All));

        Assert.True(tools.Activate(paint));

        Assert.False(tools.IsDefaultInputOn(EditorViewDefaultInputs.Selection));
        Assert.False(tools.IsDefaultInputOn(EditorViewDefaultInputs.CameraRightDrag));
        Assert.True(tools.IsDefaultInputOn(EditorViewDefaultInputs.CameraMiddleDrag));
        var click = Assert.Single(await Click(tools, MouseButton.Left));
        Assert.False(click.ToSelection);
    }

    [Fact]
    public async Task TheShortcutOfAToolActivatesItWhileNoButtonIsDown()
    {
        using var tools = CreateToolSet();
        var changes = 0;
        tools.ActiveToolChanged += (_, _) => changes++;

        // W, A, S, D, Q and E move the camera while a button is down
        await tools.Update(Frame(keys: [Keys.E], down: [MouseButton.Right]));
        Assert.Same(translate, tools.ActiveTool);
        // Another service (the camera) has the mouse
        await tools.Update(Frame(keys: [Keys.E], mouseAvailable: false));
        Assert.Same(translate, tools.ActiveTool);

        await tools.Update(Frame(keys: [Keys.E]));

        Assert.Same(rotate, tools.ActiveTool);
        Assert.Equal(1, changes);
        Assert.Equal(["Initialize", "Activate", "Deactivate"], translate.Calls);
        Assert.Equal(["Initialize", "Activate"], rotate.Calls);
    }

    [Fact]
    public async Task TheSwitchKeyCyclesTheTransformationsAndComesBackFromAnotherTool()
    {
        using var tools = CreateToolSet();

        await tools.Update(Frame(keys: [Keys.Space]));
        Assert.Same(rotate, tools.ActiveTool);
        await tools.Update(Frame(keys: [Keys.Space]));
        Assert.Same(scale, tools.ActiveTool);
        await tools.Update(Frame(keys: [Keys.Space]));
        Assert.Same(translate, tools.ActiveTool);

        await tools.Update(Frame(keys: [Keys.R]));
        await tools.Update(Frame(keys: [Keys.Q]));
        Assert.Same(select, tools.ActiveTool);
        await tools.Update(Frame(keys: [Keys.Space]));
        Assert.Same(scale, tools.ActiveTool);
    }

    [Fact]
    public async Task AToolThatThrowsIsTurnedOffAndLogged()
    {
        var paint = new FakeTool("Paint", Keys.P) { UpdateException = new InvalidOperationException("Broken brush") };
        using var tools = CreateToolSet(paint);
        IEditorViewTool? failedTool = null;
        tools.ToolFailed += (_, tool) => failedTool = tool;
        tools.Activate(paint);

        await tools.Update(Frame());

        Assert.Same(paint, failedTool);
        Assert.True(tools.IsFailed(paint));
        Assert.Same(select, tools.ActiveTool);
        var error = Assert.Single(logger.Messages, x => x.Type == LogMessageType.Error);
        Assert.Contains("'Paint'", error.Text);
        Assert.Contains(typeof(FakeTool).FullName!, error.Text);
        Assert.Contains(typeof(FakeTool).Assembly.GetName().Name!, error.Text);
        // The tool stays off
        Assert.False(tools.Activate(paint));
        await tools.Update(Frame(keys: [Keys.P]));
        Assert.Same(select, tools.ActiveTool);
    }

    [Fact]
    public void WhenTheDefaultToolFailsTheNextToolIsTheDefaultOne()
    {
        select.InitializeException = new InvalidOperationException("Broken select");

        using var tools = CreateToolSet();

        Assert.True(tools.IsFailed(select));
        Assert.Same(translate, tools.DefaultTool);
        Assert.Same(translate, tools.ActiveTool);
    }

    [Fact]
    public void DisposingTheToolsDeactivatesTheActiveOneAndDisposesThemAll()
    {
        var tools = CreateToolSet();

        tools.Dispose();

        Assert.Equal(["Initialize", "Activate", "Deactivate", "Dispose"], translate.Calls);
        Assert.Equal(["Initialize", "Dispose"], select.Calls);
    }

    private static EditorViewClick RightClick => new() { Button = MouseButton.Right, Position = new(0.5f), Ray = new(Vector3.Zero, -Vector3.UnitZ) };

    private EditorViewToolSet CreateToolSet(params FakeTool[] pluginTools)
    {
        var tools = new EditorViewToolSet([select, translate, rotate, scale, .. pluginTools], [translate, rotate, scale], Keys.Space, logger,
            ownerOf: tool => pluginTools.Contains(tool) ? "Plugin" : null);
        // The fake tools don't use the context
        tools.Initialize(null!);
        return tools;
    }

    private static async Task<IReadOnlyList<EditorViewToolClick>> Click(EditorViewToolSet tools, MouseButton button)
    {
        await tools.Update(Frame(pressed: [button], down: [button]));
        return await tools.Update(Frame(released: [button]));
    }

    private static EditorViewInputFrame Frame(MouseButton[]? pressed = null, MouseButton[]? released = null, MouseButton[]? down = null, Keys[]? keys = null, float move = 0, bool mouseAvailable = true,
        bool? mouseToContextMenu = null, bool cameraMoved = false)
    {
        return new EditorViewInputFrame
        {
            PressedButtons = (pressed ?? []).ToHashSet(),
            ReleasedButtons = (released ?? []).ToHashSet(),
            DownButtons = (down ?? []).ToHashSet(),
            PressedKeys = (keys ?? []).ToHashSet(),
            MouseDelta = new(move, 0),
            IsMouseAvailable = mouseAvailable,
            // Only the camera may have the mouse without keeping the context menu closed
            IsMouseAvailableToContextMenu = mouseToContextMenu ?? mouseAvailable,
            HasCameraMoved = cameraMoved,
        };
    }

    private sealed class FakeTool(string name, Keys shortcut, EditorViewDefaultInputs defaultInputs = EditorViewDefaultInputs.All) : IEditorViewTool
    {
        public string Name => name;

        public string? IconPath => null;

        public Keys Shortcut => shortcut;

        public EditorViewDefaultInputs DefaultInputs => defaultInputs;

        public bool IsUsingMouse { get; set; }

        public Exception? InitializeException { get; set; }

        public Exception? UpdateException { get; set; }

        public bool UpdateNeverCompletes { get; set; }

        public Task? UpdateTask { get; set; }

        public CancellationToken UpdateToken { get; private set; }

        public EditorViewContextMenu? ContextMenu { get; set; }

        public List<string> Calls { get; } = [];

        public void Initialize(IEditorViewToolContext context)
        {
            Calls.Add(nameof(Initialize));
            if (InitializeException is not null)
                throw InitializeException;
        }

        public void Activate() => Calls.Add(nameof(Activate));

        public void Deactivate() => Calls.Add(nameof(Deactivate));

        public Task Update() => UpdateException is null ? Task.CompletedTask : throw UpdateException;

        public Task Update(CancellationToken cancellationToken)
        {
            UpdateToken = cancellationToken;
            // A tool that ignores the cancellation: the view stops waiting anyway
            return UpdateNeverCompletes ? new TaskCompletionSource().Task : UpdateTask ?? Update();
        }

        public EditorViewContextMenu? GetContextMenu(EditorViewClick click) => ContextMenu;

        public void Dispose() => Calls.Add(nameof(Dispose));
    }
}
