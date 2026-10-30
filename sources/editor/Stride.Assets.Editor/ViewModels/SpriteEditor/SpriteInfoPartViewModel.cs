// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Assets.Editor.ViewModels.SpriteEditor;

/// <summary>
/// A part of a sprite of the sprite sheet editor (its region, borders or center).
/// </summary>
public abstract class SpriteInfoPartViewModel : DispatcherViewModel
{
    protected SpriteInfoPartViewModel(SpriteInfoViewModel sprite)
        : base(sprite.ServiceProvider)
    {
        Sprite = sprite;
    }

    /// <summary>
    /// Gets the sprite this part belongs to.
    /// </summary>
    protected SpriteInfoViewModel Sprite { get; }

    /// <summary>
    /// Gets the undo/redo service used by this view model.
    /// </summary>
    protected IUndoRedoService UndoRedoService => ServiceProvider.Get<IUndoRedoService>();
}
