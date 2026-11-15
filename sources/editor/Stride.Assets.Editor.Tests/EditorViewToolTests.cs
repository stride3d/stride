// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Editor.EditorGame.Tools;
using Stride.Assets.Editor.Tests.Helpers;
using Stride.Core;
using Stride.Core.Assets.Editor.Editors;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Diagnostics;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Input;
using Stride.Assets.Editor.AssetEditors.EntityHierarchyEditor.Tools;
using Stride.Assets.Editor.AssetEditors.GameEditor.Game;

namespace Stride.Assets.Editor.Tests;

/// <summary>
/// The tools of the views of the scene and prefab editors: the editor's own, the ones a plugin declares with
/// <see cref="EditorViewToolAttribute"/> (as the measure tool of these tests) or registers with
/// <see cref="ScenePluginContextExtensions.SceneViewTools"/>, how the open views follow them, and how a failing one is turned off.
/// </summary>
public sealed class EditorViewToolTests
{
    [Fact]
    public void TheEditorDeclaresTheSelectionAndTransformationTools()
    {
        var tools = PluginRegistrations.Of(new StrideEditorPlugin(), typeof(IAssetEditorView)).GetRegistry<SceneViewToolDescriptor>();

        Assert.Equal([typeof(SelectTool), typeof(TranslateTool), typeof(RotateTool), typeof(ScaleTool)], tools.OrderBy(x => x.Order).Select(x => x.ToolType));
        Assert.Equal([0, 10, 20, 30], tools.OrderBy(x => x.Order).Select(x => x.Order));
    }

    [Fact]
    public async Task TheToolsOfAPluginComeAndGoWithThePlugin()
    {
        await using var fixture = await AssetsSessionFixture.OpenAsync(initializeEditor: false);
        var plugin = new MeasurePlugin();
        var logger = new LoggerResult();
        var host = new TestPluginService(plugin);
        host.RegisterSession(fixture.Session, logger);
        try
        {
            // The plugin context registers the tools that the plugin's assembly declares
            var tools = ToolTypes(host);
            Assert.Contains(typeof(MeasureTool), tools);
            Assert.Contains(typeof(ToolThatCannotBeCreated), tools);
            Assert.Same(plugin, host.SceneViewTools.GetOwner(host.SceneViewTools.Items.First(x => x.ToolType == typeof(MeasureTool))));
            // A tool must be a class with a public parameterless constructor: each view creates its own. The other tools
            // are logged and left out, and the plugin joins
            Assert.DoesNotContain(typeof(AbstractTool), tools);
            Assert.DoesNotContain(typeof(ToolWithoutParameterlessConstructor), tools);
            Assert.Contains(plugin, host.SessionPlugins);
            AssertRefused(logger, typeof(AbstractTool), plugin);
            AssertRefused(logger, typeof(ToolWithoutParameterlessConstructor), plugin);
            // The editor's own tools come from its plugin, in the session's host
            Assert.Contains(typeof(TranslateTool), ToolTypes(fixture.Plugins));
            Assert.DoesNotContain(typeof(MeasureTool), ToolTypes(fixture.Plugins));
        }
        finally
        {
            host.Dispose();
        }

        Assert.Empty(host.SceneViewTools.Items);
        Assert.Contains(typeof(TranslateTool), ToolTypes(fixture.Plugins));
    }

    [Fact]
    public async Task APluginCanRegisterAToolItself()
    {
        await using var fixture = await AssetsSessionFixture.OpenAsync(initializeEditor: false);
        var host = new TestPluginService(new ToolRegisteringPlugin(SceneViewToolDescriptor.For<UndeclaredTool>(order: 5)), new ToolRegisteringPlugin(SceneViewToolDescriptor.For<UndeclaredTool>()));
        host.RegisterSession(fixture.Session, new LoggerResult());
        try
        {
            // Two plugins register the same tool: the views create it once, at the place the first one gives
            Assert.Equal(2, host.SceneViewTools.Items.Count(x => x.ToolType == typeof(UndeclaredTool)));
            var tools = EditorGameToolService.CreateTools(EditorGameToolService.GetToolDescriptors(host.SceneViewTools.Items), new LoggerResult());
            Assert.Single(tools, x => x is UndeclaredTool);
            Assert.Equal([typeof(SelectTool), typeof(UndeclaredTool), typeof(TranslateTool)], tools.Take(3).Select(x => x.GetType()));
        }
        finally
        {
            host.Dispose();
        }
        Assert.Empty(host.SceneViewTools.Items);
    }

