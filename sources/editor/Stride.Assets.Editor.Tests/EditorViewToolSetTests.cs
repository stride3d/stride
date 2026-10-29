// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Editor.EditorGame.Tools;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Tools;
using Stride.Core.Diagnostics;
using Stride.Input;

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
        Assert.Equal(new EditorViewToolClick(MouseButton.Right, ToSelection: false), rightClick);

        // A left click while the right button is down (the camera rotates) doesn't select either
        await tools.Update(Frame(pressed: [MouseButton.Left], down: [MouseButton.Left, MouseButton.Right]));
        var leftClick = Assert.Single(await tools.Update(Frame(released: [MouseButton.Left], down: [MouseButton.Right])));
        Assert.False(leftClick.ToSelection);
    }

    [Fact]
    public async Task NoClickSelectsWhileAnotherServiceHasTheMouse()
    {
        using var tools = CreateToolSet();

        await tools.Update(Frame(pressed: [MouseButton.Left], down: [MouseButton.Left]));
        var click = Assert.Single(await tools.Update(Frame(released: [MouseButton.Left], mouseAvailable: false)));

        Assert.False(click.ToSelection);
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

    private EditorViewToolSet CreateToolSet(params FakeTool[] pluginTools)
    {
        var tools = new EditorViewToolSet([select, translate, rotate, scale, .. pluginTools], [translate, rotate, scale], Keys.Space, logger);
        // The fake tools don't use the context
        tools.Initialize(null!);
        return tools;
    }

    private static async Task<IReadOnlyList<EditorViewToolClick>> Click(EditorViewToolSet tools, MouseButton button)
    {
        await tools.Update(Frame(pressed: [button], down: [button]));
        return await tools.Update(Frame(released: [button]));
    }

    private static EditorViewInputFrame Frame(MouseButton[]? pressed = null, MouseButton[]? released = null, MouseButton[]? down = null, Keys[]? keys = null, float move = 0, bool mouseAvailable = true)
    {
        return new EditorViewInputFrame
        {
            PressedButtons = (pressed ?? []).ToHashSet(),
            ReleasedButtons = (released ?? []).ToHashSet(),
            DownButtons = (down ?? []).ToHashSet(),
            PressedKeys = (keys ?? []).ToHashSet(),
            MouseDelta = new(move, 0),
            IsMouseAvailable = mouseAvailable,
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

        public void Dispose() => Calls.Add(nameof(Dispose));
    }
}
