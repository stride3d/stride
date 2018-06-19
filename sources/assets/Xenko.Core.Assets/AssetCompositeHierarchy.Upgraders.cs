// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xenko.Core;
using Xenko.Core.Yaml;

namespace Xenko.Core.Assets
{
    partial class AssetCompositeHierarchy<TAssetPartDesign, TAssetPart>
    {
        protected class RootPartIdsToRootPartsUpgrader : AssetUpgraderBase
        {
            protected override void UpgradeAsset(AssetMigrationContext context, PackageVersion currentVersion, PackageVersion targetVersion, dynamic asset, PackageLoadingAssetFile assetFile, OverrideUpgraderHint overrideHint)
            {
                var rootPartIds = asset.Hierarchy.RootPartIds;
                int i = 0;
                foreach (dynamic rootPartId in rootPartIds)
                {
                    rootPartIds[i++] = "ref!! " + rootPartId.ToString();
                }
                asset.Hierarchy.RootParts = rootPartIds;
                asset.Hierarchy.RootPartIds = DynamicYamlEmpty.Default;
            }
        }
    }
}
