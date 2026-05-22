// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Services;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.ViewModels;
using Stride.Assets.Presentation.AssetEditors.GameEditor;
using Stride.Assets.Presentation.AssetEditors.GameEditor.Game;
using Stride.Assets.Presentation.AssetEditors.GameEditor.Services;
using Stride.Assets.Presentation.AssetEditors.Gizmos;
using Stride.Assets.Presentation.SceneEditor;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.BuildEngine;
using Stride.Core.Extensions;
using Stride.Core.Mathematics;
using Stride.Editor.EditorGame.Game;
using Stride.Engine;
using Stride.Input;
using Stride.Rendering;
using Stride.Rendering.Compositing;

namespace Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Game
{
    public class EditorGameEntityTransformService : EditorGameMouseServiceBase, IEditorGameEntityTransformViewModelService
    {
        private readonly List<TransformationGizmo> transformationGizmos = new List<TransformationGizmo>();
        // TODO: referencing EntityTransformationViewModel should be enough
        private readonly EntityHierarchyEditorViewModel editor;
        private readonly IEditorGameController controller;
        private Scene editorScene;
        private TransformationGizmo activeTransformationGizmo;
        private Entity entityWithGizmo;
        private EntityHierarchyEditorGame game;
        private Transformation activeTransformation;
        private TransformationSpace space;
        private double gizmoSize = 1.0f;
        private bool dynamicSnappingInUse = false;

        private bool isEntityDuplicationInProgress = false;
        private Entity entityDuplicationPreviousEntityWithGizmo;
        private IReadOnlyCollection<Entity> entityDuplicationPreviousSelection;
        private readonly Dictionary<Entity, Entity> entityDuplicationSrcToPreviewEntityMap = [];
        private readonly Dictionary<AbsoluteId, Entity> entityDuplicationSrcIdToPreviewEntityMap = [];

        public EditorGameEntityTransformService([NotNull] EntityHierarchyEditorViewModel editor, [NotNull] IEditorGameController controller)
        {
            if (editor == null) throw new ArgumentNullException(nameof(editor));
            if (controller == null) throw new ArgumentNullException(nameof(controller));
            this.editor = editor;
            this.controller = controller;
        }

        /// <inheritdoc/>
        public override bool IsControllingMouse { get; protected set; }

        /// <summary>
        /// Gets the translation gizmo.
        /// </summary>
        public TranslationGizmo TranslationGizmo { get; private set; }

        /// <summary>
        /// Gets the rotation gizmo.
        /// </summary>
        public RotationGizmo RotationGizmo { get; private set; }

        /// <summary>
        /// Gets the scale gizmo.
        /// </summary>
        public ScaleGizmo ScaleGizmo { get; private set; }

        public TransformationGizmo ActiveTransformationGizmo
        {
            get { return activeTransformationGizmo; }
            private set
            {
                if (activeTransformationGizmo != null)
                    activeTransformationGizmo.IsEnabled = false;

                activeTransformationGizmo = value;

                if (activeTransformationGizmo != null && EntityWithGizmo != null)
                {
                    activeTransformationGizmo.IsEnabled = true;
                }
            }
        }

        /// <summary>
        /// Gets the current entity which has the gizmo attached.
        /// </summary>
        public Entity EntityWithGizmo
        {
            get { return entityWithGizmo; }
            set
            {
                entityWithGizmo = value;
                foreach (var gizmo in transformationGizmos)
                    gizmo.AnchorEntity = entityWithGizmo;

                ActiveTransformationGizmo = activeTransformationGizmo;
            }
        }

        public override IEnumerable<Type> Dependencies
        {
            get
            {
                yield return typeof(IEditorGameEntitySelectionService);
                yield return typeof(EditorGameModelSelectionService);
            }
        }

