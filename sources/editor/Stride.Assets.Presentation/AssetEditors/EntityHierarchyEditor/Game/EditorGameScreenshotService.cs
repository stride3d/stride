// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.IO;
using System.Threading.Tasks;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Services;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.ViewModels;
using Stride.Assets.Presentation.AssetEditors.GameEditor.Game;
using Stride.Editor.EditorGame.Game;
using Stride.Graphics;

namespace Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Game
{
    public class EditorGameScreenshotService : EditorGameServiceBase, IEditorGameScreenshotService
    {
        private readonly EntityHierarchyEditorViewModel editor;

        private EntityHierarchyEditorGame game;

        public EditorGameScreenshotService(EntityHierarchyEditorViewModel editor)
        {
            this.editor = editor;
        }

        protected override Task<bool> Initialize(EditorServiceGame editorGame)
        {
            if (editorGame == null) throw new ArgumentNullException(nameof(editorGame));
            game = (EntityHierarchyEditorGame)editorGame;

            return Task.FromResult(true);
        }

        /// <inheritdoc/>
        public async Task<byte[]> CaptureViewportAsync()
        {
            return await editor.Controller.InvokeAsync(() =>
            {
                var presenter = game.GraphicsDevice.Presenter;
                if (presenter?.BackBuffer == null)
                    throw new InvalidOperationException("Graphics presenter or back buffer is not available.");

                using var stream = new MemoryStream();
                presenter.BackBuffer.Save(game.GraphicsContext.CommandList, stream, ImageFileType.Png);
                return stream.ToArray();
            });
        }
    }
}
