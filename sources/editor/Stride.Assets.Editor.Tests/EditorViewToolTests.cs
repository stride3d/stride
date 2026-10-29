// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Editor.EditorGame.Tools;
using Stride.Assets.Editor.Tests.Helpers;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Tools;
using Stride.Assets.Presentation.AssetEditors.GameEditor.Game;
using Stride.Core;
using Stride.Core.Assets.Editor.Editors;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Diagnostics;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Input;

namespace Stride.Assets.Editor.Tests;

/// <summary>
/// The tools of the views of the scene and prefab editors: the editor's own, and the ones a plugin declares with
/// <see cref="EditorViewToolAttribute"/>, as the measure tool of these tests, which the plugin context registers.
/// </summary>
public sealed class EditorViewToolTests
{
    [Fact]
    public void TheEditorDeclaresTheSelectionAndTransformationTools()
    {
        var tools = PluginRegistrations.Of(new StrideEditorPlugin(), typeof(IAssetEditorView)).EditorViewToolTypes;

        Assert.Equal([typeof(RotateTool), typeof(ScaleTool), typeof(SelectTool), typeof(TranslateTool)], tools.OrderBy(x => x.Name));
    }

    [Fact]
    public async Task TheToolsOfAPluginComeAndGoWithThePlugin()
    {
        using var fixture = await AssetsSessionFixture.OpenAsync(initializeEditor: false);
        var host = new TestPluginService(new MeasurePlugin());
        host.RegisterSession(fixture.Session, new LoggerResult());
        try
        {
            // The plugin context registers the tools that the plugin's assembly declares
            var tools = host.GetEditorViewToolTypes();
            Assert.Contains(typeof(MeasureTool), tools);
            Assert.Contains(typeof(ToolThatCannotBeCreated), tools);
            // A tool must be a class with a public parameterless constructor: each view creates its own
            Assert.DoesNotContain(typeof(AbstractTool), tools);
            Assert.DoesNotContain(typeof(ToolWithoutParameterlessConstructor), tools);
            // The editor's own tools come from its plugin, in the session's host
            Assert.Contains(typeof(TranslateTool), fixture.Plugins.GetEditorViewToolTypes());
            Assert.DoesNotContain(typeof(MeasureTool), fixture.Plugins.GetEditorViewToolTypes());
        }
        finally
        {
            host.Dispose();
        }

        Assert.Empty(host.GetEditorViewToolTypes());
        Assert.Contains(typeof(TranslateTool), fixture.Plugins.GetEditorViewToolTypes());
    }

    [Fact]
    public async Task APluginCanRegisterAToolItself()
    {
        using var fixture = await AssetsSessionFixture.OpenAsync(initializeEditor: false);
        var host = new TestPluginService(new ToolRegisteringPlugin(typeof(UndeclaredTool)), new ToolRegisteringPlugin(typeof(UndeclaredTool)));
        host.RegisterSession(fixture.Session, new LoggerResult());
        try
        {
            // Two plugins register the same tool: the views create it once
            Assert.Single(host.GetEditorViewToolTypes(), x => x == typeof(UndeclaredTool));
            Assert.Equal(2, host.EditorViewToolTypes.Count(x => x == typeof(UndeclaredTool)));
        }
        finally
        {
            host.Dispose();
        }
        Assert.Empty(host.EditorViewToolTypes);
    }

    [Fact]
    public async Task APluginThatRegistersAToolTheViewsCannotCreateDoesNotJoin()
    {
        using var fixture = await AssetsSessionFixture.OpenAsync(initializeEditor: false);
        var plugin = new ToolRegisteringPlugin(typeof(AbstractTool));
        var logger = new LoggerResult();
        var host = new TestPluginService(plugin);
        host.RegisterSession(fixture.Session, logger);
        try
        {
            Assert.DoesNotContain(plugin, host.SessionPlugins);
            Assert.Empty(host.EditorViewToolTypes);
        }
        finally
        {
            host.Dispose();
        }
    }