        Transformation IEditorGameTransformViewModelService.ActiveTransformation
        {
            get
            {
                return activeTransformation;
            }
            set
            {
                activeTransformation = value;
                TransformationGizmo nextGizmo;
                switch (value)
                {
                    case Transformation.Translation:
                        nextGizmo = TranslationGizmo;
                        break;
                    case Transformation.Rotation:
                        nextGizmo = RotationGizmo;
                        break;
                    case Transformation.Scale:
                        nextGizmo = ScaleGizmo;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(value));
                }

                controller.InvokeAsync(() => ActiveTransformationGizmo = nextGizmo);
            }
        }

        TransformationSpace IEditorGameTransformViewModelService.TransformationSpace { get { return space; } set { space = value; controller.InvokeAsync(() => transformationGizmos.ForEach(x => x.Space = value)); } }

        double IEditorGameEntityTransformViewModelService.GizmoSize { get { return gizmoSize; } set { gizmoSize = value; controller.InvokeAsync(() => transformationGizmos.ForEach(x => x.SizeFactor = SmoothGizmoSize((float)value))); } }

        internal IEditorGameEntitySelectionService Selection => Services.Get<IEditorGameEntitySelectionService>();

        /// <inheritdoc />
        public override ValueTask DisposeAsync()
        {
            EnsureNotDestroyed(nameof(EditorGameEntityTransformService));

            OnDeactivate();
            return base.DisposeAsync();
        }

        protected override Task<bool> Initialize(EditorServiceGame editorGame)
        {
            game = (EntityHierarchyEditorGame)editorGame;
            editorScene = game.EditorScene;

            var transformMainGizmoRenderStage = new RenderStage("TransformGizmoOpaque", "Main");
            var transformTransparentGizmoRenderStage = new RenderStage("TransformGizmoTransparent", "Main") { SortMode = new BackToFrontSortMode() };
            game.EditorSceneSystem.GraphicsCompositor.RenderStages.Add(transformMainGizmoRenderStage);
            game.EditorSceneSystem.GraphicsCompositor.RenderStages.Add(transformTransparentGizmoRenderStage);

            var meshRenderFeature = game.EditorSceneSystem.GraphicsCompositor.RenderFeatures.OfType<MeshRenderFeature>().First();
            // Reset all stages for TransformationGrizmoGroup
            meshRenderFeature.RenderStageSelectors.Add(new SimpleGroupToRenderStageSelector
            {
                RenderGroup = TransformationGizmo.TransformationGizmoGroupMask,
            });
            meshRenderFeature.RenderStageSelectors.Add(new MeshTransparentRenderStageSelector
            {
                EffectName = EditorGraphicsCompositorHelper.EditorForwardShadingEffect,
                RenderGroup = TransformationGizmo.TransformationGizmoGroupMask,
                OpaqueRenderStage = transformMainGizmoRenderStage,
                TransparentRenderStage = transformTransparentGizmoRenderStage,
            });
            meshRenderFeature.PipelineProcessors.Add(new MeshPipelineProcessor { TransparentRenderStage = transformTransparentGizmoRenderStage });

            var editorCompositor = (EditorTopLevelCompositor)game.EditorSceneSystem.GraphicsCompositor.Game;
            editorCompositor.PostGizmoCompositors.Add(new ClearRenderer { ClearFlags = ClearRendererFlags.DepthOnly });
            editorCompositor.PostGizmoCompositors.Add(new SingleStageRenderer { RenderStage = transformMainGizmoRenderStage, Name = "Transform Opaque Gizmos" });
            editorCompositor.PostGizmoCompositors.Add(new SingleStageRenderer { RenderStage = transformTransparentGizmoRenderStage, Name = "Transform Transparent Gizmos" });

            TranslationGizmo = new TranslationGizmo();
            RotationGizmo = new RotationGizmo();
            ScaleGizmo = new ScaleGizmo();
            TranslationGizmo.TransformationStarted += OnGizmoTransformationStarted;
            ScaleGizmo.TransformationStarted += OnGizmoTransformationStarted;
            RotationGizmo.TransformationStarted += OnGizmoTransformationStarted;
            TranslationGizmo.TransformationEnded += OnGizmoTransformationFinished;
            ScaleGizmo.TransformationEnded += OnGizmoTransformationFinished;
            RotationGizmo.TransformationEnded += OnGizmoTransformationFinished;

            transformationGizmos.Add(TranslationGizmo);
            transformationGizmos.Add(RotationGizmo);
            transformationGizmos.Add(ScaleGizmo);

            // Initialize and add the Gizmo entities to the gizmo scene
            MicrothreadLocalDatabases.MountCommonDatabase();

            // initialize the gizmo
            foreach (var gizmo in transformationGizmos)
                gizmo.Initialize(game.Services, editorScene);

            OnActivate();

            // set the default active transformation gizmo
            ActiveTransformationGizmo = TranslationGizmo;

            // Start update script (with priority 1 so that it happens after UpdateModifiedEntitiesList is called -- which usually happens from a EditorGameComtroller.PostAction() which has a default priority 0)
            game.Script.AddTask(Update, 1);
            return Task.FromResult(true);
        }

