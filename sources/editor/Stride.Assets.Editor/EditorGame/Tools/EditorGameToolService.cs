// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Specialized;
using Stride.Assets.Editor.ViewModels;
using Stride.Core;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.BuildEngine;
using Stride.Core.Diagnostics;
using Stride.Core.Mathematics;
using Stride.Editor.EditorGame.Game;
using Stride.Editor.EditorGame.ViewModels;
using Stride.Engine;
using Stride.Input;
using Stride.Rendering;
using Stride.Rendering.Compositing;
using Stride.Assets.Editor.AssetEditors.EntityHierarchyEditor.Game;
using Stride.Assets.Editor.AssetEditors.EntityHierarchyEditor.Tools;
using Stride.Assets.Editor.AssetEditors.GameEditor;
using Stride.Assets.Editor.AssetEditors.GameEditor.Game;
using Stride.Assets.Editor.AssetEditors.Gizmos;
using Stride.Assets.Editor.SceneEditor;

namespace Stride.Assets.Editor.EditorGame.Tools;

/// <summary>
/// The tools of the view of a scene or prefab editor, in both user interfaces: select, translate, rotate, scale and the
/// tools of the plugins. It arbitrates the input of the view between the active tool, the selection and the camera
/// (<see cref="EditorViewToolSet"/>), and has the keys of the transformation gizmos of the WPF scene editor. The toolbar of
/// each user interface drives it through <see cref="IEditorGameToolViewModelService"/>.
/// </summary>
/// <remarks>
/// The tools of the plugins come from the registry that the plugin host declares (<see cref="SceneEditorPluginHost.SceneViewTools"/>):
/// the view follows it, adding and removing tools as the plugins join and leave, and runs them through the host's
/// contribution guard.
/// </remarks>
/// <remarks>
/// <list type="bullet">
/// <item>Q: select. W: translate. E: rotate. R: scale. Space: next transformation.</item>
/// <item>Shift while dragging a gizmo: snap. N: snap the selection to the grid.</item>
/// </list>
/// The keys are the scene editor settings (<see cref="SceneEditorSettings"/>). The snaps and the gizmo size come from the
/// settings of the scene, through the toolbar.
/// </remarks>
[EditorGameService(typeof(EntityHierarchyEditorController), Order = 210)]
public sealed class EditorGameToolService : EditorGameMouseServiceBase, IEditorGameToolService, IEditorGameToolViewModelService, IEditorGameViewDependent
{
    // The key bindings of the scene editor settings, read as they are used so that a change applies at once, as in WPF (the
    // switch key when the view opens)
    private static Keys SwitchGizmo => SceneEditorSettings.SwitchGizmo.GetValue();
    private static Keys SnapSelectionToGrid => SceneEditorSettings.SnapSelectionToGrid.GetValue();
    private static Keys DynamicSnap => SceneEditorSettings.ControlDynamicSnapSelectionToGrid.GetValue();

    private readonly EntityHierarchyEditorController controller;
    private readonly EntityHierarchyEditorViewModel editor;
    private readonly HashSet<Type> failedToolTypes = [];
    private EditorViewToolSet? toolSet;
    private EditorViewToolContext? context;
    private EntityHierarchyEditorGame? game;
    private ContributionRegistry<SceneViewToolDescriptor>? registry;
    private IReadOnlyDictionary<Type, object?> toolOwners = new Dictionary<Type, object?>();
    private Vector3 lastCameraPosition;

    /// <summary>
    /// Initializes a new instance of the <see cref="EditorGameToolService"/> class.
    /// </summary>
    public EditorGameToolService(EntityHierarchyEditorController controller, EntityHierarchyEditorViewModel editor)
    {
        this.controller = controller;
        this.editor = editor;
    }

    /// <inheritdoc/>
    public override bool IsControllingMouse { get; protected set; }

    /// <inheritdoc/>
    public override IEnumerable<Type> Dependencies
    {
        get
        {
            yield return typeof(EditorGameEntitySelectionService);
            yield return typeof(IEditorGameCameraService);
        }
    }

    /// <inheritdoc/>
    public IEditorViewTool ActiveTool => ToolSet.ActiveTool;


    /// <inheritdoc/>
    public IReadOnlyList<IEditorViewTool> Tools => toolSet?.Tools ?? [];

    /// <inheritdoc/>
    public event EventHandler<IEditorViewTool>? ActiveToolChanged;

    /// <inheritdoc/>
    public event EventHandler? ToolsChanged;

