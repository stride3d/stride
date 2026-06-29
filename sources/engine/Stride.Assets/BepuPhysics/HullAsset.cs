// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Core.Assets;
using Stride.Core.Diagnostics;
using Stride.BepuPhysics.Definitions;
using System.Collections.Generic;
using Stride.Core.Annotations;
using Stride.Rendering;
using Stride.Core.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using Stride.Assets;
using Stride.Core.Yaml;
using Stride.Core.Yaml.Serialization;

namespace Stride.BepuPhysics.Assets
{
    [Display((int)AssetDisplayPriority.Physics, "Hull")]
    [DataContract("HullAsset")]
    [AssetDescription(FileExtension)]
    [AssetContentType(typeof(DecomposedHulls))]
    [AssetFormatVersion(StrideConfig.LogicalPackageName, CurrentVersion, "2.0.0.0")]
    [AssetUpgrader(StrideConfig.LogicalPackageName, "2.0.0.0", "3.0.0.0", typeof(VhacdV4Upgrader))]
    public class HullAsset : Asset
    {
        private const string CurrentVersion = "3.0.0.0";

        public const string FileExtension = ".sdhull";

        /// <summary>
        /// Multiple meshes -> Multiple Hulls per mesh
        /// </summary>
        [Display(Browsable = false)]
        [DataMember(10)]
        public List<List<DecomposedHulls.Hull>> ConvexHulls = null;

        /// <userdoc>
        /// Model asset from where the engine will derive the convex hull.
        /// </userdoc>
        [DataMember(30)]
        public Model Model; // Do note that this field is also assigned through reflection in HullAssetFactoryTemplateGenerator as a workaround

        /// <userdoc>
        /// The offset with the real graphic mesh.
        /// </userdoc>
        [DataMember(31)]
        public Vector3 LocalOffset;

        /// <userdoc>
        /// The local rotation of the collider shape.
        /// </userdoc>
        [DataMember(32)]
        public Quaternion LocalRotation = Quaternion.Identity;

        /// <userdoc>
        /// The scaling of the generated convex hull.
        /// </userdoc>
        [DataMember(45)]
        public Vector3 Scaling = Vector3.One;

        /// <summary>
        /// Parameters used when decomposing the given <see cref="Model"/> into a hull
        /// </summary>
        [DataMember(50)]
        [NotNull]
        public ConvexHullDecompositionParameters Decomposition { get; set; } = new ConvexHullDecompositionParameters();

        // V-HACD v1 (HACD clustering) -> V-HACD v4 (voxel/Voronoi). Algorithm change,
        // not just rename: most v1 knobs have no v4 equivalent. Map what carries over;
        // drop the rest. Output WILL differ even with these defaults — rebake expected.
        private class VhacdV4Upgrader : AssetUpgraderBase
        {
            protected override void UpgradeAsset(AssetMigrationContext context, PackageVersion currentVersion, PackageVersion targetVersion, dynamic asset, PackageLoadingAssetFile assetFile, OverrideUpgraderHint overrideHint)
            {
                if (!asset.ContainsChild("Decomposition"))
                    return;

                dynamic decomp = asset.Decomposition;
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

                foreach (var name in new[] { "PosSampling", "AngleSampling", "PosRefine", "AngleRefine", "Alpha" })
                {
                    if (decomp.ContainsChild(name))
                    {
                        decomp.RemoveChild(name);
                        migrated = true;
                    }
                }

                if (migrated)
                {
                    context.Log.Warning($"Migrated convex hull decomposition parameters in '{assetFile.OriginalFilePath}' from V-HACD v1 to V-HACD v4. Decomposition output may differ; re-tune if needed.");
                }
            }
        }
    }
}
