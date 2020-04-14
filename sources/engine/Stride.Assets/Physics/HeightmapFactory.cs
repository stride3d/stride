// Copyright (c) Xenko contributors (https://xenko.com)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xenko.Core.Assets;

namespace Xenko.Assets.Physics
{
    public class HeightmapFactory : AssetFactory<HeightmapAsset>
    {
        public static HeightmapAsset Create()
        {
            return new HeightmapAsset();
        }

        public override HeightmapAsset New()
        {
            return Create();
        }
    }
}