    /// <inheritdoc/>
    public event EventHandler<EditorViewContextMenuRequest>? ContextMenuRequested;

    /// <inheritdoc/>
    public event EventHandler<IEditorViewTool>? ToolFailed;

    private EditorViewToolSet ToolSet => toolSet ?? throw new InvalidOperationException("The service is not initialized.");

    private EditorViewToolContext Context => context ?? throw new InvalidOperationException("The service is not initialized.");

    private EntityHierarchyEditorGame Game => game ?? throw new InvalidOperationException("The service is not initialized.");

    private EditorGameEntitySelectionService Selection => Services.Get<EditorGameEntitySelectionService>();

    /// <inheritdoc/>
    public bool IsDefaultInputOn(EditorViewDefaultInputs inputs) => toolSet?.IsDefaultInputOn(inputs) ?? true;

    /// <inheritdoc/>
    public Task<(IEditorViewTool ActiveTool, IReadOnlyList<IEditorViewTool> FailedTools)> GetStateAsync()
    {
        return controller.InvokeAsync(() => (ToolSet.ActiveTool, (IReadOnlyList<IEditorViewTool>)ToolSet.Tools.Where(ToolSet.IsFailed).ToList()));
    }

    /// <inheritdoc/>
    public Task<bool> RunMenuEntryAsync(IEditorViewTool tool, EditorViewMenuEntry entry)
    {
        return controller.InvokeAsync(() => ToolSet.RunMenuEntry(tool, entry));
    }

    /// <inheritdoc/>
    public Task<bool> ActivateAsync(IEditorViewTool tool)
    {
        return controller.InvokeAsync(() => ToolSet.Activate(tool));
    }

    /// <inheritdoc/>
    public Task SetSpaceAsync(TransformationSpace space)
    {
        return controller.InvokeAsync(() => Context.Transformation.Space = space);
    }

    /// <inheritdoc/>
    public Task SetGizmoSizeAsync(float size)
    {
        return controller.InvokeAsync(() => Context.Transformation.GizmoSize = size);
    }

    /// <inheritdoc/>
    public Task SetSnapAsync(Transformation transformation, float value, bool isActive)
    {
        return controller.InvokeAsync(() => Context.Transformation.SetSnap(transformation, value, isActive));
    }

    /// <inheritdoc/>
    /// <remarks>The tools that draw something fitted to the view (the transformation gizmos) fit it to that view.</remarks>
    public void PrepareView(EditorGameViewCamera view)
    {
        if (toolSet is null)
            return;
        foreach (var tool in toolSet.Tools)
        {
            if (tool is IEditorGameViewDependent dependent && !toolSet.IsFailed(tool))
                dependent.PrepareView(view);
        }
    }

    /// <inheritdoc/>
    public override ValueTask DisposeAsync()
    {
        EnsureNotDestroyed(nameof(EditorGameToolService));

        if (registry is not null)
            registry.CollectionChanged -= RegistryChanged;
        if (toolSet is not null)
        {
            toolSet.ActiveToolChanged -= ToolSetActiveToolChanged;
            toolSet.ToolFailed -= ToolSetToolFailed;
            toolSet.ToolsChanged -= ToolSetToolsChanged;
            toolSet.Dispose();
        }
        return base.DisposeAsync();
    }

    /// <inheritdoc/>
    protected override Task<bool> Initialize(EditorServiceGame editorGame)
    {
        game = (EntityHierarchyEditorGame)editorGame;
        AddGizmoRenderStages(game);

        context = new EditorViewToolContext(game, controller, Selection, Services.Get<IEditorGameCameraService>(), UpdateTransformations);
        var pluginService = editor.ServiceProvider.TryGet<IAssetsPluginService>();
        registry = (pluginService as EditorPluginHost)?.GetDeclaredRegistry<SceneViewToolDescriptor>();
        var tools = CreateTools(GetToolDescriptors(ReadRegistry(registry, out toolOwners)), controller.Logger, [], failedToolTypes);
        toolSet = new EditorViewToolSet(tools, tools.OfType<TransformationTool>().ToList(), SwitchGizmo, controller.Logger, pluginService as IPluginContributionGuard, OwnerOf);
        toolSet.ActiveToolChanged += ToolSetActiveToolChanged;
        toolSet.ToolFailed += ToolSetToolFailed;
        toolSet.ToolsChanged += ToolSetToolsChanged;
        // The registry changes on the editor's thread; the view follows it on the game thread
        if (registry is not null)
            registry.CollectionChanged += RegistryChanged;

        // The gizmos load their materials and fonts from the database
        MicrothreadLocalDatabases.MountCommonDatabase();
        toolSet.Initialize(context);

        // After the selection changes, which the controller posts with the default priority (0), as in the WPF scene editor
        game.Script.AddTask(Update, 1);
        return Task.FromResult(true);
    }

