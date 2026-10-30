// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Reflection;
using Stride.Assets.Editor.ViewModels;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Game;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Tools;
using Stride.Assets.Presentation.AssetEditors.GameEditor;
using Stride.Assets.Presentation.AssetEditors.GameEditor.Game;
using Stride.Assets.Presentation.AssetEditors.Gizmos;
using Stride.Assets.Presentation.SceneEditor;
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

namespace Stride.Assets.Editor.EditorGame.Tools;

/// <summary>
/// The tools of the view of a scene or prefab editor: select, translate, rotate, scale and the tools of the plugins. It
/// arbitrates the input of the view between the active tool, the selection and the camera
/// (<see cref="EditorViewToolSet"/>), and has the keys of the transformation gizmos of the WPF scene editor.
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item>Q: select. W: translate. E: rotate. R: scale. Space: next transformation.</item>
/// <item>Shift while dragging a gizmo: snap. N: snap the selection to the grid.</item>
/// </list>
/// The keys are the scene editor settings (<see cref="SceneEditorSettings"/>), shared with the WPF editor; the snap values are
/// the defaults of the WPF scene settings, which are not read yet. The cross-platform counterpart of the WPF
/// <c>EditorGameEntityTransformService</c>.
/// </remarks>
[EditorGameService(typeof(EntityHierarchyEditorController), Order = 210)]
public sealed class EditorGameToolService : EditorGameMouseServiceBase, IEditorGameToolService, IEditorGameViewModelService
{
    // The key bindings of the scene editor settings, read as they are used so that a change applies at once, as in WPF (the
    // switch key when the view opens)
    private static Keys SwitchGizmo => SceneEditorSettings.SwitchGizmo.GetValue();
    private static Keys SnapSelectionToGrid => SceneEditorSettings.SnapSelectionToGrid.GetValue();
    private static Keys DynamicSnap => SceneEditorSettings.ControlDynamicSnapSelectionToGrid.GetValue();

    private readonly EntityHierarchyEditorController controller;
    private readonly EntityHierarchyEditorViewModel editor;
    private EditorViewToolSet? toolSet;
    private EditorViewToolContext? context;
    private EntityHierarchyEditorGame? game;

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

    /// <summary>
    /// Gets the tools of the view, in toolbar order, once the service is initialized (any thread: the list does not change).
    /// </summary>
    internal IReadOnlyList<IEditorViewTool> Tools => toolSet?.Tools ?? [];

    /// <summary>
    /// Raised with the new active tool (game thread).
    /// </summary>
    internal event EventHandler<IEditorViewTool>? ActiveToolChanged;

    /// <summary>
    /// Raised with a tool turned off after an error (game thread).
    /// </summary>
    internal event EventHandler<IEditorViewTool>? ToolFailed;

    private EditorViewToolSet ToolSet => toolSet ?? throw new InvalidOperationException("The service is not initialized.");

    private EditorViewToolContext Context => context ?? throw new InvalidOperationException("The service is not initialized.");

    private EntityHierarchyEditorGame Game => game ?? throw new InvalidOperationException("The service is not initialized.");

    private EditorGameEntitySelectionService Selection => Services.Get<EditorGameEntitySelectionService>();

    /// <inheritdoc/>
    public bool IsDefaultInputOn(EditorViewDefaultInputs inputs) => toolSet?.IsDefaultInputOn(inputs) ?? true;

    /// <summary>
    /// Gets the active tool and the tools turned off after an error.
    /// </summary>
    internal Task<(IEditorViewTool ActiveTool, IReadOnlyList<IEditorViewTool> FailedTools)> GetStateAsync()
    {
        return controller.InvokeAsync(() => (ToolSet.ActiveTool, (IReadOnlyList<IEditorViewTool>)ToolSet.Tools.Where(ToolSet.IsFailed).ToList()));
    }

