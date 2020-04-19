// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Extensions;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModel;

namespace Stride.Assets.Presentation.AssetEditors.SpriteEditor.ViewModels
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
