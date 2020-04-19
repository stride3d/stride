// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core;
using Stride.Core.Yaml;
using Stride.Core.Yaml.Serialization;

namespace Stride.Core.Assets
{
    public abstract class AssetUpgraderBase : IAssetUpgrader
    {
        public void Upgrade(AssetMigrationContext context, string dependencyName, PackageVersion currentVersion, PackageVersion targetVersion, YamlMappingNode yamlAssetNode, PackageLoadingAssetFile assetFile)
        {
            dynamic asset = new DynamicYamlMapping(yamlAssetNode);

            // upgrade the asset
            var baseBranch = asset["~Base"];
            var basePartsBranch = asset["~BaseParts"] as DynamicYamlArray;

            // Detect in what kind of override context we are
            var overrideHint = (baseBranch != null || (basePartsBranch != null && basePartsBranch.Node.Children.Count > 0))
                ? OverrideUpgraderHint.Derived
                : OverrideUpgraderHint.Unknown;

            // Upgrade the asset
            UpgradeAsset(context, currentVersion, targetVersion, asset, assetFile, overrideHint);
            SetSerializableVersion(asset, dependencyName, targetVersion);

            // Upgrade its base
            if (baseBranch != null)
            {
                UpgradeBase(context, dependencyName, currentVersion, targetVersion, baseBranch, assetFile);
            }

            // Upgrade base parts
            if (basePartsBranch != null)
            {
                foreach (dynamic assetBase in basePartsBranch)
                {
                    UpgradeBase(context, dependencyName, currentVersion, targetVersion, assetBase, assetFile);
                }
            }
        }

        private void UpgradeBase(AssetMigrationContext context, string dependencyName, PackageVersion currentVersion, PackageVersion targetVersion, dynamic assetBase, PackageLoadingAssetFile assetFile)
        {
            var baseAsset = assetBase["Asset"];
            if (baseAsset != null)
            {
                UpgradeAsset(context, currentVersion, targetVersion, baseAsset, assetFile, OverrideUpgraderHint.Base);
                SetSerializableVersion(baseAsset, dependencyName, targetVersion);
            }
        }

        protected abstract void UpgradeAsset(AssetMigrationContext context, PackageVersion currentVersion, PackageVersion targetVersion, dynamic asset, PackageLoadingAssetFile assetFile, OverrideUpgraderHint overrideHint);

        public static void SetSerializableVersion(dynamic asset, string dependencyName, PackageVersion value)
        {
            if (asset.IndexOf(nameof(Asset.SerializedVersion)) == -1)
            {
                asset.SerializedVersion = new YamlMappingNode();

                // Ensure that it is stored right after the asset Id
                asset.MoveChild(nameof(Asset.SerializedVersion), asset.IndexOf(nameof(Asset.Id)) + 1);
            }

            asset.SerializedVersion[dependencyName] = value;
        }
    }
}