    [Fact]
    public void AViewLeavesOutATypeThatIsNotATool()
    {
        var logger = new LoggerResult();

        var tools = EditorGameToolService.CreateTools([typeof(MeasurePlugin), typeof(TranslateTool)], logger);

        Assert.Equal([typeof(SelectTool), typeof(TranslateTool)], tools.Select(x => x.GetType()));
        var error = Assert.Single(logger.Messages, x => x.Type == LogMessageType.Error);
        Assert.Contains(typeof(MeasurePlugin).FullName!, error.Text);
    }

    [Fact]
    public void EachViewCreatesTheToolsInToolbarOrder()
    {
        var logger = new LoggerResult();

        var tools = EditorGameToolService.CreateTools([typeof(MeasureTool), typeof(ScaleTool), typeof(ToolThatCannotBeCreated), typeof(TranslateTool), typeof(RotateTool), typeof(SelectTool)], logger);

        Assert.Equal([typeof(SelectTool), typeof(TranslateTool), typeof(RotateTool), typeof(ScaleTool), typeof(MeasureTool)], tools.Select(x => x.GetType()));
        var error = Assert.Single(logger.Messages, x => x.Type == LogMessageType.Error);
        Assert.Contains(typeof(ToolThatCannotBeCreated).FullName!, error.Text);
        Assert.Contains(typeof(ToolThatCannotBeCreated).Assembly.GetName().Name!, error.Text);
    }

    [Fact]
    public void AViewWithoutTheEditorToolsStillHasTheSelectTool()
    {
        var tools = EditorGameToolService.CreateTools([typeof(MeasureTool)], new LoggerResult());

        Assert.Equal([typeof(SelectTool), typeof(MeasureTool)], tools.Select(x => x.GetType()));
    }

    [Fact]
    public async Task APluginToolGetsItsClicksWithTheRayUnderTheCursor()
    {
        var measure = new MeasureTool();
        using var tools = new EditorViewToolSet([new SelectTool(), measure], [], Keys.Space, new LoggerResult());
        tools.Initialize(null!);

        // M activates the measure tool, which keeps the camera but not the selection
        await tools.Update(new EditorViewInputFrame { PressedKeys = new HashSet<Keys> { Keys.M } });
        Assert.Same(measure, tools.ActiveTool);
        Assert.True(tools.IsDefaultInputOn(EditorViewDefaultInputs.CameraMiddleDrag | EditorViewDefaultInputs.CameraRightDrag));
        Assert.False(tools.IsDefaultInputOn(EditorViewDefaultInputs.Selection));

        await tools.Update(new EditorViewInputFrame { PressedButtons = new HashSet<MouseButton> { MouseButton.Left }, DownButtons = new HashSet<MouseButton> { MouseButton.Left } });
        var click = Assert.Single(await tools.Update(new EditorViewInputFrame { ReleasedButtons = new HashSet<MouseButton> { MouseButton.Left } }));
        Assert.False(click.ToSelection);

        // Two clicks on the ground, 3 units apart
        tools.Click(new EditorViewClick { Button = MouseButton.Left, Position = new Vector2(0.4f, 0.5f), Ray = new Ray(new Vector3(1, 5, 2), -Vector3.UnitY) });
        tools.Click(new EditorViewClick { Button = MouseButton.Left, Position = new Vector2(0.6f, 0.5f), Ray = new Ray(new Vector3(4, 5, 2), Vector3.Normalize(new Vector3(0, -1, 0))) });
        Assert.Equal(3, measure.Distance, 3);
    }

    [Fact]
    public void TheRayOfAClickGoesFromTheCameraThroughTheCursor()
    {
        var camera = new CameraComponent { UseCustomAspectRatio = true, AspectRatio = 2, VerticalFieldOfView = 90 };
        // A camera at (0, 0, 10), looking at the origin
        var cameraWorld = Matrix.Translation(0, 0, 10);

        var center = EditorGameHelper.CalculateRayFromMousePosition(camera, new Vector2(0.5f), cameraWorld);
        var topRight = EditorGameHelper.CalculateRayFromMousePosition(camera, new Vector2(1, 0), cameraWorld);

        Assert.Equal(new Vector3(0, 0, 10), center.Position);
        AssertNearEqual(-Vector3.UnitZ, center.Direction);
        // With a vertical field of view of 90 degrees and an aspect ratio of 2, the top right corner is at (2, 1) one unit away
        AssertNearEqual(Vector3.Normalize(new Vector3(2, 1, -1)), topRight.Direction);
    }

