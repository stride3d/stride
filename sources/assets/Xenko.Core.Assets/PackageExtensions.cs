// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using Xenko.Core.IO;
using Xenko.Core.Serialization;
using Xenko.Core.Serialization.Contents;

namespace Xenko.Core.Assets
{
    /// <summary>
    /// Extensions for <see cref="Package"/>
    /// </summary>
    public static class PackageExtensions
    {
        /// <summary>
        /// Finds an asset from all the packages by its asset reference.
        /// It will first try by id, then location.
        /// </summary>
        /// <param name="package">The package.</param>
        /// <param name="reference">The reference to the asset.</param>
        /// <returns>An <see cref="AssetItem" /> or <c>null</c> if not found.</returns>
        public static AssetItem FindAsset(this Package package, IReference reference)
        {
            return package.FindAsset(reference.Id) ?? package.FindAsset(reference.Location);
        }

        internal static IEnumerable<Package> GetPackagesWithDependencies(this Package currentPackage)
        {
            // Let's do a depth first search
            if (currentPackage == null)
            {
                yield break;
            }

            yield return currentPackage;

            var session = currentPackage.Session;

            foreach (var storeDep in currentPackage.Meta.Dependencies)
            {
                var package = session.Packages.Find(storeDep);
                // In case the package is not found (when working with session not fully loaded/resolved with all deps)
                if (package != null)
                {
                    yield return package;
                }
            }

            foreach (var localDep in currentPackage.LocalDependencies)
            {
                var package = session.Packages.Find(localDep.Id);
                // In case the package is not found (when working with session not fully loaded/resolved with all deps)
                if (package != null)
                {
                    yield return package;
                }
            }
        }

        public static IEnumerable<Package> GetPackagesWithRecursiveDependencies(this Package startPackage)
        {
            // Non-recursive Depth First Search
            var packagesToVisit = new Stack<Package>();
            var visitedPackages = new HashSet<Package>();
            packagesToVisit.Push(startPackage);

            var session = startPackage.Session;

            while (packagesToVisit.Count > 0)
            {
                var packageToVisit = packagesToVisit.Pop();
                if (visitedPackages.Add(packageToVisit))
                {
                    yield return packageToVisit;

                    // Push references
                    foreach (var storeDep in packageToVisit.Meta.Dependencies)
                    {
                        var package = session.Packages.Find(storeDep);
                        // In case the package is not found (when working with session not fully loaded/resolved with all deps)
                        if (package != null)
                        {
                            packagesToVisit.Push(package);
                        }
                    }

                    foreach (var localDep in packageToVisit.LocalDependencies)
                    {
                        var package = session.Packages.Find(localDep.Id);
                        // In case the package is not found (when working with session not fully loaded/resolved with all deps)
                        if (package != null)
                        {
                            packagesToVisit.Push(package);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Finds the package dependencies for the specified <see cref="Package" />. See remarks.
        /// </summary>
        /// <param name="rootPackage">The root package.</param>
        /// <param name="includeRootPackage">if set to <c>true</c> [include root package].</param>
        /// <param name="isRecursive">if set to <c>true</c> [is recursive].</param>
        /// <param name="storeOnly">if set to <c>true</c> [ignores local packages and keeps only store packages].</param>
        /// <returns>List&lt;Package&gt;.</returns>
        /// <exception cref="System.ArgumentNullException">rootPackage</exception>
        /// <exception cref="System.ArgumentException">Root package must be part of a session;rootPackage</exception>
        public static PackageCollection FindDependencies(this Package rootPackage, bool includeRootPackage = false, bool isRecursive = true, bool storeOnly = false)
        {
            if (rootPackage == null) throw new ArgumentNullException("rootPackage");
            var packages = new PackageCollection();

            if (includeRootPackage)
            {
                packages.Add(rootPackage);
            }

            FillPackageDependencies(rootPackage, isRecursive, packages, storeOnly);

            return packages;
        }

        /// <summary>
        /// Determines whether the specified packages contains an asset by its guid.
        /// </summary>
        /// <param name="packages">The packages.</param>
        /// <param name="assetId">The asset unique identifier.</param>
        /// <returns><c>true</c> if the specified packages contains asset; otherwise, <c>false</c>.</returns>
        public static bool ContainsAsset(this IEnumerable<Package> packages, AssetId assetId)
        {
            return packages.Any(package => package.Assets.ContainsById(assetId));
        }

        /// <summary>
        /// Determines whether the specified packages contains an asset by its location.
        /// </summary>
        /// <param name="packages">The packages.</param>
        /// <param name="location">The location.</param>
        /// <returns><c>true</c> if the specified packages contains asset; otherwise, <c>false</c>.</returns>
        public static bool ContainsAsset(this IEnumerable<Package> packages, UFile location)
        {
            return packages.Any(package => package.Assets.Find(location) != null);
        }

        private static void FillPackageDependencies(Package rootPackage, bool isRecursive, ICollection<Package> packagesFound, bool storeOnly = false)
        {
            var session = rootPackage.Session;

            if (session == null && (rootPackage.Meta.Dependencies.Count > 0 || rootPackage.LocalDependencies.Count > 0))
            {
                throw new InvalidOperationException("Cannot query package with dependencies when it is not attached to a session");
            }

            // 1. Load store package
            foreach (var packageDependency in rootPackage.Meta.Dependencies)
            {
                var package = session.Packages.Find(packageDependency);
                if (package == null)
                {
                    continue;
                }

                if (!packagesFound.Contains(package))
                {
                    packagesFound.Add(package);

                    if (isRecursive)
                    {
                        FillPackageDependencies(package, isRecursive, packagesFound, storeOnly);
                    }
                }
            }

            if (storeOnly)
            {
                return;
            }

            // 2. Load local packages
            foreach (var packageReference in rootPackage.LocalDependencies)
            {
                var package = session.Packages.Find(packageReference.Id);
                if (package == null)
                {
                    continue;
                }

                if (!packagesFound.Contains(package))
                {
                    packagesFound.Add(package);

                    if (isRecursive)
                    {
                        FillPackageDependencies(package, isRecursive, packagesFound, storeOnly);
                    }
                }
            }

        }
    }
}
