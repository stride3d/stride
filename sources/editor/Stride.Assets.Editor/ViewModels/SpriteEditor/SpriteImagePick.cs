// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Mathematics;

namespace Stride.Assets.Editor.ViewModels.SpriteEditor;

/// <summary>
/// A click of the pixel tools (magic wand, color key picker) on the image of the sprite sheet editor.
/// </summary>
/// <param name="Position">The pixel under the mouse, in pixels of the image.</param>
/// <param name="AddToRegion">Whether the magic wand adds the sprite to the current region (Ctrl) rather than replacing it.</param>
public readonly record struct SpriteImagePick(Vector2 Position, bool AddToRegion);
