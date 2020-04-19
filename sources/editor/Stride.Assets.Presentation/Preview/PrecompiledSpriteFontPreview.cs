// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Assets.Presentation.Preview.Views;
using Stride.Assets.SpriteFont;
using Stride.Editor.Preview;

namespace Stride.Assets.Presentation.Preview
{
    /// <summary>
    /// An implementation of the <see cref="AssetPreview"/> that can preview precompiled sprite fonts.
    /// </summary>
    [AssetPreview(typeof(PrecompiledSpriteFontAsset), typeof(SpriteFontPreviewView))]
    public class PrecompiledSpriteFontPreview : FontPreview<PrecompiledSpriteFontAsset>
    {
        protected override bool IsFontNotPremultiplied()
        {
            return !Asset.IsPremultiplied;
        }
    }
}
