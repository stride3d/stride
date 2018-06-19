// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Xenko.Assets.Presentation.AssetEditors.EntityHierarchyEditor.ViewModels;
using Xenko.Assets.Presentation.AssetEditors.GameEditor.Services;
using Xenko.Editor.EditorGame.ViewModels;

namespace Xenko.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Services
{
    public interface IEditorGameMaterialHighlightViewModelService : IEditorGameViewModelService
    {
        bool IsActive { get; set; }

        Tuple<Guid, int> GetTargetMeshIndex(EntityViewModel entity);
    }
}
