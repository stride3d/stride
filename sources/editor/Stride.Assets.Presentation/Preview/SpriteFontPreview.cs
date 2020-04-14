// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xenko.Assets.Presentation.Preview.Views;
using Xenko.Assets.SpriteFont;
using Xenko.Editor.Preview;

namespace Xenko.Assets.Presentation.Preview
{
    /// <summary>
    /// An implementation of the <see cref="AssetPreview"/> that can preview sprite fonts.
    /// </summary>
    [AssetPreview(typeof(SpriteFontAsset), typeof(SpriteFontPreviewView))]
    public class SpriteFontPreview : FontPreview<SpriteFontAsset>
    {
        protected override bool IsFontNotPremultiplied()
        {
            return !Asset.FontType.IsPremultiplied;
        }
    }
}
