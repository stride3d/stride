// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Xenko.Core.Assets;
using Xenko.Core.Assets.Compiler;
using Xenko.Core;
using Xenko.Core.Annotations;
using Xenko.Core.Yaml;
using Xenko.Core.Yaml.Serialization;
using Xenko.Rendering;
using Xenko.Rendering.ProceduralModels;

namespace Xenko.Assets.Models
{
    /// <summary>
    /// The geometric primitive asset.
    /// </summary>
    [DataContract("ProceduralModelAsset")]
    [AssetDescription(FileExtension)]
    [AssetContentType(typeof(Model))]
    [Display((int)AssetDisplayPriority.Models + 40, "Procedural model")]
#if XENKO_SUPPORT_BETA_UPGRADE
    [AssetFormatVersion(XenkoConfig.PackageName, CurrentVersion, "1.9.0-beta01")]
    [AssetUpgrader(XenkoConfig.PackageName, "1.9.0-beta01", "2.0.0.0", typeof(EmptyAssetUpgrader))]
#else
    [AssetFormatVersion(XenkoConfig.PackageName, CurrentVersion, "2.0.0.0")]
#endif
    public sealed class ProceduralModelAsset : Asset, IModelAsset
    {
        private const string CurrentVersion = "2.0.0.0";

        /// <summary>
        /// The default file extension used by the <see cref="ProceduralModelAsset"/>.
        /// </summary>
        public const string FileExtension = ".xkpromodel";

        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        /// <value>The type.</value>
        /// <userdoc>The type of procedural model to generate</userdoc>
        [DataMember(10)]
        [NotNull]
        [Display("Type", Expand = ExpandRule.Always)]
        public IProceduralModel Type { get; set; }

        /// <inheritdoc/>
        [DataMemberIgnore]
        public List<ModelMaterial> Materials => Type.MaterialInstances.Select(x => new ModelMaterial { Name = x.Key, MaterialInstance = x.Value }).ToList();

        private class Upgrader : AssetUpgraderBase
        {
            protected override void UpgradeAsset(AssetMigrationContext context, PackageVersion currentVersion, PackageVersion targetVersion, dynamic asset, PackageLoadingAssetFile assetFile, OverrideUpgraderHint overrideHint)
            {
                // Introduction of MaterialInstance
                var material = asset.Type.Material;
                if (material != null)
                {
                    asset.Type.MaterialInstance = new YamlMappingNode();
                    asset.Type.MaterialInstance.Material = material;
                    asset.Type.Material = DynamicYamlEmpty.Default;
                }
                var type = asset.Type.Node as YamlMappingNode;
                if (type != null && type.Tag == "!CubeProceduralModel")
                {
                    // Size changed from scalar to vector3
                    var size = asset.Type.Size as DynamicYamlScalar;
                    if (size != null)
                    {
                        var vecSize = new YamlMappingNode
                        {
                            { new YamlScalarNode("X"), new YamlScalarNode(size.Node.Value) },
                            { new YamlScalarNode("Y"), new YamlScalarNode(size.Node.Value) },
                            { new YamlScalarNode("Z"), new YamlScalarNode(size.Node.Value) }
                        };
                        vecSize.Style = DataStyle.Compact;
                        asset.Type.Size = vecSize;
                    }
                }
            }
        }

        class RenameCapsuleHeight : AssetUpgraderBase
        {
            protected override void UpgradeAsset(AssetMigrationContext context, PackageVersion currentVersion, PackageVersion targetVersion, dynamic asset, PackageLoadingAssetFile assetFile, OverrideUpgraderHint overrideHint)
            {
                var proceduralType = asset.Type;
                if (proceduralType.Node.Tag == "!CapsuleProceduralModel" && proceduralType.Height != null)
                {
                    proceduralType.Length = 2f * (float)proceduralType.Height;
                    proceduralType.Height = DynamicYamlEmpty.Default;
                }
            }
        }

