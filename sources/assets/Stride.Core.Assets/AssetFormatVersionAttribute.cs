// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Core;

namespace Stride.Core.Assets
{
    /// <summary>
    /// Describes what format version this asset currently uses, for asset upgrading.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class AssetFormatVersionAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AssetFormatVersionAttribute"/> class.
        /// </summary>
        /// <param name="name">The dependency name.</param>
        /// <param name="version">The current format version of this asset.</param>
        /// <param name="minUpgradableVersion">The minimum format version that supports upgrade for this asset.</param>
        public AssetFormatVersionAttribute(string name, int version, int minUpgradableVersion = 0)
            : this(name, "0.0." + version, minUpgradableVersion != 0 ? "0.0." + minUpgradableVersion : null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AssetFormatVersionAttribute"/> class.
        /// </summary>
        /// <param name="name">The dependency name.</param>
        /// <param name="version">The current format version of this asset.</param>
        /// <param name="minUpgradableVersion">The minimum format version that supports upgrade for this asset.</param>
        public AssetFormatVersionAttribute(string name, string version, string minUpgradableVersion = null)
        {
            Name = name;
            Version = PackageVersion.Parse(version);
            MinUpgradableVersion = PackageVersion.Parse(minUpgradableVersion ?? "0");
        }

        /// <summary>
        /// Gets or sets the dependency name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets the current format version of this asset.
        /// </summary>
        /// <value>
        /// The current format version of this asset.
        /// </value>
        public PackageVersion Version { get; set; }

        /// <summary>
        /// Gets the minimum format version that supports upgrade for this asset.
        /// </summary>
        /// <value>
        /// The minimum format version that supports upgrade for this asset.
        /// </value>
        public PackageVersion MinUpgradableVersion { get; set; }
    }
}
