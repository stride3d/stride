// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using System.Linq;
using Stride.Core.Assets.Analysis;

namespace Stride.Core.Assets.Compiler
{
    /// <summary>
    /// Enumerate all assets of this package and its references.
    /// </summary>
    public class PackageAssetEnumerator : IPackageCompilerSource
    {
        private readonly Package package;

        public PackageAssetEnumerator(Package package)
        {
            this.package = package;
        }

        /// <inheritdoc/>
        public IEnumerable<AssetItem> GetAssets(AssetCompilerResult assetCompilerResult)
        {
            // Check integrity of the packages
            var packageAnalysis = new PackageSessionAnalysis(package.Session, new PackageAnalysisParameters());
            packageAnalysis.Run(assetCompilerResult);
            if (assetCompilerResult.HasErrors)
            {
                yield break;
            }

            var packages = package.GetPackagesWithDependencies();
            foreach (var pack in packages)
            {
                foreach (var asset in pack.Assets)
                {
                    yield return asset;
                }
            }
        }
    }
}
