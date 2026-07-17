// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets;
using Stride.Core;
using Stride.Core.Diagnostics;
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
    [AssetFormatVersion(StrideConfig.LogicalPackageName, CurrentVersion, "2.0.0.0")]
    [AssetUpgrader(StrideConfig.LogicalPackageName, "2.0.0.0", "3.0.0.0", typeof(ConvexHullDecompositionParametersUpgrader))]
    [AssetUpgrader(StrideConfig.LogicalPackageName, "3.0.0.0", "4.0.0.0", typeof(VhacdV4Upgrader))]
    public partial class ColliderShapeAsset : Asset
    {
        private const string CurrentVersion = "4.0.0.0";

        public const string FileExtension = ".sdphy";

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

        // V-HACD v1 (HACD clustering) -> V-HACD v4 (voxel/Voronoi). Algorithm change,
        // not just rename: most v1 knobs have no v4 equivalent. Map what carries over;
        // drop the rest. Output WILL differ even with these defaults — rebake expected.
        private class VhacdV4Upgrader : AssetUpgraderBase
        {
            protected override void UpgradeAsset(AssetMigrationContext context, PackageVersion currentVersion, PackageVersion targetVersion, dynamic asset, PackageLoadingAssetFile assetFile, OverrideUpgraderHint overrideHint)
            {
                bool warned = false;
                foreach (dynamic item in asset.ColliderShapes)
                {
                    dynamic shape = item.Value;
                    if (shape.Node.Tag != "!ConvexHullColliderShapeDesc")
                        continue;
                    if (!shape.ContainsChild("Decomposition"))
                        continue;

                    dynamic decomp = shape.Decomposition;
                    bool migrated = false;

                    // Depth -> MaxRecursionDepth (both bounded recursion 1..32, direct).
                    if (decomp.ContainsChild("Depth"))
                    {
                        decomp.MaxRecursionDepth = decomp.Depth;
                        decomp.RemoveChild("Depth");
                        migrated = true;
                    }

                    // Threshold -> MinimumVolumePercentErrorAllowed.
                    // v1 default 0.01 (ratio); v4 default 1.0 (percent). Multiply by 100
                    // for unit consistency — this only matters if the user changed it.
                    if (decomp.ContainsChild("Threshold"))
                    {
                        if (float.TryParse((string)decomp.Threshold, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var t))
                            decomp.MinimumVolumePercentErrorAllowed = t * 100.0;
                        decomp.RemoveChild("Threshold");
                        migrated = true;
                    }

                    // Unmappable in v4 — drop. ShrinkWrap / FillMode / MaxConvexHulls /
                    // Resolution / MaxNumVerticesPerConvexHull get their v4 defaults.
                    foreach (var name in new[] { "PosSampling", "AngleSampling", "PosRefine", "AngleRefine", "Alpha" })
                    {
                        if (decomp.ContainsChild(name))
                        {
                            decomp.RemoveChild(name);
                            migrated = true;
                        }
                    }

                    if (migrated && !warned)
                    {
                        context.Log.Warning($"Migrated convex hull decomposition parameters in '{assetFile.OriginalFilePath}' from V-HACD v1 to V-HACD v4. Decomposition output may differ; re-tune if needed.");
                        warned = true;
                    }
                }
            }
        }
    }
}
