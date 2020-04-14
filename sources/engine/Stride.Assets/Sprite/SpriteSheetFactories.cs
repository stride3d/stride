// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Assets;

namespace Stride.Assets.Sprite
{
    public class SpriteSheetSprite2DFactory : AssetFactory<SpriteSheetAsset>
    {
        public static SpriteSheetAsset Create()
        {
            return new SpriteSheetAsset
            {
                Type = SpriteSheetType.Sprite2D,
            };
        }

        public override SpriteSheetAsset New()
        {
            return Create();
        }
    }

    public class SpriteSheetUIFactory : AssetFactory<SpriteSheetAsset>
    {
        public static SpriteSheetAsset Create()
        {
            return new SpriteSheetAsset
            {
                Type = SpriteSheetType.UI,
            };
        }

        public override SpriteSheetAsset New()
        {
            return Create();
        }
    }

}