    /// <summary>
    /// Gets the tools of a view: the built-in tools (select, translate, rotate and scale), then the tools of the plugins,
    /// each type once. The built-in tools don't depend on the plugins the user interface registers: the WPF editor doesn't
    /// register the default plugin of the cross-platform one, which declares them.
    /// </summary>
    internal static IReadOnlyList<SceneViewToolDescriptor> GetToolDescriptors(IEnumerable<SceneViewToolDescriptor> pluginTools)
    {
        var builtIn = BuiltInToolTypes.Select(SceneViewToolDescriptor.FromType);
        return builtIn.Concat(pluginTools).DistinctBy(x => x.ToolType).ToList();
    }

    /// <summary>
    /// The tools of the editor itself, whichever plugin declares them: they run as the editor's own, without the guard and
    /// without the time limit of an update (a large Ctrl+drag duplication keeps the translation busy for minutes).
    /// </summary>
    private static readonly Type[] BuiltInToolTypes = [typeof(SelectTool), typeof(TranslateTool), typeof(RotateTool), typeof(ScaleTool)];

    /// <summary>
    /// Gets one instance of every given tool, in toolbar order: by <see cref="SceneViewToolDescriptor.Order"/>, then by type
    /// name. A tool of <paramref name="existing"/> is kept; another is created. A tool that can't be created, or a type that is
    /// not an <see cref="IEditorViewTool"/>, is logged and left out.
    /// </summary>
    /// <param name="descriptors">The tools.</param>
    /// <param name="logger">The logger of the tools left out.</param>
    /// <param name="existing">The tools that the view already has.</param>
    /// <param name="failedTypes">The types that could not be created before: left out without a new error; the types that
    /// fail now are added.</param>
    /// <returns>The tools, with the select tool first.</returns>
    internal static List<IEditorViewTool> CreateTools(IEnumerable<SceneViewToolDescriptor> descriptors, ILogger logger, IEnumerable<IEditorViewTool>? existing = null, ISet<Type>? failedTypes = null)
    {
        var existingTools = (existing ?? []).ToDictionary(x => x.GetType());
        var tools = new List<IEditorViewTool>();
        var ordered = descriptors
            .DistinctBy(x => x.ToolType)
            .OrderBy(x => x.Order)
            .ThenBy(x => x.ToolType.FullName, StringComparer.Ordinal);
        foreach (var descriptor in ordered)
        {
            var type = descriptor.ToolType;
            if (existingTools.TryGetValue(type, out var tool))
            {
                tools.Add(tool);
                continue;
            }
            if (failedTypes?.Contains(type) == true)
                continue;
            if (!type.IsAssignableTo(typeof(IEditorViewTool)))
            {
                logger.Error($"The tool {type.FullName} ({type.Assembly.GetName().Name}) is not an {nameof(IEditorViewTool)} and is turned off.");
                failedTypes?.Add(type);
                continue;
            }
            try
            {
                tools.Add((IEditorViewTool)Activator.CreateInstance(type)!);
            }
            catch (Exception e)
            {
                logger.Error($"The tool {type.FullName} ({type.Assembly.GetName().Name}) could not be created and is turned off.", e);
                failedTypes?.Add(type);
            }
        }
        // The select tool comes first: the view goes back to it when a tool fails
        var selectTool = tools.OfType<SelectTool>().FirstOrDefault() ?? new SelectTool();
        tools.Remove(selectTool);
        tools.Insert(0, selectTool);
        return tools;
    }

