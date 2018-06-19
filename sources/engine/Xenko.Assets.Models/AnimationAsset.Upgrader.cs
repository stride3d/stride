// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xenko.Core.Assets;
using Xenko.Core;

namespace Xenko.Assets.Models
{
    public class AnimationAssetUpgraderFramerate : AssetUpgraderBase
    {
        /// <inheritdoc/>
        protected override void UpgradeAsset(AssetMigrationContext context, PackageVersion currentVersion, PackageVersion targetVersion, dynamic asset, PackageLoadingAssetFile assetFile, OverrideUpgraderHint overrideHint)
        {
            asset.AnimationTimeMinimum = "0:00:00:00.0000000";
            asset.AnimationTimeMaximum = "0:00:30:00.0000000";  // Tentatively set this to 30 minutes
        }
    }
}