    [Fact]
    public async Task AToolTheViewsCannotUseIsLeftOutAndThePluginJoins()
    {
        await using var fixture = await AssetsSessionFixture.OpenAsync(initializeEditor: false);
        var plugin = new ToolRegisteringPlugin(SceneViewToolDescriptor.FromType(typeof(AbstractTool)), SceneViewToolDescriptor.For<UndeclaredTool>(), SceneViewToolDescriptor.FromType(typeof(MeasurePlugin)));
        var logger = new LoggerResult();
        var host = new TestPluginService(plugin);
        host.RegisterSession(fixture.Session, logger);
        try
        {
            Assert.Contains(plugin, host.SessionPlugins);
            Assert.Contains(typeof(UndeclaredTool), ToolTypes(host));
            Assert.DoesNotContain(typeof(AbstractTool), ToolTypes(host));
            Assert.DoesNotContain(typeof(MeasurePlugin), ToolTypes(host));
            AssertRefused(logger, typeof(AbstractTool), plugin);
            AssertRefused(logger, typeof(MeasurePlugin), plugin);
        }
        finally
        {
            host.Dispose();
        }
    }

    [Fact]
    public async Task TheOldToolListStillRegistersTools()
    {
        await using var fixture = await AssetsSessionFixture.OpenAsync(initializeEditor: false);
        var plugin = new ObsoleteToolRegisteringPlugin(typeof(UndeclaredTool), typeof(AbstractTool));
        var logger = new LoggerResult();
        var host = new TestPluginService(plugin);
        host.RegisterSession(fixture.Session, logger);
        try
        {
            Assert.Contains(plugin, host.SessionPlugins);
            var descriptor = Assert.Single(host.SceneViewTools.Items, x => x.ToolType == typeof(UndeclaredTool));
            Assert.Equal(SceneViewToolDescriptor.DefaultOrder, descriptor.Order);
            Assert.Same(plugin, host.SceneViewTools.GetOwner(descriptor));
            Assert.DoesNotContain(typeof(AbstractTool), ToolTypes(host));
            AssertRefused(logger, typeof(AbstractTool), plugin);
        }
        finally
        {
            host.Dispose();
        }
        Assert.Empty(host.SceneViewTools.Items);
    }

    [Fact]
    public async Task APluginThatBypassesTheTypedRegistryWithABadToolDoesNotJoin()
    {
        await using var fixture = await AssetsSessionFixture.OpenAsync(initializeEditor: false);
        var plugin = new RawToolRegisteringPlugin(SceneViewToolDescriptor.FromType(typeof(AbstractTool)));
        var host = new TestPluginService(plugin);
        host.RegisterSession(fixture.Session, new LoggerResult());
        try
        {
            // The registry of the host refuses the tool: the plugin's registrations are undone
            Assert.DoesNotContain(plugin, host.SessionPlugins);
            Assert.Empty(host.SceneViewTools.Items);
        }
        finally
        {
            host.Dispose();
        }
    }

    [Fact]
    public async Task AnOpenViewFollowsTheToolsOfThePluginsThatJoinAndLeave()
    {
        await using var fixture = await AssetsSessionFixture.OpenAsync(initializeEditor: false);
        var plugin = new ToolRegisteringPlugin(SceneViewToolDescriptor.For<UndeclaredTool>(order: 40));
        var logger = new LoggerResult();
        var host = new TestPluginService();
        host.RegisterSession(fixture.Session, logger);
        IReadOnlyDictionary<Type, object?> owners = new Dictionary<Type, object?>();
        var failedTypes = new HashSet<Type>();
        var initial = EditorGameToolService.CreateTools(EditorGameToolService.GetToolDescriptors(EditorGameToolService.ReadRegistry(host.SceneViewTools, out owners)), logger);
        using var view = new EditorViewToolSet(initial, initial.OfType<TransformationTool>().ToList(), Keys.Space, logger, host, tool => owners.GetValueOrDefault(tool.GetType()));
        view.Initialize(null!);
        var changes = 0;
        host.SceneViewTools.CollectionChanged += (_, _) => changes++;
        try
        {
            Assert.DoesNotContain(view.Tools, x => x is UndeclaredTool);

            // The plugin joins: the view creates its tool
            host.PluginsDiscovered([plugin]);
            Assert.True(changes > 0);
            EditorGameToolService.FollowRegistry(host.SceneViewTools, view, x => owners = x, logger, failedTypes);
            var tool = Assert.Single(view.Tools.OfType<UndeclaredTool>());
            Assert.Same(plugin, owners[typeof(UndeclaredTool)]);
            Assert.True(view.Activate(tool));
            Assert.Same(tool, view.ActiveTool);

            // The plugin leaves: the view goes back to its default tool, and the tool is disposed
            host.PluginsRemoved([plugin]);
            EditorGameToolService.FollowRegistry(host.SceneViewTools, view, x => owners = x, logger, failedTypes);
            Assert.DoesNotContain(view.Tools, x => x is UndeclaredTool);
            Assert.IsType<SelectTool>(view.ActiveTool);
            Assert.True(tool.IsDisposed);

            // It joins again: a new instance
            host.PluginsDiscovered([plugin]);
            EditorGameToolService.FollowRegistry(host.SceneViewTools, view, x => owners = x, logger, failedTypes);
            Assert.NotSame(tool, Assert.Single(view.Tools.OfType<UndeclaredTool>()));
            // The view of this test has no game: only the plugin's tool is checked
            Assert.DoesNotContain(logger.Messages, x => x.Type >= LogMessageType.Error && x.Text.Contains(nameof(UndeclaredTool)));
        }
        finally
        {
            host.Dispose();
        }
    }