        class RenameDiameters : AssetUpgraderBase
        {
            protected override void UpgradeAsset(AssetMigrationContext context, PackageVersion currentVersion, PackageVersion targetVersion, dynamic asset, PackageLoadingAssetFile assetFile, OverrideUpgraderHint overrideHint)
            {
                var proceduralType = asset.Type;
                if (proceduralType.Diameter != null)
                {
                    proceduralType.Radius = 0.5f * (float)proceduralType.Diameter;
                    proceduralType.Diameter = DynamicYamlEmpty.Default;
                }
                if (proceduralType.Node.Tag == "!TorusProceduralModel" && proceduralType.Thickness != null)
                {
                    proceduralType.Thickness = 0.5f * (float)proceduralType.Thickness;
                }
            }
        }

        class Standardization : AssetUpgraderBase
        {
            protected override void UpgradeAsset(AssetMigrationContext context, PackageVersion currentVersion, PackageVersion targetVersion, dynamic asset, PackageLoadingAssetFile assetFile, OverrideUpgraderHint overrideHint)
            {
                var proceduralType = asset.Type;

                if (proceduralType.ScaleUV != null)
                {
                    var currentScale = (float)proceduralType.ScaleUV;

                    var vecSize = new YamlMappingNode
                    {
                        { new YamlScalarNode("X"), new YamlScalarNode(currentScale.ToString(CultureInfo.InvariantCulture)) },
                        { new YamlScalarNode("Y"), new YamlScalarNode(currentScale.ToString(CultureInfo.InvariantCulture)) }
                    };
                    vecSize.Style = DataStyle.Compact;

                    proceduralType.RemoveChild("ScaleUV");

                    proceduralType.UvScale = vecSize;
                }
                else if (proceduralType.UVScales != null)
                {
                    var x = (float)proceduralType.UVScales.X;
                    var y = (float)proceduralType.UVScales.Y;

                    var vecSize = new YamlMappingNode
                    {
                        { new YamlScalarNode("X"), new YamlScalarNode(x.ToString(CultureInfo.InvariantCulture)) },
                        { new YamlScalarNode("Y"), new YamlScalarNode(y.ToString(CultureInfo.InvariantCulture)) }
                    };
                    vecSize.Style = DataStyle.Compact;

                    proceduralType.RemoveChild("UVScales");

                    proceduralType.UvScale = vecSize;
                }
                else
                {
                    var vecSize = new YamlMappingNode
                    {
                        { new YamlScalarNode("X"), new YamlScalarNode("1") },
                        { new YamlScalarNode("Y"), new YamlScalarNode("1") }
                    };
                    vecSize.Style = DataStyle.Compact;

                    proceduralType.UvScale = vecSize;
                }
            }
        }

        class CapsuleRadiusDefaultChange : AssetUpgraderBase
        {
            protected override void UpgradeAsset(AssetMigrationContext context, PackageVersion currentVersion, PackageVersion targetVersion, dynamic asset, PackageLoadingAssetFile assetFile, OverrideUpgraderHint overrideHint)
            {
                // SerializedVersion format changed during renaming upgrade. However, before this was merged back in master, some asset upgrader still with older version numbers were developed.
                // However since this upgrader can be reapplied, it is not a problem
                var proceduralType = asset.Type;
                if (proceduralType.Node.Tag == "!CapsuleProceduralModel" && currentVersion != PackageVersion.Parse("0.0.6"))
                {
                    if (proceduralType.Radius == null)
                    {
                        proceduralType.Radius = 0.25f;
                    }
                }
            }
        }

        class ConeOffsetChange : AssetUpgraderBase
        {
            protected override void UpgradeAsset(AssetMigrationContext context, PackageVersion currentVersion, PackageVersion targetVersion, dynamic asset, PackageLoadingAssetFile assetFile, OverrideUpgraderHint overrideHint)
            {
                // SerializedVersion format changed during renaming upgrade. However, before this was merged back in master, some asset upgrader still with older version numbers were developed.
                var proceduralType = asset.Type;
                if (proceduralType.Node.Tag == "!ConeProceduralModel" && currentVersion != PackageVersion.Parse("0.0.6"))
                {
                    if (proceduralType.LocalOffset == null)
                    {
                        dynamic offset = new DynamicYamlMapping(new YamlMappingNode());
                        offset.AddChild("X", 0.0f);
                        offset.AddChild("Y", 0.5f);
                        offset.AddChild("Z", 0.0f);
                        proceduralType.AddChild("LocalOffset", offset);
                    }
                }
            }
        }
    }
}
