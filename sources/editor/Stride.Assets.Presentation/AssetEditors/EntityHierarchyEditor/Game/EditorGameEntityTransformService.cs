// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Stride.Core.BuildEngine;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Extensions;
using Stride.Core.Mathematics;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Services;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.ViewModels;
using Stride.Assets.Presentation.AssetEditors.GameEditor;
using Stride.Assets.Presentation.AssetEditors.GameEditor.Game;
using Stride.Assets.Presentation.AssetEditors.GameEditor.Services;
using Stride.Assets.Presentation.AssetEditors.Gizmos;
using Stride.Assets.Presentation.SceneEditor;
using Stride.Editor.EditorGame.Game;
using Stride.Engine;
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

        public override IEnumerable<Type> Dependencies {  get { yield return typeof(IEditorGameEntitySelectionService); } }

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
        public override Task DisposeAsync()
        {
            EnsureNotDestroyed(nameof(EditorGameEntityTransformService));

            var selectionService = Services.Get<IEditorGameEntitySelectionService>();
            if (selectionService != null)
                selectionService.SelectionUpdated -= UpdateModifiedEntitiesList;
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
            TranslationGizmo.TransformationEnded += OnGizmoTransformationFinished;
            ScaleGizmo.TransformationEnded += OnGizmoTransformationFinished;
            RotationGizmo.TransformationEnded += OnGizmoTransformationFinished;

            transformationGizmos.Add(TranslationGizmo);
            transformationGizmos.Add(RotationGizmo);
            transformationGizmos.Add(ScaleGizmo);

            Services.Get<IEditorGameEntitySelectionService>().SelectionUpdated += UpdateModifiedEntitiesList;

            // Initialize and add the Gizmo entities to the gizmo scene
            MicrothreadLocalDatabases.MountCommonDatabase();

            // initialize the gizmo
            foreach (var gizmo in transformationGizmos)
                gizmo.Initialize(game.Services, editorScene);

            // Deactivate all transformation gizmo by default
            foreach (var gizmo in transformationGizmos)
                gizmo.IsEnabled = false;

            // set the default active transformation gizmo
            ActiveTransformationGizmo = TranslationGizmo;

            // Start update script (with priority 1 so that it happens after UpdateModifiedEntitiesList is called -- which usually happens from a EditorGameComtroller.PostAction() which has a default priority 0)
            game.Script.AddTask(Update, 1);
            return Task.FromResult(true);
        }

        private async Task Update()
        {
            while (!IsDisposed)
            {
                if (IsActive)
                {
                    if (IsMouseAvailable)
                    {
                        if (game.Input.IsKeyPressed(SceneEditorSettings.SnapSelectionToGrid.GetValue()))
                        {
                            SnapSelectionToGrid();
                        }
                        if (game.Input.IsKeyPressed(SceneEditorSettings.TranslationGizmo.GetValue()))
                        {
                            await editor.Dispatcher.InvokeAsync(() => editor.Transform.ActiveTransformation = Transformation.Translation);
                        }
                        if (game.Input.IsKeyPressed(SceneEditorSettings.RotationGizmo.GetValue()))
                        {
                            await editor.Dispatcher.InvokeAsync(() => editor.Transform.ActiveTransformation = Transformation.Rotation);
                        }
                        if (game.Input.IsKeyPressed(SceneEditorSettings.ScaleGizmo.GetValue()))
                        {
                            await editor.Dispatcher.InvokeAsync(() => editor.Transform.ActiveTransformation = Transformation.Scale);
                        }
                        if (game.Input.IsKeyPressed(SceneEditorSettings.SwitchGizmo.GetValue()))
                        {
                            var current = activeTransformation;
                            var next = (int)(current + 1) % Enum.GetValues(typeof(Transformation)).Length;
                            await editor.Dispatcher.InvokeAsync(() => editor.Transform.ActiveTransformation = (Transformation)next);
                        }
                    }

                    IEnumerable<Task> tasks;
                    lock (transformationGizmos)
                    {
                        tasks = transformationGizmos.Select(x => x.Update());
                    }

                    IsControllingMouse = activeTransformationGizmo != null && activeTransformationGizmo.IsUnderMouse() && IsMouseAvailable;

                    await Task.WhenAll(tasks);
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


        private void UpdateModifiedEntitiesList(object sender, [NotNull] EntitySelectionEventArgs e)
        {
            EntityWithGizmo = e.NewSelection.LastOrDefault();
            if (ActiveTransformationGizmo != null && EntityWithGizmo == null)
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

        private void OnGizmoTransformationFinished(object sender, EventArgs e)
        {
            var transformations = new Dictionary<AbsoluteId, TransformationTRS>();
            foreach (var item in Selection.GetSelectedRootIds())
            {
                var entity = (Entity)controller.FindGameSidePart(item);
                transformations.Add(item, new TransformationTRS(entity.Transform));
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
    }
}