    /// <summary>
    /// Renders the transformation gizmos over the scene, as the WPF scene editor.
    /// </summary>
    private static void AddGizmoRenderStages(EntityHierarchyEditorGame editorGame)
    {
        var compositor = editorGame.EditorSceneSystem.GraphicsCompositor;
        var opaqueRenderStage = new RenderStage("TransformGizmoOpaque", "Main");
        var transparentRenderStage = new RenderStage("TransformGizmoTransparent", "Main") { SortMode = new BackToFrontSortMode() };
        compositor.RenderStages.Add(opaqueRenderStage);
        compositor.RenderStages.Add(transparentRenderStage);

        var meshRenderFeature = compositor.RenderFeatures.OfType<MeshRenderFeature>().First();
        // Reset all stages for the transformation gizmo group
        meshRenderFeature.RenderStageSelectors.Add(new SimpleGroupToRenderStageSelector
        {
            RenderGroup = TransformationGizmo.TransformationGizmoGroupMask,
        });
        meshRenderFeature.RenderStageSelectors.Add(new MeshTransparentRenderStageSelector
        {
            EffectName = EditorGraphicsCompositorHelper.EditorForwardShadingEffect,
            RenderGroup = TransformationGizmo.TransformationGizmoGroupMask,
            OpaqueRenderStage = opaqueRenderStage,
            TransparentRenderStage = transparentRenderStage,
        });
        meshRenderFeature.PipelineProcessors.Add(new MeshPipelineProcessor { TransparentRenderStage = transparentRenderStage });

        var editorCompositor = (EditorTopLevelCompositor)compositor.Game;
        editorCompositor.AddOverlayGizmoCompositor(new SceneRendererCollection
        {
            new SingleStageRenderer { RenderStage = opaqueRenderStage, Name = "Transform Opaque Gizmos" },
            new SingleStageRenderer { RenderStage = transparentRenderStage, Name = "Transform Transparent Gizmos" },
        }, EditorTopLevelCompositor.TopmostOverlayGizmoLayer);
    }

    private async Task Update()
    {
        while (!IsDisposed)
        {
            if (IsActive)
            {
                var frame = ReadInput();
                Context.Transformation.IsSnapKeyDown = Game.Input.IsKeyDown(DynamicSnap);
                if (frame.IsMouseAvailable && Game.Input.IsKeyPressed(SnapSelectionToGrid))
                    SnapSelectionToGridPositions();

                var clicks = await ToolSet.Update(frame);
                IsControllingMouse = ToolSet.IsUsingMouse && IsMouseAvailable;
                foreach (var click in clicks)
                {
                    Dispatch(click);
                }
            }

            await Game.Script.NextFrame();
        }
    }

    private EditorViewInputFrame ReadInput()
    {
        var input = Game.Input;
        var backBuffer = Game.GraphicsDevice.Presenter.BackBuffer;
        return new EditorViewInputFrame
        {
            PressedButtons = input.PressedButtons.ToHashSet(),
            ReleasedButtons = input.ReleasedButtons.ToHashSet(),
            DownButtons = input.DownButtons.ToHashSet(),
            PressedKeys = input.PressedKeys.ToHashSet(),
            MouseDelta = input.MouseDelta * new Vector2(backBuffer.Width, backBuffer.Height),
            IsMouseAvailable = IsMouseAvailable,
            IsMouseAvailableToContextMenu = Context.Camera is IEditorGameMouseService camera ? IsMouseAvailableExcept(camera) : IsMouseAvailable,
            HasCameraMoved = HasCameraMoved(),
        };
    }

    /// <summary>
    /// Whether the camera moved since the last frame (its keys move it while the right button is down).
    /// </summary>
    private bool HasCameraMoved()
    {
        var position = Context.Camera.Position;
        var moved = position != lastCameraPosition;
        lastCameraPosition = position;
        return moved;
    }

    /// <summary>
    /// Gives a click to the selection, when the arbitration says so, then to the active tool, and asks for the context menu
    /// on a right click without drag.
    /// </summary>
    private void Dispatch(EditorViewToolClick toolClick)
    {
        var input = Game.Input;
        var entity = Selection.GetEntityUnderMouse(out var picking);
        var isCtrlDown = input.IsKeyDown(Keys.LeftCtrl) || input.IsKeyDown(Keys.RightCtrl);
        if (toolClick.ToSelection)
            Selection.Select(entity, isCtrlDown);

        var camera = Context.Camera;
        var mousePosition = input.MousePosition;
        var click = new EditorViewClick
        {
            Button = toolClick.Button,
            Position = mousePosition,
            IsCtrlDown = isCtrlDown,
            IsShiftDown = input.IsKeyDown(Keys.LeftShift) || input.IsKeyDown(Keys.RightShift),
            IsAltDown = input.IsKeyDown(Keys.LeftAlt) || input.IsKeyDown(Keys.RightAlt),
            Entity = entity,
            Picking = picking,
            Ray = EditorGameHelper.CalculateRayFromMousePosition(camera.Component, mousePosition, Matrix.Invert(camera.ViewMatrix)),
        };
        if (toolClick.ToTool)
            ToolSet.Click(click);
        if (toolClick.OpensContextMenu)
            RequestContextMenu(click);
    }

