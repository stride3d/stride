// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading.Tasks;
using Xenko.Core.Serialization.Contents;
using Xenko.Assets.Presentation.AssetEditors.GameEditor.Services;
using Xenko.Assets.Presentation.SceneEditor;
using Xenko.Editor.EditorGame.Game;

namespace Xenko.Assets.Presentation.AssetEditors.GameEditor.Game
{
    /// <summary>
    /// A class that provides access to debug information of an editor game.
    /// </summary>
    public class EditorGameDebugService : EditorGameServiceBase, IEditorGameDebugViewModelService
    {
        private Engine.Game game;

        /// <summary>
        /// Gets the stats of the scene editor asset manager.
        /// </summary>
        public ContentManagerStats ContentManagerStats => game.Content.GetStats();

        /// <inheritdoc/>
        protected override Task<bool> Initialize(EditorServiceGame editorGame)
        {
            game = editorGame;
            return Task.FromResult(true);
        }
    }
}
