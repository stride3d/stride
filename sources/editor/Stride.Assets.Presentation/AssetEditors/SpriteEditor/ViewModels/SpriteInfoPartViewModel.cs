// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xenko.Core.Extensions;
using Xenko.Core.Presentation.Services;
using Xenko.Core.Presentation.ViewModel;

namespace Xenko.Assets.Presentation.AssetEditors.SpriteEditor.ViewModels
{
    public class SpriteInfoPartViewModel : DispatcherViewModel
    {
        protected readonly SpriteInfoViewModel Sprite;

        public SpriteInfoPartViewModel(SpriteInfoViewModel sprite)
            : base(sprite.SafeArgument(nameof(sprite)).ServiceProvider)
        {
            Sprite = sprite;
        }

        /// <summary>
        /// Gets the undo/redo service used by this view model.
        /// </summary>
        protected IUndoRedoService UndoRedoService => ServiceProvider.Get<IUndoRedoService>();
    }
}
