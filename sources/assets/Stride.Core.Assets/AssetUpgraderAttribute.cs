// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Core;

namespace Stride.Core.Assets
{
    /// <summary>
    /// Describes which upgrader type to use to upgrade an asset, depending on this current version number.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class AssetUpgraderAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AssetUpgraderAttribute"/> with a range of supported initial version numbers.
        /// </summary>
        /// <param name="name">The dependency name.</param>
        /// <param name="startMinVersion">The minimal initial version number this upgrader can work on.</param>
        /// <param name="targetVersion">The target version number of this upgrader.</param>
        /// <param name="assetUpgraderType">The type of upgrader to instantiate to upgrade the asset.</param>
        public AssetUpgraderAttribute(string name, string startMinVersion, string targetVersion, Type assetUpgraderType)
        {
            Name = name;
            StartVersion = PackageVersion.Parse(startMinVersion);
            TargetVersion = PackageVersion.Parse(targetVersion);

            if (!typeof(IAssetUpgrader).IsAssignableFrom(assetUpgraderType))
                throw new ArgumentException(@"The assetUpgraderType must implement IAssetUpgrader interface", nameof(assetUpgraderType));
            if (TargetVersion <= StartVersion)
                throw new ArgumentException(@"The target version is lower or equal to the start version.", nameof(targetVersion));
            AssetUpgraderType = assetUpgraderType;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AssetUpgraderAttribute"/> with a single supported initial version number.
        /// </summary>
        /// <param name="name">The dependency name.</param>
        /// <param name="startVersion">The initial version number this upgrader can work on.</param>
        /// <param name="targetVersion">The target version number of this upgrader.</param>
        /// <param name="assetUpgraderType">The type of upgrader to instantiate to upgrade the asset.</param>
        public AssetUpgraderAttribute(string name, int startVersion, int targetVersion, Type assetUpgraderType)
            : this(name, "0.0." + startVersion, "0.0." + targetVersion, assetUpgraderType)
        {
        }

        /// <summary>
        /// Gets or sets the dependency name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the minimal initial version number this upgrader can work on.
        /// </summary>
        public PackageVersion StartVersion { get; set; }

        /// <summary>
        /// Gets or sets the target version number of this upgrader.
        /// </summary>
        public PackageVersion TargetVersion { get; set; }

        /// <summary>
        /// Gets or sets the type of upgrader to instantiate to upgrade the asset.
        /// </summary>
        public Type AssetUpgraderType { get; set; }
    }
}
