// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Windows;
using Stride.Editor.Annotations;
using Stride.Editor.Preview.View;

namespace Stride.Assets.Presentation.Preview.Views
{
    /// <summary>
    /// The view of any <see cref="PreviewFromSpriteBatch{T}"/> that declares no view of its own: zoom buttons and the
    /// sprite size.
    /// </summary>
    [AssetPreviewView(typeof(PreviewFromSpriteBatch<>))]
    public class SpritePreviewView : StridePreviewView
    {
        static SpritePreviewView()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(SpritePreviewView), new FrameworkPropertyMetadata(typeof(SpritePreviewView)));
        }
    }
}