        protected void OnActivate()
        {
            Services.Get<IEditorGameEntitySelectionService>().SelectionUpdated += UpdateModifiedEntitiesList;

            // Deactivate all transformation gizmo by default
            foreach (var gizmo in transformationGizmos)
                gizmo.IsEnabled = false;
        }

        protected void OnDeactivate()
        {
            EntityWithGizmo = null;
            foreach (var gizmo in transformationGizmos)
            {
                gizmo.CancelTransform();
                gizmo.ModifiedEntities = [];
                gizmo.IsEnabled = false;
            }

            isEntityDuplicationInProgress = false;
            foreach (var (_, previewEntity) in entityDuplicationSrcToPreviewEntityMap)
            {
                // Detach from scene
                previewEntity.SetParent(null);
                previewEntity.Scene = null;
            }
            entityDuplicationSrcToPreviewEntityMap.Clear();
            entityDuplicationSrcIdToPreviewEntityMap.Clear();

            var selectionService = Services.Get<IEditorGameEntitySelectionService>();
            if (selectionService != null)
                selectionService.SelectionUpdated -= UpdateModifiedEntitiesList;
        }

        private async Task Update()
        {
            while (!IsDisposed)
            {
                if (IsActive)
                {
                    if (IsMouseAvailable)
                    {
                        // Snap the current selection to the grid, on keypress
                        if (game.Input.IsKeyPressed(SceneEditorSettings.SnapSelectionToGrid.GetValue()))
                        {
                            SnapSelectionToGrid();
                        }

                        // Use snapping while pressing a key and moving an object(entity) in the scene
                        DynamicSnapSelectionToGrid(game.Input.IsKeyDown(SceneEditorSettings.ControlDynamicSnapSelectionToGrid.GetValue()));

                        // Activate transformation snapping
                        if (game.Input.IsKeyPressed(SceneEditorSettings.TranslationGizmo.GetValue()))
                        {
                            editor.Dispatcher.InvokeAsync(() => editor.Transform.ActiveTransformation = Transformation.Translation);
                        }

                        // Activate rotation snapping
                        if (game.Input.IsKeyPressed(SceneEditorSettings.RotationGizmo.GetValue()))
                        {
                            editor.Dispatcher.InvokeAsync(() => editor.Transform.ActiveTransformation = Transformation.Rotation);
                        }

                        // Activate scale snapping
                        if (game.Input.IsKeyPressed(SceneEditorSettings.ScaleGizmo.GetValue()))
                        {
                            editor.Dispatcher.InvokeAsync(() => editor.Transform.ActiveTransformation = Transformation.Scale);
                        }

                        // Toggle between different snapping methods
                        if (game.Input.IsKeyPressed(SceneEditorSettings.SwitchGizmo.GetValue()))
                        {
                            var current = activeTransformation;
                            var next = (int)(current + 1) % Enum.GetValues<Transformation>().Length;
                            editor.Dispatcher.InvokeAsync(() => editor.Transform.ActiveTransformation = (Transformation)next);
                        }

                        if (game.Input.IsKeyPressed(Keys.Escape)
                            && activeTransformationGizmo is not null
                            && activeTransformationGizmo.IsTransformationInProgress)
                        {
                            activeTransformationGizmo.CancelTransform();
                        }
                    }

                    foreach (var x in transformationGizmos)
                    {
                        x.Update();
                    }

                    IsControllingMouse = activeTransformationGizmo != null && activeTransformationGizmo.IsEnabled && activeTransformationGizmo.IsUnderMouse() && IsMouseAvailable;
                }

                await game.Script.NextFrame();
            }
        }

