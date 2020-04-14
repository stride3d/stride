// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core.Diagnostics;
using Stride.Core.Serialization.Contents;
using Stride.Core.Yaml;

namespace Stride.Core.Assets
{
    /// <summary>
    /// Context used by <see cref="IAssetUpgrader"/>.
    /// </summary>
    public class AssetMigrationContext
    {
        /// <summary>
        /// Initializes a new instance of <see cref="AssetMigrationContext"/>.
        /// </summary>
        /// <param name="package"></param>
        /// <param name="assetReference"></param>
        /// <param name="assetFullPath"></param>
        /// <param name="log"></param>
        public AssetMigrationContext(Package package, IReference assetReference, string assetFullPath, ILogger log)
        {
            if (log == null) throw new ArgumentNullException(nameof(log));
            Package = package;
            AssetReference = assetReference;
            AssetFullPath = assetFullPath;
            Log = new AssetLogger(package, assetReference, assetFullPath, log);
        }

        /// <summary>
        /// The current package where the current asset is being migrated. This is null when the asset being migrated is a package.
        /// </summary>
        public Package Package { get; }

        public IReference AssetReference { get; }

        public string AssetFullPath { get; }

        /// <summary>
        /// The logger for this context.
        /// </summary>
        public ILogger Log { get; }
    }
}
