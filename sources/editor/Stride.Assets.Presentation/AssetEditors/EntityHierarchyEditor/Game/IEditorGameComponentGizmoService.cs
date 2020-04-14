// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading.Tasks;
using Xenko.Editor.EditorGame.Game;
using Xenko.Engine;

namespace Xenko.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Game
{
    public interface IEditorGameComponentGizmoService : IEditorGameService
    {
        bool FixedSize { get; set; }

        float SceneUnit { get; }

        void UpdateGizmoEntitiesSelection(Entity entity, bool isSelected);

        Entity GetContentEntityUnderMouse();
    }
}
