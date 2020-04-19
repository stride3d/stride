// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Extensions;
using Stride.Assets.Presentation.ViewModel.Commands;
using Stride.Assets.Sprite;
using Stride.Core.Presentation.Quantum;
using Stride.Core.Presentation.Quantum.Presenters;
using Stride.Assets.Presentation.AssetEditors.SpriteEditor.ViewModels;
using Stride.Assets.Presentation.NodePresenters.Commands;

namespace Stride.Assets.Presentation.ViewModel
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