        private void SnapSelectionToGrid()
        {
            if (Selection.SelectedRootIdCount == 0)
                return;

            var transformations = new Dictionary<AbsoluteId, TransformationTRS>();
            foreach (var item in Selection.GetSelectedRootIds())
            {
                var entity = (Entity)controller.FindGameSidePart(item);
                entity.Transform.Position = MathUtil.Snap(entity.Transform.Position, TranslationGizmo.SnapValue);
                transformations.Add(item, new TransformationTRS(entity.Transform));
            }

            InvokeTransformationFinished(transformations);
        }

        private void DynamicSnapSelectionToGrid(bool useDynamicSnapping)
        {
            if (!useDynamicSnapping && dynamicSnappingInUse)
            {
                activeTransformationGizmo.UseSnap = false;
                dynamicSnappingInUse = false;
            }

            if (useDynamicSnapping && !dynamicSnappingInUse && !ActiveTransformationGizmo.UseSnap)
            {
                dynamicSnappingInUse = true;
                activeTransformationGizmo.UseSnap = true;
            }
        }

        private void UpdateModifiedEntitiesList(object sender, [NotNull] EntitySelectionEventArgs e)
        {
            if (!IsActive || isEntityDuplicationInProgress)
            {
                return;
            }
            EntityWithGizmo = e.NewSelection.LastOrDefault();
            if (ActiveTransformationGizmo != null && !ActiveTransformationGizmo.IsTransformationInProgress && EntityWithGizmo == null)
            {
                // Reset the transformation axes if the selection is cleared.
                ActiveTransformationGizmo.ClearTransformationAxes();
            }

            var modifiedEntities = new List<Entity>();
            modifiedEntities.AddRange(e.NewSelection);

            // update the selected entities collections on transformation gizmo
            foreach (var gizmo in transformationGizmos)
            {
                gizmo.AnchorEntity = EntityWithGizmo;
                gizmo.ModifiedEntities = modifiedEntities;
            }
        }

        private void OnGizmoTransformationStarted(object sender, EventArgs e)
        {
            isEntityDuplicationInProgress = game.Input.IsKeyDown(Keys.LeftCtrl) || game.Input.IsKeyDown(Keys.RightCtrl);
            if (isEntityDuplicationInProgress)
            {
                // Duplication occurs in the editor so we should make the gizmo update proxy entities
                // until the editor returns the duplicated entities
                entityDuplicationPreviousEntityWithGizmo = EntityWithGizmo;
                entityDuplicationPreviousSelection = ActiveTransformationGizmo?.ModifiedEntities ?? [];
                entityDuplicationSrcToPreviewEntityMap.Clear();
                foreach (var id in Selection.GetSelectedRootIds())
                {
                    var entity = (Entity)controller.FindGameSidePart(id);
                    var previewEntity = BuildDuplicationPreviewEntity(entity);

                    entityDuplicationSrcToPreviewEntityMap[entity] = previewEntity;
                    entityDuplicationSrcIdToPreviewEntityMap[id] = previewEntity;
                }
                ActiveTransformationGizmo.RemapModifyingEntities(entityDuplicationSrcToPreviewEntityMap);
                if (EntityWithGizmo is not null
                    && entityDuplicationSrcToPreviewEntityMap.TryGetValue(EntityWithGizmo, out var anchorProxyEntity))
                {
                    EntityWithGizmo = anchorProxyEntity;
                }

                var modelSelectionService = Services.Get<EditorGameModelSelectionService>();
                modelSelectionService?.ChangeSelection(entityDuplicationSrcIdToPreviewEntityMap.Values.ToList());
            }
        }

