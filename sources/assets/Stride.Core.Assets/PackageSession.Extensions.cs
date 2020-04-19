// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Linq;
using Stride.Core.Assets.Analysis;
using Stride.Core.IO;
using Stride.Core.Serialization;

namespace Stride.Core.Assets
{
    /// <summary>
    /// Extension methods for <see cref="PackageSession"/>.
    /// </summary>
    public static class PackageSessionExtensions
    {
        /// <summary>
        /// Create a <see cref="Package"/> that can be used to compile an <see cref="AssetItem"/> by analyzing and resolving its dependencies.
        /// </summary>
        /// <returns>The package packageSession that can be used to compile the asset item.</returns>
        public static Package CreateCompilePackageFromAsset(this PackageSession session, AssetItem originalAssetItem)
        {
            // create the compile root package and package session
            var assetPackageCloned = new Package();
            //the following line is necessary to attach a session to the package
            // ReSharper disable once UnusedVariable
            var compilePackageSession = new PackageSession(assetPackageCloned);

            AddAssetToCompilePackage(session, originalAssetItem, assetPackageCloned);

            return assetPackageCloned;
        }

        public static void AddAssetToCompilePackage(this PackageSession session, AssetItem originalAssetItem, Package assetPackageCloned)
        {
            if (originalAssetItem == null) throw new ArgumentNullException("originalAssetItem");

            // Find the asset from the session
            var assetItem = originalAssetItem.Package.FindAsset(originalAssetItem.Id);
            if (assetItem == null)
            {
                throw new ArgumentException("Cannot find the specified AssetItem instance in the session");
            }

            // Calculate dependencies
            // Search only for references
            var dependencies = session.DependencyManager.ComputeDependencies(assetItem.Id, AssetDependencySearchOptions.Out | AssetDependencySearchOptions.Recursive, ContentLinkType.Reference);
            if (dependencies == null)
                throw new InvalidOperationException("The asset doesn't exist in the dependency manager anymore");

            var assetItemRootCloned = dependencies.Item.Clone();

            // Store the fullpath to the sourcefolder, this avoid us to clone hierarchy of packages
            assetItemRootCloned.SourceFolder = assetItem.FullPath.GetParent();

            if (assetPackageCloned.Assets.Find(assetItemRootCloned.Id) == null)
                assetPackageCloned.Assets.Add(assetItemRootCloned);

            // For each asset item dependency, clone it in the new package
            foreach (var assetLink in dependencies.LinksOut)
            {
                // Only add assets not already added (in case of circular dependencies)
                if (assetPackageCloned.Assets.Find(assetLink.Item.Id) == null)
                {
                    // create a copy of the asset item and add it to the appropriate compile package
                    var itemCloned = assetLink.Item.Clone();

                    // Store the fullpath to the sourcefolder, this avoid us to clone hierarchy of packages
                    itemCloned.SourceFolder = assetLink.Item.FullPath.GetParent();
                    assetPackageCloned.Assets.Add(itemCloned);
                }
            }
        }
    }
}
