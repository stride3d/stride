// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Editor.EditorGame.Tools;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Tools;
using Stride.Assets.Presentation.AssetEditors.GameEditor.Game;
using Stride.Core;
using Stride.Core.Diagnostics;
using Stride.Core.Mathematics;
using Stride.Core.Reflection;
using Stride.Engine;
using Stride.Input;

namespace Stride.Assets.Editor.Tests;

/// <summary>
/// The tools of the views of the scene and prefab editors: the editor's own, and the ones a plugin declares with
/// <see cref="EditorViewToolAttribute"/>, as the measure tool of these tests.
/// </summary>
public sealed class EditorViewToolTests
{
    [Fact]
    public void TheEditorDeclaresTheSelectionAndTransformationTools()
    {
        new StrideEditorPlugin().InitializePlugin(new LoggerResult());

        Assert.Contains(typeof(SelectTool), StrideEditorPlugin.EditorViewToolTypes);
        Assert.Contains(typeof(TranslateTool), StrideEditorPlugin.EditorViewToolTypes);
        Assert.Contains(typeof(RotateTool), StrideEditorPlugin.EditorViewToolTypes);
        Assert.Contains(typeof(ScaleTool), StrideEditorPlugin.EditorViewToolTypes);
        Assert.DoesNotContain(typeof(TransformationTool), StrideEditorPlugin.EditorViewToolTypes);
    }

    [Fact]
    public void ThePluginToolsComeAndGoWithTheirAssembly()
    {
        new StrideEditorPlugin().InitializePlugin(new LoggerResult());
        var assembly = typeof(MeasureTool).Assembly;
        Assert.DoesNotContain(typeof(MeasureTool), StrideEditorPlugin.EditorViewToolTypes);

        AssemblyRegistry.Register(assembly, AssemblyCommonCategories.Assets);
        try
        {
            Assert.Contains(typeof(MeasureTool), StrideEditorPlugin.EditorViewToolTypes);
            Assert.Contains(typeof(ToolThatCannotBeCreated), StrideEditorPlugin.EditorViewToolTypes);
            // A tool must be a class with a parameterless constructor: each view creates its own
            Assert.DoesNotContain(typeof(AbstractTool), StrideEditorPlugin.EditorViewToolTypes);
            Assert.DoesNotContain(typeof(ToolWithoutParameterlessConstructor), StrideEditorPlugin.EditorViewToolTypes);
        }
        finally
        {
            AssemblyRegistry.Unregister(assembly);
        }

        Assert.DoesNotContain(typeof(MeasureTool), StrideEditorPlugin.EditorViewToolTypes);
        Assert.Contains(typeof(TranslateTool), StrideEditorPlugin.EditorViewToolTypes);
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
