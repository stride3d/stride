// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Stride.Assets.Editor.AssetEditors.EntityHierarchyEditor.Tools;
using Stride.Assets.Editor.AssetEditors.GameEditor;
using Stride.Assets.Editor.Commands;
using Stride.Assets.Editor.EditorGame.Tools;
using Stride.Assets.Editor.ViewModels;
using Stride.Core.Assets.Editor.Avalonia.Commands;
using Stride.Core.Assets.Editor.Commands;
using Stride.Core.Mathematics;
using Stride.GameStudio.Avalonia.Commands;
using Stride.GameStudio.Avalonia.Tests.Helpers;
using Stride.Input;

namespace Stride.GameStudio.Avalonia.Tests;

/// <summary>
/// The context menu of the scene view, which a right click without camera drag opens (the game side decides which clicks
/// open it: <c>EditorViewToolSetTests</c>): the entries of the active tool, then those of the editor, then the commands of the plugins at
/// <see cref="SceneLocations.SceneViewContext"/>, with the click and the entity under the cursor as their selection.
/// </summary>
public sealed class SceneViewContextMenuTests
{
    [AvaloniaFact]
    public async Task ARightClickOnAnEntityShowsTheToolEntriesThenThePluginCommandsWithTheEntity()
    {
        await using var scene = await SceneTreeDropTests.OpenSceneAsync();
        var runs = new List<CommandContext>();
        var plugin = new CommandsTestPlugin(context => context.Commands.Register(new CommandDescriptor
        {
            Id = "Tests.SceneView",
            Title = () => "Place here",
            ExecuteAsync = context =>
            {
                runs.Add(context);
                return Task.CompletedTask;
            },
            Placements = [new CommandPlacement(SceneLocations.SceneViewContext)],
        }));
        scene.Fixture.Plugins.PluginsDiscovered([plugin]);
        var tools = new FakeToolService();
        var toolbar = AttachToolbar(scene, tools);
        try
        {
            var painted = 0;
            var boxViewModel = scene.Entity("Box");
            var click = Click();

            tools.RaiseContextMenu(new EditorViewContextMenuRequest
            {
                Click = click,
                EntityId = boxViewModel.Id,
                Tool = tools.Tools[0],
                ToolMenu = new EditorViewContextMenu { Entries = [new EditorViewMenuEntry { Title = "Paint here", IconPath = "M0,0 L8,8", Execute = () => painted++ }] },
            });
            await WindowTestHelper.PumpAsync();

            var menu = scene.View.SceneViewContextMenu;
            Assert.NotNull(menu);
            Assert.True(menu.IsOpen);
            // The tool's entries first, then the entries of the editor, then the commands of the
            // plugins in their group
            var items = Describe(menu);
            Assert.Equal(["Paint here", "-", "Create", "Empty entity"], items.Take(4));
            Assert.Equal(["Rename", "-", "Place here"], items.TakeLast(3));

            var command = Assert.IsType<DescriptorCommand>(menu.Items.OfType<MenuItem>().Single(x => Equals(x.Tag, "Tests.SceneView")).Command);
            Assert.True(await command.ExecuteAsync());
            var run = Assert.Single(runs);
            Assert.Equal(SceneLocations.SceneViewContext, run.Location);
            Assert.Equal<object>([click, boxViewModel], run.Selection);

            // The tool's entry runs on the game thread (here, at once)
            var entry = menu.Items.OfType<MenuItem>().First();
            entry.Command!.Execute(null);
            await WindowTestHelper.WaitUntilAsync(() => painted == 1, "the entry of the tool");
            Assert.NotNull(entry.Icon);

            menu.Close();
            await WindowTestHelper.PumpAsync();
            Assert.Null(scene.View.SceneViewContextMenu);

            // A click on nothing: the click alone
            tools.RaiseContextMenu(new EditorViewContextMenuRequest { Click = click, Tool = tools.Tools[0] });
            await WindowTestHelper.PumpAsync();
            menu = scene.View.SceneViewContextMenu;
            Assert.NotNull(menu);
            items = Describe(menu);
            Assert.Equal("Create", items[0]);
            Assert.DoesNotContain("Paint here", items);
            Assert.Equal("Place here", items[^1]);
            command = Assert.IsType<DescriptorCommand>(menu.Items.OfType<MenuItem>().Single(x => Equals(x.Tag, "Tests.SceneView")).Command);
            Assert.True(await command.ExecuteAsync());
            Assert.Equal<object>([click], runs[^1].Selection);
            menu.Close();
        }
        finally
        {
            scene.View.SceneViewContextMenu?.Close();
            DetachToolbar(scene, toolbar);
            scene.Fixture.Plugins.PluginsRemoved([plugin]);
            await WindowTestHelper.PumpAsync();
        }
    }

