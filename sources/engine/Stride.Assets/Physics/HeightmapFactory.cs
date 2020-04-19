// Copyright (c) Stride contributors (https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Assets;

namespace Stride.Assets.Physics
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