    private static void AssertNearEqual(Vector3 expected, Vector3 actual)
    {
        var epsilon = new Vector3(1e-5f);
        Assert.True(Vector3.NearEqual(in expected, in actual, in epsilon), $"Expected {expected}, got {actual}.");
    }
}

/// <summary>
/// The plugin of the measure tool and of the other tools of these tests, which its assembly declares.
/// </summary>
internal sealed class MeasurePlugin : AssetsPlugin
{
}

/// <summary>
/// A plugin that registers a tool through its context, without the attribute.
/// </summary>
internal sealed class ToolRegisteringPlugin(Type toolType) : AssetsPlugin
{
    public override void Initialize(IEditorPluginContext context)
    {
        base.Initialize(context);
        context.EditorViewToolTypes.Register(toolType);
    }
}

/// <summary>
/// A tool without <see cref="EditorViewToolAttribute"/>: only a plugin that registers it brings it.
/// </summary>
internal sealed class UndeclaredTool : IEditorViewTool
{
    public string Name => "Undeclared";

    public string? IconPath => null;

    public Keys Shortcut => Keys.None;

    public EditorViewDefaultInputs DefaultInputs => EditorViewDefaultInputs.All;

    public void Initialize(IEditorViewToolContext context)
    {
    }
}

/// <summary>
/// A plugin tool: measures the distance between the last two points clicked on the ground, and keeps the camera drags.
/// </summary>
[EditorViewTool]
internal sealed class MeasureTool : IEditorViewTool
{
    private Vector3? lastPoint;

    public string Name => "Measure";

    public string? IconPath => null;

    public Keys Shortcut => Keys.M;

    public EditorViewDefaultInputs DefaultInputs => EditorViewDefaultInputs.CameraMiddleDrag | EditorViewDefaultInputs.CameraRightDrag;

    public float Distance { get; private set; }

    public void Initialize(IEditorViewToolContext context)
    {
    }

    public void OnClick(EditorViewClick click)
    {
        var ray = click.Ray;
        var ground = new Plane(Vector3.UnitY, 0);
        if (click.Button != MouseButton.Left || !ray.Intersects(in ground, out Vector3 point))
            return;

        if (lastPoint is { } previous)
            Distance = Vector3.Distance(previous, point);
        lastPoint = point;
    }
}

/// <summary>
/// A tool whose constructor throws: the view logs it and leaves it out.
/// </summary>
[EditorViewTool]
internal sealed class ToolThatCannotBeCreated : IEditorViewTool
{
    public ToolThatCannotBeCreated() => throw new InvalidOperationException("No brush.");

    public string Name => "Broken";

    public string? IconPath => null;

    public Keys Shortcut => Keys.None;

    public EditorViewDefaultInputs DefaultInputs => EditorViewDefaultInputs.All;

    public void Initialize(IEditorViewToolContext context)
    {
    }
}

[EditorViewTool]
internal abstract class AbstractTool : IEditorViewTool
{
    public string Name => "Abstract";

    public string? IconPath => null;

    public Keys Shortcut => Keys.None;

    public EditorViewDefaultInputs DefaultInputs => EditorViewDefaultInputs.All;

    public void Initialize(IEditorViewToolContext context)
    {
    }
}

[EditorViewTool]
internal sealed class ToolWithoutParameterlessConstructor(int size) : IEditorViewTool
{
    public string Name => $"Tool {size}";

    public string? IconPath => null;

    public Keys Shortcut => Keys.None;

    public EditorViewDefaultInputs DefaultInputs => EditorViewDefaultInputs.All;

    public void Initialize(IEditorViewToolContext context)
    {
    }
}
