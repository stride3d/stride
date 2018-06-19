// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xenko.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Services;
using Xenko.Assets.Presentation.AssetEditors.EntityHierarchyEditor.ViewModels;
using Xenko.Assets.Presentation.AssetEditors.GameEditor.Game;
using Xenko.Editor.EditorGame.Game;
using Xenko.Graphics;
using Xenko.Rendering.Skyboxes;

namespace Xenko.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Game
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
                editor.ServiceProvider.TryGet<RenderDocManager>()?.StartCapture(game.GraphicsDevice, IntPtr.Zero);

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
