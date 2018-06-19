// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using Xenko.Core.Assets.Editor.ViewModel;
using Xenko.Editor.EditorGame.ViewModels;

namespace Xenko.Assets.Presentation.AssetEditors.GameEditor.Services
{
    public interface IEditorGameAssetHighlighterViewModelService : IEditorGameViewModelService
    {
        void HighlightAssets(IEnumerable<AssetViewModel> assets);
    }
}
