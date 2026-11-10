// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#nullable disable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Stride.Assets.Editor.ViewModels;
using Stride.Editor.EditorGame.Game;
using Stride.Graphics;
using Stride.Rendering.Skyboxes;
using Stride.Assets.Editor.AssetEditors.GameEditor.Game;
using Stride.Assets.Editor.AssetEditors.EntityHierarchyEditor.Game;
using Stride.Assets.Editor.AssetEditors.EntityHierarchyEditor;
using Stride.Assets.Editor.AssetEditors.EntityHierarchyEditor.Services;
using Stride.Assets.Editor.AssetEditors.GameEditor.Services;
using Stride.Assets.Editor.Services;

namespace Stride.Assets.Editor.EditorGame
{
    [EditorGameService(typeof(EntityHierarchyEditorController), Order = 160)]
    public sealed class EditorGameCubemapService : EditorGameServiceBase, IEditorGameCubemapService
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
                // A frame debugger capture of the cubemap, if the editor has one (RenderDoc in the WPF Game Studio)
                var frameCapture = editor.ServiceProvider.TryGet<IGraphicsFrameCaptureService>();
                frameCapture?.StartFrameCapture(game.GraphicsDevice);

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

                    frameCapture?.EndFrameCapture(game.GraphicsDevice);
                }
            });
        }
    }
}
