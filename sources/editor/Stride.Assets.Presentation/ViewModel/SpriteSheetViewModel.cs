// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using Xenko.Core.Assets.Editor.ViewModel;
using Xenko.Core.Extensions;
using Xenko.Assets.Presentation.ViewModel.Commands;
using Xenko.Assets.Sprite;
using Xenko.Core.Presentation.Quantum;
using Xenko.Core.Presentation.Quantum.Presenters;
using Xenko.Assets.Presentation.AssetEditors.SpriteEditor.ViewModels;
using Xenko.Assets.Presentation.NodePresenters.Commands;

namespace Xenko.Assets.Presentation.ViewModel
{
    [AssetViewModel(typeof(SpriteSheetAsset))]
    public class SpriteSheetViewModel : AssetViewModel<SpriteSheetAsset>
    {
        internal new SpriteSheetEditorViewModel Editor => (SpriteSheetEditorViewModel)base.Editor;

        public SpriteSheetViewModel(AssetViewModelConstructionParameters parameters)
            : base(parameters)
        {
        }
    }
}