    /// <summary>
    /// Asks the user interface to open the context menu of a right click, with what the active tool adds.
    /// </summary>
    internal void RequestContextMenu(EditorViewClick click)
    {
        var (tool, menu) = ToolSet.GetContextMenu(click);
        // A tool that replaces the other entries with none keeps the menu closed
        if (menu is { ReplacesOthers: true, Entries.Count: 0 })
            return;

        ContextMenuRequested?.Invoke(this, new EditorViewContextMenuRequest
        {
            Click = click,
            EntityId = click.Entity is { } entity ? controller.GetAbsoluteId(entity) : null,
            Tool = tool,
            ToolMenu = menu,
        });
    }

    /// <summary>
    /// Snaps the positions of the selected roots to the translation snap, as one undoable operation.
    /// </summary>
    private void SnapSelectionToGridPositions()
    {
        var transformations = new Dictionary<AbsoluteId, TransformationTRS>();
        foreach (var id in Selection.GetSelectedRootIds())
        {
            if (controller.FindGameSidePart(id) is not Entity entity)
                continue;

            entity.Transform.Position = MathUtil.Snap(entity.Transform.Position, Context.Transformation.TranslationSnap);
            transformations.Add(id, new TransformationTRS(entity.Transform));
        }
        if (transformations.Count > 0)
            UpdateTransformations(transformations);
    }

    private void UpdateTransformations(IReadOnlyDictionary<AbsoluteId, TransformationTRS> transformations)
    {
        editor.Dispatcher.InvokeAsync(() => editor.UpdateTransformations(transformations));
    }

    private void ToolSetActiveToolChanged(object? sender, EventArgs e) => ActiveToolChanged?.Invoke(this, ToolSet.ActiveTool);

    private void ToolSetToolFailed(object? sender, IEditorViewTool tool) => ToolFailed?.Invoke(this, tool);

    private void ToolSetToolsChanged(object? sender, EventArgs e) => ToolsChanged?.Invoke(this, EventArgs.Empty);

    private object? OwnerOf(IEditorViewTool tool) => BuiltInToolTypes.Contains(tool.GetType()) ? null : toolOwners.GetValueOrDefault(tool.GetType());

    private void RegistryChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Several plugins join or leave at once: the view reads the registry once they did
        controller.InvokeAsync(RefreshTools);
    }

    /// <summary>
    /// Adds the tools of the plugins that joined, and removes the ones of the plugins that left (game thread).
    /// </summary>
    private void RefreshTools()
    {
        if (IsDisposed || toolSet is null)
            return;

        FollowRegistry(registry, toolSet, owners => toolOwners = owners, controller.Logger, failedToolTypes);
    }

    /// <summary>
    /// The tools that the plugins register, and the owner of each type (the first plugin that registered it).
    /// </summary>
    internal static IReadOnlyList<SceneViewToolDescriptor> ReadRegistry(ContributionRegistry<SceneViewToolDescriptor>? registry, out IReadOnlyDictionary<Type, object?> owners)
    {
        var typeOwners = new Dictionary<Type, object?>();
        owners = typeOwners;
        if (registry is null)
            return [];

        var descriptors = registry.Items;
        foreach (var descriptor in descriptors)
            typeOwners.TryAdd(descriptor.ToolType, registry.GetOwner(descriptor));
        return descriptors;
    }

    /// <summary>
    /// Gives the tools of a view what the registry holds now: the tools of the plugins that joined are created, and the
    /// ones of the plugins that left go (game thread).
    /// </summary>
    /// <param name="setOwners">Receives the owner of each tool type, before the tool set changes: the tool set reads them to run a new tool through the guard from its initialization.</param>
    internal static void FollowRegistry(ContributionRegistry<SceneViewToolDescriptor>? registry, EditorViewToolSet toolSet, Action<IReadOnlyDictionary<Type, object?>> setOwners, ILogger logger, ISet<Type> failedTypes)
    {
        var descriptors = ReadRegistry(registry, out var owners);
        var tools = CreateTools(GetToolDescriptors(descriptors), logger, toolSet.Tools, failedTypes);
        setOwners(owners);
        toolSet.SetTools(tools);
    }
}
