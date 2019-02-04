// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xenko.Core.Assets;
using Xenko.Core;
using Xenko.Physics;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Xenko.Core.Annotations;
using Xenko.Core.Yaml;
using Xenko.Core.Yaml.Serialization;

namespace Xenko.Assets.Physics
{
    [DataContract("ColliderShapeAsset")]
    [AssetDescription(FileExtension)]
    [AssetContentType(typeof(PhysicsColliderShape))]
#if XENKO_SUPPORT_BETA_UPGRADE
    [AssetFormatVersion(XenkoConfig.PackageName, CurrentVersion, "1.4.0-beta")]
    [AssetUpgrader(XenkoConfig.PackageName, "1.4.0-beta", "2.0.0.0", typeof(EmptyAssetUpgrader))]
    [AssetUpgrader(XenkoConfig.PackageName, "2.0.0.0", "3.0.0.0", typeof(ConvexHullDecompositionParametersUpgrader))]
#else
    [AssetFormatVersion(XenkoConfig.PackageName, CurrentVersion, "2.0.0.0")]
    [AssetUpgrader(XenkoConfig.PackageName, "2.0.0.0", "3.0.0.0", typeof(ConvexHullGeneratorUpgrader))]
#endif
    public partial class ColliderShapeAsset : Asset
    {
        private const string CurrentVersion = "3.0.0.0";

        public const string FileExtension = ".xkphy;pdxphy";

        /// <userdoc>
        /// The collection of shapes in this asset, a collection shapes will automatically generate a compound shape.
        /// </userdoc>
        [DataMember(10)]
        [Category]
        [MemberCollection(CanReorderItems = true, NotNullItems = true)]
        public List<IAssetColliderShapeDesc> ColliderShapes { get; } = new List<IAssetColliderShapeDesc>();

        private class ConvexHullDecompositionParametersUpgrader : AssetUpgraderBase
        {
            protected override void UpgradeAsset(AssetMigrationContext context, PackageVersion currentVersion, PackageVersion targetVersion, dynamic asset, PackageLoadingAssetFile assetFile, OverrideUpgraderHint overrideHint)
            {
                foreach (dynamic item in asset.ColliderShapes)
                {
                    dynamic shape = item.Value;
                    if (shape.Node.Tag == "!ConvexHullColliderShapeDesc")
                    {
                        dynamic decompositionParameters = shape.DecompositionParameters = new DynamicYamlMapping(new YamlMappingNode());

                        if (shape.ContainsChild("SimpleWrap"))
                        {
                            decompositionParameters.Enabled = shape.SimpleWrap.ToString().ToLowerInvariant().Equals("false");
                        }
                        if (shape.ContainsChild("Depth"))
                        {
                            decompositionParameters.Depth = shape.Depth;
                        }
                        if (shape.ContainsChild("PosSampling"))
                        {
                            decompositionParameters.PosSampling = shape.PosSampling;
                        }
                        if (shape.ContainsChild("AngleSampling"))
                        {
                            decompositionParameters.AngleSampling = shape.AngleSampling;
                        }
                        if (shape.ContainsChild("PosRefine"))
                        {
                            decompositionParameters.PosRefine = shape.PosRefine;
                        }
                        if (shape.ContainsChild("AngleRefine"))
                        {
                            decompositionParameters.AngleRefine = shape.AngleRefine;
                        }
                        if (shape.ContainsChild("Alpha"))
                        {
                            decompositionParameters.Alpha = shape.Alpha;
                        }
                        if (shape.ContainsChild("Threshold"))
                        {
                            decompositionParameters.Threshold = shape.Threshold;
                        }

                        shape.RemoveChild("SimpleWrap");
                        shape.RemoveChild("Depth");
                        shape.RemoveChild("PosSampling");
                        shape.RemoveChild("AngleSampling");
                        shape.RemoveChild("PosRefine");
                        shape.RemoveChild("AngleRefine");
                        shape.RemoveChild("Alpha");
                        shape.RemoveChild("Threshold");
                    }
                }
            }
        }
    }
}