    [Fact]
    public async Task AFailingPluginToolIsRecordedForItsPluginAndTurnedOffInEveryView()
    {
        await using var fixture = await AssetsSessionFixture.OpenAsync(initializeEditor: false);
        var plugin = new ToolRegisteringPlugin(SceneViewToolDescriptor.For<FailingTool>());
        var logger = new LoggerResult();
        var host = new TestPluginService(plugin);
        host.RegisterSession(fixture.Session, logger);
        try
        {
            var first = CreateView(host, logger);
            var second = CreateView(host, logger);
            using (first)
            using (second)
            {
                var tool = Assert.Single(first.Tools.OfType<FailingTool>());
                Assert.True(first.Activate(tool));

                await first.Update(new EditorViewInputFrame());

                Assert.True(first.IsFailed(tool));
                Assert.IsType<SelectTool>(first.ActiveTool);
                var status = host.GetPluginStatus(plugin);
                Assert.NotNull(status);
                var error = Assert.Single(status.Errors);
                Assert.Equal(ScenePluginContributions.SceneViewTool(typeof(FailingTool)), error.Contribution);
                Assert.Contains(error.Contribution, status.TurnedOffContributions);
                Assert.Contains(logger.Messages, x => x.Type == LogMessageType.Error && x.Text.Contains(plugin.GetType().FullName!));
                // The tool of the other view is off too
                var other = Assert.Single(second.Tools.OfType<FailingTool>());
                Assert.False(second.Activate(other));
                Assert.True(second.IsFailed(other));
            }
        }
        finally
        {
            host.Dispose();
        }

        static EditorViewToolSet CreateView(TestPluginService host, ILogger logger)
        {
            var tools = EditorGameToolService.CreateTools(EditorGameToolService.GetToolDescriptors(EditorGameToolService.ReadRegistry(host.SceneViewTools, out var owners)), logger);
            var view = new EditorViewToolSet(tools, [], Keys.Space, logger, host, tool => owners.GetValueOrDefault(tool.GetType()));
            view.Initialize(null!);
            return view;
        }
    }

    [Fact]
    public void AViewLeavesOutATypeThatIsNotATool()
    {
        var logger = new LoggerResult();

        var tools = EditorGameToolService.CreateTools([new SceneViewToolDescriptor { ToolType = typeof(MeasurePlugin) }, SceneViewToolDescriptor.For<TranslateTool>()], logger);

        Assert.Equal([typeof(SelectTool), typeof(TranslateTool)], tools.Select(x => x.GetType()));
        var error = Assert.Single(logger.Messages, x => x.Type == LogMessageType.Error);
        Assert.Contains(typeof(MeasurePlugin).FullName!, error.Text);
    }

    [Fact]
    public void EachViewCreatesTheToolsInToolbarOrder()
    {
        var logger = new LoggerResult();

        var tools = EditorGameToolService.CreateTools(new[] { typeof(MeasureTool), typeof(ScaleTool), typeof(ToolThatCannotBeCreated), typeof(TranslateTool), typeof(RotateTool), typeof(SelectTool) }.Select(SceneViewToolDescriptor.FromType), logger);

        Assert.Equal([typeof(SelectTool), typeof(TranslateTool), typeof(RotateTool), typeof(ScaleTool), typeof(MeasureTool)], tools.Select(x => x.GetType()));
        var error = Assert.Single(logger.Messages, x => x.Type == LogMessageType.Error);
        Assert.Contains(typeof(ToolThatCannotBeCreated).FullName!, error.Text);
        Assert.Contains(typeof(ToolThatCannotBeCreated).Assembly.GetName().Name!, error.Text);
    }

    [Fact]
    public void AViewHasTheEditorToolsWhateverPluginsAreRegistered()
    {
        // The WPF editor doesn't register the default plugin of the cross-platform one, which declares the editor tools
        var tools = EditorGameToolService.CreateTools(EditorGameToolService.GetToolDescriptors([SceneViewToolDescriptor.For<MeasureTool>()]), new LoggerResult());
        Assert.Equal([typeof(SelectTool), typeof(TranslateTool), typeof(RotateTool), typeof(ScaleTool), typeof(MeasureTool)], tools.Select(x => x.GetType()));

        // A tool declared by a plugin too is created once, at its own place
        tools = EditorGameToolService.CreateTools(EditorGameToolService.GetToolDescriptors([SceneViewToolDescriptor.For<TranslateTool>(order: 100)]), new LoggerResult());
        Assert.Single(tools, x => x is TranslateTool);
        Assert.IsType<TranslateTool>(tools[1]);
    }

