// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Editor.Preview;
using Stride.Assets.Presentation.Preview;
using Stride.Editor.Annotations;
using Stride.Editor.Avalonia.Preview.Views;

namespace Stride.Assets.Editor.Avalonia.Views.Preview;

/// <summary>
/// The view of any <see cref="PreviewFromSpriteBatch{T}"/> that declares no view of its own.
/// </summary>
[AssetPreviewView(typeof(PreviewFromSpriteBatch<>))]
public class SpritePreviewView : StridePreviewView
{
    /// <inheritdoc />
    protected override Type StyleKeyOverride => typeof(SpritePreviewView);
}