    [AvaloniaFact]
    public async Task AToolCanReplaceThePluginCommandsOrKeepTheMenuClosed()
    {
        await using var scene = await SceneTreeDropTests.OpenSceneAsync();
        var plugin = new CommandsTestPlugin(context => context.Commands.Register(new CommandDescriptor
        {
            Id = "Tests.SceneView",
            Title = () => "Place here",
            ExecuteAsync = _ => Task.CompletedTask,
            Placements = [new CommandPlacement(SceneLocations.SceneViewContext)],
        }));
        scene.Fixture.Plugins.PluginsDiscovered([plugin]);
        var tools = new FakeToolService();
        var toolbar = AttachToolbar(scene, tools);
        try
        {
            // The entries of the tool replace the commands
            tools.RaiseContextMenu(new EditorViewContextMenuRequest
            {
                Click = Click(),
                Tool = tools.Tools[0],
                ToolMenu = new EditorViewContextMenu { Entries = [new EditorViewMenuEntry { Title = "Only this", Execute = () => { } }], ReplacesOthers = true },
            });
            await WindowTestHelper.PumpAsync();
            var menu = scene.View.SceneViewContextMenu;
            Assert.NotNull(menu);
            Assert.Equal(["Only this"], Describe(menu));
            menu.Close();
            await WindowTestHelper.PumpAsync();

            // A tool that replaces them with nothing keeps the menu closed (the game side asks no menu then; the view agrees)
            tools.RaiseContextMenu(new EditorViewContextMenuRequest { Click = Click(), Tool = tools.Tools[0], ToolMenu = new EditorViewContextMenu { ReplacesOthers = true } });
            await WindowTestHelper.PumpAsync();
            Assert.Null(scene.View.SceneViewContextMenu);
        }
        finally
        {
            scene.View.SceneViewContextMenu?.Close();
            DetachToolbar(scene, toolbar);
            scene.Fixture.Plugins.PluginsRemoved([plugin]);
            await WindowTestHelper.PumpAsync();
        }
    }

    [AvaloniaFact]
    public async Task WithoutToolEntriesOrPluginCommandsTheMenuShowsTheEntriesOfTheEditor()
    {
        await using var scene = await SceneTreeDropTests.OpenSceneAsync();
        var tools = new FakeToolService();
        var toolbar = AttachToolbar(scene, tools);
        try
        {
            // No plugin command at the place, no tool entry: the entries of the editor, the creation at the cursor and
            // the actions on the selection
            scene.Editor.SelectedContent.Clear();
            scene.Editor.SelectedContent.Add(scene.Entity("Box"));
            tools.RaiseContextMenu(new EditorViewContextMenuRequest { Click = Click(), Tool = tools.Tools[0] });
            await WindowTestHelper.PumpAsync();

            var menu = scene.View.SceneViewContextMenu;
            Assert.NotNull(menu);
            Assert.True(menu.IsOpen);
            var items = Describe(menu);
            Assert.Equal(["Create", "Empty entity"], items.Take(2));
            Assert.Equal(["Actions", "Open prefab in editor", "Select prefab in asset view", "Break link to prefab", "Create prefab from selection", "-", "Duplicate", "Cut", "Copy", "Delete", "-", "Rename"],
                items.SkipWhile(x => x != "Actions"));
            // The entity is created at the cursor, by the editor's command bound by its identifier
            var createEntity = Assert.IsType<CommandReference>(menu.Items.OfType<MenuItem>().Single(x => Equals(x.Header, "Empty entity")).Command);
            Assert.Equal(SceneEditorCommands.NewEntityAtCursorId, createEntity.Id);
            Assert.True(createEntity.CanExecute(null));
            // Cut, Copy, Delete and Rename are the shared Edit commands, which act on the selected entities through the
            // Edit bindings of the view
            var edits = menu.Items.OfType<MenuItem>().Select(x => x.Command).OfType<CommandReference>().Select(x => x.Id).Where(x => x.StartsWith("Stride.Edit.", StringComparison.Ordinal));
            Assert.Equal([GameStudioCommands.CutId, GameStudioCommands.CopyId, GameStudioCommands.DeleteId, GameStudioCommands.RenameId], edits);
            var rename = menu.Items.OfType<MenuItem>().Single(x => x.Command is CommandReference { Id: GameStudioCommands.RenameId });
            Assert.True(scene.Editor.RenameCommand.CanExecute(null));
            Assert.True(rename.IsEffectivelyEnabled);
            var copy = menu.Items.OfType<MenuItem>().Single(x => x.Command is CommandReference { Id: GameStudioCommands.CopyId });
            Assert.Equal(scene.Editor.CopyCommand.CanExecute(null), copy.IsEffectivelyEnabled);
            Assert.DoesNotContain(menu.Items.OfType<Control>(), CommandSlot.IsSlotItem);
            menu.Close();
        }
        finally
        {
            scene.View.SceneViewContextMenu?.Close();
            DetachToolbar(scene, toolbar);
        }
    }

