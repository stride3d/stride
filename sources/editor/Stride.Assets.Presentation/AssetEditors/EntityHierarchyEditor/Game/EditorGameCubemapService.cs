// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Services;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.ViewModels;
using Stride.Assets.Presentation.AssetEditors.GameEditor.Game;
using Stride.Editor.EditorGame.Game;
using Stride.Graphics;
using Stride.Rendering.Skyboxes;

namespace Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Game
{
    public class EditorGameCubemapService : EditorGameServiceBase, IEditorGameCubemapService
    {
        private readonly EntityHierarchyEditorViewModel editor;

        private EntityHierarchyEditorGame game;

        public EditorGameCubemapService(EntityHierarchyEditorViewModel editor)
        {
            this.editor = editor;
        }

        public override IEnumerable<Type> Dependencies { get { yield return typeof(IEditorGameCameraService); } }

        internal IEditorGameCameraService Camera => Services.Get<IEditorGameCameraService>();

        protected override Task<bool> Initialize(EditorServiceGame editorGame)
        {
            if (editorGame == null) throw new ArgumentNullException(nameof(editorGame));
            game = (EntityHierarchyEditorGame)editorGame;

            return Task.FromResult(true);
        }

        /// <inheritdoc/>
        public async Task<Image> CaptureCubemap()
        {
            return await editor.Controller.InvokeAsync(() =>
            {
                editor.ServiceProvider.TryGet<RenderDocManager>()?.StartFrameCapture(game.GraphicsDevice, IntPtr.Zero);

                var editorCompositor = game.EditorSceneSystem.GraphicsCompositor.Game;
                try
                {
                    // Disable Gizmo
                    game.EditorSceneSystem.GraphicsCompositor.Game = null;

                    var editorCameraPosition = Camera.Position;

                    // Capture cubemap
                    using (var cubemap = CubemapSceneRenderer.GenerateCubemap(game, editorCameraPosition, 1024))
                    {
                        return cubemap.GetDataAsImage(game.GraphicsContext.CommandList);
                    }
                }
                finally
                {
                    game.EditorSceneSystem.GraphicsCompositor.Game = editorCompositor;

                    editor.ServiceProvider.TryGet<RenderDocManager>()?.EndFrameCapture(game.GraphicsDevice, IntPtr.Zero);
                }
            });
        }
    }
}