    [Fact]
    public void AViewWithoutTheEditorToolsStillHasTheSelectTool()
    {
        var tools = EditorGameToolService.CreateTools([SceneViewToolDescriptor.For<MeasureTool>()], new LoggerResult());

        Assert.Equal([typeof(SelectTool), typeof(MeasureTool)], tools.Select(x => x.GetType()));
    }

    [Fact]
    public void TheRefusalsOfADescriptorSayWhatIsWrong()
    {
        Assert.Null(SceneViewToolDescriptor.GetRefusal(SceneViewToolDescriptor.For<MeasureTool>()));
        Assert.NotNull(SceneViewToolDescriptor.GetRefusal(null));
        Assert.Contains(nameof(IEditorViewTool), SceneViewToolDescriptor.GetRefusal(new SceneViewToolDescriptor { ToolType = typeof(MeasurePlugin) }));
        Assert.Contains("parameterless", SceneViewToolDescriptor.GetRefusal(SceneViewToolDescriptor.FromType(typeof(ToolWithoutParameterlessConstructor))));
        Assert.Contains("parameterless", SceneViewToolDescriptor.GetRefusal(SceneViewToolDescriptor.FromType(typeof(AbstractTool))));
        Assert.Throws<ArgumentException>(() => SceneViewToolDescriptor.Validate(SceneViewToolDescriptor.FromType(typeof(AbstractTool))));
        // The attribute gives the order
        Assert.Equal(10, SceneViewToolDescriptor.FromType(typeof(TranslateTool)).Order);
        Assert.Equal(SceneViewToolDescriptor.DefaultOrder, SceneViewToolDescriptor.FromType(typeof(UndeclaredTool)).Order);
    }

    private static List<Type> ToolTypes(SceneEditorPluginHost host) => host.SceneViewTools.Items.Select(x => x.ToolType).ToList();

    private static void AssertRefused(LoggerResult logger, Type toolType, AssetsPlugin plugin)
    {
        Assert.Contains(logger.Messages, x => x.Type == LogMessageType.Error && x.Text.Contains($"[{toolType.FullName}]") && x.Text.Contains($"[{plugin.GetType().FullName}]") && x.Text.Contains("left out"));
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
/// A plugin that registers tools through its context, without the attribute.
/// </summary>
internal sealed class ToolRegisteringPlugin(params SceneViewToolDescriptor[] tools) : AssetsPlugin
{
    public override void Initialize(IEditorPluginContext context)
    {
        base.Initialize(context);
        foreach (var tool in tools)
            context.SceneViewTools().Register(tool);
    }
}

/// <summary>
/// A plugin that registers tools with the old list of types.
/// </summary>
internal sealed class ObsoleteToolRegisteringPlugin(params Type[] toolTypes) : AssetsPlugin
{
    public override void Initialize(IEditorPluginContext context)
    {
        base.Initialize(context);
#pragma warning disable STRPLUGIN006 // The old API, which the editor still supports
        foreach (var type in toolTypes)
            context.EditorViewToolTypes.Register(type);
#pragma warning restore STRPLUGIN006
    }
}

/// <summary>
/// A plugin that registers tools in the raw registry, without the checks of the typed method.
/// </summary>
internal sealed class RawToolRegisteringPlugin(params SceneViewToolDescriptor[] tools) : AssetsPlugin
{
    public override void Initialize(IEditorPluginContext context)
    {
        base.Initialize(context);
        foreach (var tool in tools)
            context.GetRegistry<SceneViewToolDescriptor>().Register(tool);
    }
}

/// <summary>
/// A tool whose update throws.
/// </summary>
internal sealed class FailingTool : IEditorViewTool
{
    public string Name => "Failing";

    public string? IconPath => null;

    public Keys Shortcut => Keys.None;

    public EditorViewDefaultInputs DefaultInputs => EditorViewDefaultInputs.All;

    public void Initialize(IEditorViewToolContext context)
    {
    }

    public Task Update() => throw new InvalidOperationException("No brush.");
}

/// <summary>
/// A tool without <see cref="EditorViewToolAttribute"/>: only a plugin that registers it brings it.
/// </summary>
internal sealed class UndeclaredTool : IEditorViewTool
{
    public string Name => "Undeclared";

    public bool IsDisposed { get; private set; }

    public void Dispose() => IsDisposed = true;

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
