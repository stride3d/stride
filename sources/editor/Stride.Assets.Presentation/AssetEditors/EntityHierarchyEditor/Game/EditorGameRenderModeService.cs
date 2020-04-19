// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Services;
using Stride.Assets.Presentation.AssetEditors.GameEditor;
using Stride.Assets.Presentation.AssetEditors.GameEditor.Game;
using Stride.Assets.Presentation.AssetEditors.GameEditor.Services;
using Stride.Assets.Presentation.SceneEditor;
using Stride.Editor.EditorGame.Game;
using Stride.Rendering;
using Stride.Rendering.Compositing;

namespace Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Game
{
    public class EditorGameRenderModeService : EditorGameServiceBase, IEditorGameRenderModeService, IEditorGameRenderModeViewModelService
    {
        private EntityHierarchyEditorGame game;
        private MaterialFilterRenderFeature materialFilterRenderFeature;

        // descibes the state of savedCameras (true: for game preview, false: for normal rendering)
        private bool isPreviewMode;
        private List<SceneCameraSlot> savedCameras = new List<SceneCameraSlot>();

        public EditorRenderMode RenderMode { get; set; } = EditorRenderMode.DefaultEditor;

        private readonly HashSet<IEditorGameMouseService> disabledMouseServicesDuringGamePreview = new HashSet<IEditorGameMouseService>();

        public override IEnumerable<Type> Dependencies { get { yield return typeof(IEditorGameMouseService); } }

        /// <inheritdoc/>
        public override Task DisposeAsync()
        {
            EnsureNotDestroyed(nameof(EditorGameRenderModeService));
            return base.DisposeAsync();
        }

        protected override Task<bool> Initialize(EditorServiceGame editorGame)
        {
            game = (EntityHierarchyEditorGame)editorGame;
            game.Script.AddTask(Update);

            return Task.FromResult(true);
        }

        public override void UpdateGraphicsCompositor(EditorServiceGame game)
        {
            base.UpdateGraphicsCompositor(game);

            // Make a copy of cameras (for game preview)
            savedCameras.Clear();
            savedCameras.AddRange(game.SceneSystem.GraphicsCompositor.Cameras);
            isPreviewMode = false; // saved camera means we are not yet in preview mode

            // Make sure it is null if nothing found
            materialFilterRenderFeature = null;

            // Meshes
            var meshRenderFeature = game.SceneSystem.GraphicsCompositor.RenderFeatures.OfType<MeshRenderFeature>().First();
            if (meshRenderFeature != null)
            {
                // Add material filtering
                meshRenderFeature.RenderFeatures.Add(materialFilterRenderFeature = new MaterialFilterRenderFeature());
            }
        }

        private async Task Update()
        {
            while (!IsDisposed)
            {
                await game.Script.NextFrame();

                if (!IsActive)
                    continue;

                var renderMode = ((IEditorGameRenderModeViewModelService)this).RenderMode;

                // Toggle graphics compositor to display scene using either editor or game graphics compositor
                var previewGameGraphicsCompositor = renderMode.PreviewGameGraphicsCompositor;
                var gameTopLevel = game.SceneSystem.GraphicsCompositor.Game as EditorTopLevelCompositor;
                if (gameTopLevel != null)
                {
                    // Enable preview game if requested
                    gameTopLevel.EnablePreviewGame = previewGameGraphicsCompositor;
                    // Disable Gizmo during preview game
                    game.EditorSceneSystem.GraphicsCompositor.Game.Enabled = !previewGameGraphicsCompositor;

                    if (gameTopLevel.EnablePreviewGame != isPreviewMode)
                    {
                        // Swap cameras collection content between game and savedCameras
                        var tempCameras = new List<SceneCameraSlot>(game.SceneSystem.GraphicsCompositor.Cameras);

                        game.SceneSystem.GraphicsCompositor.Cameras.Clear();
                        game.SceneSystem.GraphicsCompositor.Cameras.AddRange(savedCameras);

                        savedCameras.Clear();
                        savedCameras = tempCameras;

                        isPreviewMode = gameTopLevel.EnablePreviewGame;
                    }

                    // Setup material filter
                    materialFilterRenderFeature.MaterialFilter =
                        (materialFilterRenderFeature != null && renderMode.Mode == GameEditor.RenderMode.SingleStream)
                        ? renderMode.StreamDescriptor.Filter
                        : null;

                    // Disable mouse services while we are in game preview, and reenable them after
                    // TODO: A more robust mechanism for filtering or redirecting input?
                    if (previewGameGraphicsCompositor)
                    {
                        // Disable any mouse services that were enabled
                        foreach (var service in game.EditorServices.Services.OfType<IEditorGameMouseService>())
                        {
                            if (service.IsActive && disabledMouseServicesDuringGamePreview.Add(service))
                                service.IsActive = false;
                        }
                    }
                    else if (disabledMouseServicesDuringGamePreview.Count > 0)
                    {
                        // Need to restore some mouse services
                        foreach (var service in disabledMouseServicesDuringGamePreview)
                        {
                            service.IsActive = true;
                        }
                        disabledMouseServicesDuringGamePreview.Clear();
                    }
                }
            }
        }
    }
}