    [AvaloniaFact]
    public async Task TheToolbarFollowsTheToolsOfTheView()
    {
        await using var scene = await SceneTreeDropTests.OpenSceneAsync();
        var tools = new FakeToolService();
        var toolbar = AttachToolbar(scene, tools);
        try
        {
            var translate = toolbar.Tools[1];
            var measure = new MeasureTool();

            // A plugin's tool joins the view
            tools.SetTools([.. tools.Tools, measure]);
            await WindowTestHelper.WaitUntilAsync(() => toolbar.Tools.Count == 5, "the new tool");
            Assert.Same(measure, toolbar.Tools[4].Tool);
            // The items of the tools that stay are kept
            Assert.Same(translate, toolbar.Tools[1]);

            // It leaves
            tools.SetTools(tools.Tools.Take(4).ToList());
            await WindowTestHelper.WaitUntilAsync(() => toolbar.Tools.Count == 4, "the tool to leave");
            Assert.DoesNotContain(toolbar.Tools, x => x.Tool == measure);
        }
        finally
        {
            DetachToolbar(scene, toolbar);
        }
    }

    internal static EditorViewToolbarViewModel AttachToolbar(SceneTreeDropTests.SceneFixture scene, FakeToolService tools)
    {
        var editor = scene.Editor;
        // The headless editor has no game: the toolbar of its view gets a tool service without a game
        var toolbar = new EditorViewToolbarViewModel(editor.ServiceProvider, tools, null, null, findEntity: id => editor.FindPartViewModel(id) as Stride.Assets.Presentation.ViewModels.EntityViewModel);
        editor.ViewOptions = toolbar;
        return toolbar;
    }

    internal static void DetachToolbar(SceneTreeDropTests.SceneFixture scene, EditorViewToolbarViewModel toolbar)
    {
        scene.Editor.ViewOptions = null;
        toolbar.Destroy();
    }

    private static EditorViewClick Click() => new() { Button = MouseButton.Right, Position = new Vector2(0.5f), Ray = new Ray(new Vector3(0, 5, 10), -Vector3.UnitZ) };

    private static List<string> Describe(ContextMenu menu)
    {
        return menu.Items.OfType<Control>().Where(x => x.IsVisible).Select(x => x is MenuItem item ? item.Header?.ToString() ?? "" : "-").ToList();
    }

    /// <summary>
    /// The tool service of a view without a game: the calls to the game thread run at once.
    /// </summary>
    internal sealed class FakeToolService : IEditorGameToolViewModelService
    {
        public IReadOnlyList<IEditorViewTool> Tools { get; private set; } = [new SelectTool(), new TranslateTool(), new RotateTool(), new ScaleTool()];

        public event EventHandler? ToolsChanged;

        public event EventHandler<EditorViewContextMenuRequest>? ContextMenuRequested;

        public event EventHandler<IEditorViewTool>? ActiveToolChanged { add { } remove { } }

        public event EventHandler<IEditorViewTool>? ToolFailed { add { } remove { } }

        public void SetTools(IReadOnlyList<IEditorViewTool> tools)
        {
            Tools = tools;
            ToolsChanged?.Invoke(this, EventArgs.Empty);
        }

        public void RaiseContextMenu(EditorViewContextMenuRequest request) => ContextMenuRequested?.Invoke(this, request);

        public Task<(IEditorViewTool ActiveTool, IReadOnlyList<IEditorViewTool> FailedTools)> GetStateAsync()
            => Task.FromResult((Tools[0], (IReadOnlyList<IEditorViewTool>)[]));

        public Task<bool> RunMenuEntryAsync(IEditorViewTool tool, EditorViewMenuEntry entry)
        {
            entry.Execute();
            return Task.FromResult(true);
        }

        public Task<bool> ActivateAsync(IEditorViewTool tool) => Task.FromResult(true);

        public Task SetSpaceAsync(TransformationSpace space) => Task.CompletedTask;

        public Task SetGizmoSizeAsync(float size) => Task.CompletedTask;

        public Task SetSnapAsync(Transformation transformation, float value, bool isActive) => Task.CompletedTask;
    }

    /// <summary>
    /// A tool of a plugin, for the toolbar.
    /// </summary>
    private sealed class MeasureTool : IEditorViewTool
    {
        public string Name => "Measure";

        public string? IconPath => null;

        public Keys Shortcut => Keys.None;

        public EditorViewDefaultInputs DefaultInputs => EditorViewDefaultInputs.All;

        public void Initialize(IEditorViewToolContext context)
        {
        }
    }
}
