// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Threading.Tasks;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Game;
using Stride.Assets.Presentation.AssetEditors.Gizmos;
using Stride.Editor.EditorGame.Game;

namespace Stride.Assets.Presentation.AssetEditors.GameEditor.Game
{
    public class EditorGameSpaceMarkerService : EditorGameServiceBase
    {
        private EntityHierarchyEditorGame game;
        private SpaceMarker spaceMarker;

        protected override Task<bool> Initialize(EditorServiceGame editorGame)
        {
            game = (EntityHierarchyEditorGame)editorGame;
            spaceMarker = new SpaceMarker(game);
            spaceMarker.Initialize(game.Services, game.EditorScene);
            game.Script.AddTask(Update);
            return Task.FromResult(true);
        }

        private async Task Update()
        {
            // update all gizmo of the scene.
            while (!IsDisposed)
            {
                spaceMarker.Update();
                await game.Script.NextFrame();
            }
        }
    }
}
