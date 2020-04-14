// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Assets;
using Stride.Graphics;

namespace Stride.Assets.SpriteFont
{
    public class OfflineRasterizedSpriteFontFactory : AssetFactory<SpriteFontAsset>
    {
        public static SpriteFontAsset Create()
        {
            return new SpriteFontAsset
            {
                FontSource = new SystemFontProvider("Arial"),
                FontType = new OfflineRasterizedSpriteFontType()
                {
                    CharacterRegions = { new CharacterRegion(' ', (char)127) }                 
                },
            };
        }

        public override SpriteFontAsset New()
        {
            return Create();
        }
    }

    public class RuntimeRasterizedSpriteFontFactory : AssetFactory<SpriteFontAsset>
    {
        public static SpriteFontAsset Create()
        {
            return new SpriteFontAsset
            {
                FontSource = new SystemFontProvider("Arial"),
                FontType = new RuntimeRasterizedSpriteFontType(),
            };
        }

        public override SpriteFontAsset New()
        {
            return Create();
        }
    }

    public class SignedDistanceFieldSpriteFontFactory: AssetFactory<SpriteFontAsset>
    {
        public static SpriteFontAsset Create()
        {
            return new SpriteFontAsset
            {
                FontSource = new SystemFontProvider("Arial"),
                FontType = new SignedDistanceFieldSpriteFontType()
                {
                    CharacterRegions = { new CharacterRegion(' ', (char)127) }
                },
            };
        }

        public override SpriteFontAsset New()
        {
            return Create();
        }
    }
}
