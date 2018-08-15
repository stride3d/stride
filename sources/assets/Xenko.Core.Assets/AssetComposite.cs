// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Xenko.Core;
using Xenko.Core.Yaml.Serialization;

namespace Xenko.Core.Assets
{
    /// <summary>
    /// Base class for an asset that supports inheritance by composition.
    /// </summary>
    public abstract class AssetComposite : Asset, IAssetComposite
    {
        [Obsolete("The AssetPart struct might be removed soon")]
        public abstract IEnumerable<AssetPart> CollectParts();

        public abstract IIdentifiable FindPart(Guid partId);

        public abstract bool ContainsPart(Guid id);

        public class FixPartReferenceUpgrader : AssetUpgraderBase
        {
            protected override void UpgradeAsset(AssetMigrationContext context, PackageVersion currentVersion, PackageVersion targetVersion, dynamic asset, PackageLoadingAssetFile assetFile, OverrideUpgraderHint overrideHint)
            {
                var rootNode = (YamlNode)asset.Node;

                var allScalarNodes = rootNode.AllNodes.OfType<YamlScalarNode>().ToList();

                var nextIsId = false;
                var inPublicUIElements = false;
                foreach (var node in allScalarNodes)
                {
                    var indexFirstSlash = node.Value.IndexOf('/');
                    Guid targetGuid = Guid.Empty;
                    if (indexFirstSlash == -1)
                    {
                        Guid.TryParseExact(node.Value, "D", out targetGuid);
                    }
                    else
                    {
                        Guid entityGuid;
                        if (Guid.TryParseExact(node.Value.Substring(0, indexFirstSlash), "D", out entityGuid))
                        {
                            Guid.TryParseExact(node.Value.Substring(indexFirstSlash + 1), "D", out targetGuid);
                        }
                    }

                    if (targetGuid != Guid.Empty && !nextIsId && !inPublicUIElements)
                    {
                        node.Value = "ref!! " + targetGuid;
                    }
                    else
                    {
                        if (nextIsId && targetGuid == Guid.Empty)
                            nextIsId = false;

                        if (inPublicUIElements && node.Value == "Hierarchy")
                            inPublicUIElements = false;

                        if (node.Value.Contains("Id"))
                            nextIsId = true;

                        if (node.Value == "PublicUIElements")
                            inPublicUIElements = true;
                    }
                }
            }
        }
    }
}
