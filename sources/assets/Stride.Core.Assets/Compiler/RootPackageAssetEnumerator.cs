// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Stride.Core.Assets.Analysis;
using Stride.Core.Extensions;

namespace Stride.Core.Assets.Compiler
{
    /// <summary>
    /// Only enumerates assets that are marked as roots and their dependencies.
    /// </summary>
    public class RootPackageAssetEnumerator : IPackageCompilerSource
    {
        private readonly Package rootPackage;
        private readonly BuildDependencyManager buildDependencyManager;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="package">The start package.</param>
        /// <param name="extraRoots">The extra roots that needs to be collected with their dependencies.</param>
        public RootPackageAssetEnumerator(Package package)
        {
            rootPackage = package;
            buildDependencyManager = new BuildDependencyManager();
        }

        /// <inheritdoc/>
        public IEnumerable<AssetItem> GetAssets(AssetCompilerResult assetCompilerResult)
        {
            // Check integrity of the packages
            var packageAnalysis = new PackageSessionAnalysis(rootPackage.Session, new PackageAnalysisParameters());
            packageAnalysis.Run(assetCompilerResult);
            if (assetCompilerResult.HasErrors)
            {
                yield break;
            }

            // Compute list of assets to compile and their dependencies
            var packagesProcessed = new HashSet<Package>();
            var assetsReferenced = new HashSet<AssetItem>();
            CollectReferences(rootPackage, assetsReferenced, packagesProcessed);

            foreach (var assetItem in assetsReferenced)
            {
                yield return assetItem;
            }
        }

        /// <summary>
        /// Helper method to collect explicit AssetReferences
        /// </summary>
        /// <param name="package"></param>
        /// <param name="assetsReferenced"></param>
        /// <param name="packagesProcessed"></param>
        private void CollectReferences(Package package, HashSet<AssetItem> assetsReferenced, HashSet<Package> packagesProcessed)
        {
            // Check if already processed
            if (!packagesProcessed.Add(package))
                return;

            // Determine set of assets to compile
            // Start with roots:
            //  1. Explicit AssetReferences
            foreach (var reference in package.RootAssets)
            {
                // Locate reference
                var asset = package.Session.FindAsset(reference.Id) ?? package.Session.FindAsset(reference.Location);
                if (asset != null)
                {
                    assetsReferenced.Add(asset);
                }
            }

            //  2. Process referenced packages as well (for their roots)
            foreach (var dependency in package.Container.FlattenedDependencies.Select(x => x.Package).NotNull())
            {
                CollectReferences(dependency, assetsReferenced, packagesProcessed);
            }

            // 3. Some types are marked with AlwaysMarkAsRoot
            foreach (var assetItem in package.Assets)
            {
                if (AssetRegistry.IsAssetTypeAlwaysMarkAsRoot(assetItem.Asset.GetType()))
                {
                    assetsReferenced.Add(assetItem);
                }
            }
        }
    }
}