    /// <summary>
    /// Makes a tool the active one.
    /// </summary>
    /// <returns><c>false</c> when the tool was turned off.</returns>
    internal Task<bool> ActivateAsync(IEditorViewTool tool)
    {
        return controller.InvokeAsync(() => ToolSet.Activate(tool));
    }

    /// <summary>
    /// Sets the space of the translation and rotation gizmos.
    /// </summary>
    internal Task SetSpaceAsync(TransformationSpace space)
    {
        return controller.InvokeAsync(() => Context.Transformation.Space = space);
    }

    /// <summary>
    /// Sets the snap of a transformation, and whether it snaps without the snap key.
    /// </summary>
    internal Task SetSnapAsync(Transformation transformation, float value, bool isActive)
    {
        return controller.InvokeAsync(() => Context.Transformation.SetSnap(transformation, value, isActive));
    }

    /// <inheritdoc/>
    public override ValueTask DisposeAsync()
    {
        EnsureNotDestroyed(nameof(EditorGameToolService));

        if (toolSet is not null)
        {
            toolSet.ActiveToolChanged -= ToolSetActiveToolChanged;
            toolSet.ToolFailed -= ToolSetToolFailed;
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
        var toolTypes = editor.ServiceProvider.TryGet<IAssetsPluginService>()?.GetEditorViewToolTypes() ?? [];
        var tools = CreateTools(toolTypes, controller.Logger);
        toolSet = new EditorViewToolSet(tools, tools.OfType<TransformationTool>().ToList(), SwitchGizmo, controller.Logger);
        toolSet.ActiveToolChanged += ToolSetActiveToolChanged;
        toolSet.ToolFailed += ToolSetToolFailed;

        // The gizmos load their materials and fonts from the database
        MicrothreadLocalDatabases.MountCommonDatabase();
        toolSet.Initialize(context);

        // After the selection changes, which the controller posts with the default priority (0), as in the WPF scene editor
        game.Script.AddTask(Update, 1);
        return Task.FromResult(true);
    }

    /// <summary>
    /// Creates one instance of every given tool type, in toolbar order: by <see cref="EditorViewToolAttribute.Order"/>, then
    /// by type name. A tool that can't be created, or a type that is not an <see cref="IEditorViewTool"/>, is logged and left out.
    /// </summary>
    /// <returns>The tools, with the select tool first.</returns>
    internal static List<IEditorViewTool> CreateTools(IEnumerable<Type> toolTypes, ILogger logger)
    {
        var tools = new List<IEditorViewTool>();
        var types = toolTypes
            .OrderBy(x => x.GetCustomAttribute<EditorViewToolAttribute>()?.Order ?? EditorViewToolAttribute.DefaultOrder)
            .ThenBy(x => x.FullName, StringComparer.Ordinal);
        foreach (var type in types)
        {
            if (!type.IsAssignableTo(typeof(IEditorViewTool)))
            {
                logger.Error($"The tool {type.FullName} ({type.Assembly.GetName().Name}) is not an {nameof(IEditorViewTool)} and is turned off.");
                continue;
            }
            try
            {
                tools.Add((IEditorViewTool)Activator.CreateInstance(type)!);
            }
            catch (Exception e)
            {
                logger.Error($"The tool {type.FullName} ({type.Assembly.GetName().Name}) could not be created and is turned off.", e);
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
        };
    }

    /// <summary>
    /// Gives a click to the selection, when the arbitration says so, then to the active tool.
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
        ToolSet.Click(new EditorViewClick
        {
            Button = toolClick.Button,
            Position = mousePosition,
            IsCtrlDown = isCtrlDown,
            IsShiftDown = input.IsKeyDown(Keys.LeftShift) || input.IsKeyDown(Keys.RightShift),
            IsAltDown = input.IsKeyDown(Keys.LeftAlt) || input.IsKeyDown(Keys.RightAlt),
            Entity = entity,
            Picking = picking,
            Ray = EditorGameHelper.CalculateRayFromMousePosition(camera.Component, mousePosition, Matrix.Invert(camera.ViewMatrix)),
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
}
