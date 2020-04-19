// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Editor.EditorGame.ViewModels;

namespace Stride.Assets.Presentation.AssetEditors.GameEditor.Services
{
    public interface IEditorGameAssetHighlighterViewModelService : IEditorGameViewModelService
    {
        void HighlightAssets(IEnumerable<AssetViewModel> assets);
    }
}
