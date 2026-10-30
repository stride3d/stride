// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Assets.Editor.ViewModels.SpriteEditor;

/// <summary>
/// The tool of the sprite sheet editor that a click or a drag on the image uses.
/// </summary>
public enum SpriteSheetEditorToolMode
{
    /// <summary>
    /// The handles move and resize the region of the sprite.
    /// </summary>
    None,

    /// <summary>
    /// A click on the image picks the color key of the sprite sheet.
    /// </summary>
    ColorPick,

    /// <summary>
    /// A click on the image sets the region of the sprite to the sprite under the mouse (Ctrl adds it to the region).
    /// </summary>
    MagicWand,

    /// <summary>
    /// A handle moves the center of the sprite (2D sprite sheets).
    /// </summary>
    SpriteCenter,

    /// <summary>
    /// The handles resize the borders of the sprite (UI sprite sheets).
    /// </summary>
    Borders,
}
