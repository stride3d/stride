// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets;
using Stride.Core;
using Stride.Physics;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Stride.Core.Annotations;
using Stride.Core.Yaml;
using Stride.Core.Yaml.Serialization;

namespace Stride.Assets.Physics
{
    [DataContract("ColliderShapeAsset")]
    [AssetDescription(FileExtension)]
    [AssetContentType(typeof(PhysicsColliderShape))]
    [AssetFormatVersion(StrideConfig.PackageName, CurrentVersion, "2.0.0.0")]
    [AssetUpgrader(StrideConfig.PackageName, "2.0.0.0", "3.0.0.0", typeof(ConvexHullDecompositionParametersUpgrader))]
    public partial class ColliderShapeAsset : Asset
    {
        private const string CurrentVersion = "3.0.0.0";

        public const string FileExtension = ".sdphy;pdxphy";

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
                        dynamic decomposition = shape.Decomposition = new DynamicYamlMapping(new YamlMappingNode());

                        if (shape.ContainsChild("SimpleWrap"))
                        {
                            decomposition.Enabled = shape.SimpleWrap.ToString().ToLowerInvariant().Equals("false");
                        }
                        if (shape.ContainsChild("Depth"))
                        {
                            decomposition.Depth = shape.Depth;
                        }
                        if (shape.ContainsChild("PosSampling"))
                        {
                            decomposition.PosSampling = shape.PosSampling;
                        }
                        if (shape.ContainsChild("AngleSampling"))
                        {
                            decomposition.AngleSampling = shape.AngleSampling;
                        }
                        if (shape.ContainsChild("PosRefine"))
                        {
                            decomposition.PosRefine = shape.PosRefine;
                        }
                        if (shape.ContainsChild("AngleRefine"))
                        {
                            decomposition.AngleRefine = shape.AngleRefine;
                        }
                        if (shape.ContainsChild("Alpha"))
                        {
                            decomposition.Alpha = shape.Alpha;
                        }
                        if (shape.ContainsChild("Threshold"))
                        {
                            decomposition.Threshold = shape.Threshold;
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