        private void OnGizmoTransformationFinished(object sender, TransformationEndedEventArgs e)
        {
            if (isEntityDuplicationInProgress)
            {
                var newTransformations = new Dictionary<AbsoluteId, TransformationTRS?>();
                foreach (var (srcId, previewEntity) in entityDuplicationSrcIdToPreviewEntityMap)
                {
                    newTransformations.Add(srcId, new TransformationTRS(previewEntity.Transform));
                    // Detach from scene
                    previewEntity.SetParent(null);
                    previewEntity.Scene = null;
                }
                entityDuplicationSrcIdToPreviewEntityMap.Clear();
                entityDuplicationSrcToPreviewEntityMap.Clear();

                if (e.IsCanceled)
                {
                    // Reselect previous entities
                    ActiveTransformationGizmo?.ModifiedEntities = entityDuplicationPreviousSelection;
                    EntityWithGizmo = entityDuplicationPreviousEntityWithGizmo;
                    var modelSelectionService = Services.Get<EditorGameModelSelectionService>();
                    modelSelectionService?.ChangeSelection(entityDuplicationPreviousSelection);
                }
                else
                {
                    // Clear preview entities selection (which will select the duplicated entities after the editor finishes actual duplication)
                    ActiveTransformationGizmo?.ModifiedEntities = [];
                    EntityWithGizmo = null;
                    var modelSelectionService = Services.Get<EditorGameModelSelectionService>();
                    modelSelectionService?.ChangeSelection([]);
                    // Confirm duplication
                    editor.Dispatcher.InvokeAsync(() => editor.DuplicateEntities(newTransformations));
                }

                isEntityDuplicationInProgress = false;
                entityDuplicationPreviousEntityWithGizmo = null;
                entityDuplicationPreviousSelection = null;
                return;
            }

            if (e.IsCanceled)
            {
                return;
            }

            var transformations = new Dictionary<AbsoluteId, TransformationTRS>();
            foreach (var id in Selection.GetSelectedRootIds())
            {
                var entity = (Entity)controller.FindGameSidePart(id);
                transformations.Add(id, new TransformationTRS(entity.Transform));
            }

            InvokeTransformationFinished(transformations);
        }

        private void InvokeTransformationFinished(IReadOnlyDictionary<AbsoluteId, TransformationTRS> transformation)
        {
            editor.Dispatcher.InvokeAsync(() => editor.UpdateTransformations(transformation));
        }

        private static float SmoothGizmoSize(float value)
        {
            return value > 1 ? 1 + (value - 1) * 0.1f : (float)Math.Pow(value, 0.333);
        }

        void IEditorGameTransformViewModelService.UpdateSnap(Transformation transformation, float value, bool isActive)
        {
            controller.InvokeAsync(() =>
            {
                switch (transformation)
                {
                    case Transformation.Translation:
                        TranslationGizmo.SnapValue = value;
                        TranslationGizmo.UseSnap = isActive;
                        break;
                    case Transformation.Rotation:
                        RotationGizmo.SnapValue = value;
                        RotationGizmo.UseSnap = isActive;
                        break;
                    case Transformation.Scale:
                        ScaleGizmo.SnapValue = value;
                        ScaleGizmo.UseSnap = isActive;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(transformation));
                }
            });
        }

        private static Entity BuildDuplicationPreviewEntity(Entity srcEntity)
        {
            var dupePreviewEntity = srcEntity.Clone();
            dupePreviewEntity.Name = $"Duplication {dupePreviewEntity.Name}";
            var parentEntity = srcEntity.GetParent();
            if (parentEntity is not null)
            {
                dupePreviewEntity.SetParent(parentEntity);
            }
            else
            {
                dupePreviewEntity.Scene = srcEntity.Scene;
            }
            return dupePreviewEntity;
        }
    }
}
