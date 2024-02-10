using Stride.Core.Assets;

namespace Stride.Assets.BepuPhysics
{
    internal class HullAssetFactory : AssetFactory<HullAsset>
    {
        public static HullAsset Create()
        {
            return new HullAsset();
        }

        public override HullAsset New()
        {
            return Create();
        }
    }
}
