// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;

using Stride.Core.Assets.Diagnostics;
using Stride.Core.Assets.Tracking;
using Stride.Core.Diagnostics;
using Stride.Core.IO;
using Stride.Core.Serialization.Contents;

namespace Stride.Core.Assets.Analysis
{
    /// <summary>
    /// Analysis for <see cref="AssetItem"/>.
    /// </summary>
    public static class AssetAnalysis
    {
        public static LoggerResult Run(IEnumerable<AssetItem> items, AssetAnalysisParameters parameters)
        {
            if (items == null) throw new ArgumentNullException(nameof(items));
            if (parameters == null) throw new ArgumentNullException(nameof(parameters));

            var result = new LoggerResult();
            Run(items, result, parameters);
            return result;
        }

        public static void Run(IEnumerable<AssetItem> items, ILogger log, AssetAnalysisParameters parameters)
        {
            if (items == null) throw new ArgumentNullException(nameof(items));
            if (log == null) throw new ArgumentNullException(nameof(log));
            if (parameters == null) throw new ArgumentNullException(nameof(parameters));

            foreach (var assetItem in items)
            {
                Run(assetItem, log, parameters);
            }
        }

        public static LoggerResult FixAssetReferences(IEnumerable<AssetItem> items)
        {
            var parameters = new AssetAnalysisParameters() { IsProcessingAssetReferences = true, IsLoggingAssetNotFoundAsError =  true};
            var result = new LoggerResult();
            Run(items, result, parameters);
            return result;
        }

        public static void Run(AssetItem assetItem, ILogger log, AssetAnalysisParameters parameters)
        {
            if (assetItem == null) throw new ArgumentNullException(nameof(assetItem));
            if (log == null) throw new ArgumentNullException(nameof(log));
            if (parameters == null) throw new ArgumentNullException(nameof(parameters));

            if (assetItem.Package == null)
            {
                throw new InvalidOperationException("AssetItem must belong to an existing package");
            }

            var package = assetItem.Package;

            // Check that there is no duplicate in assets
            if (package.Session != null)
            {
                var packages = package.FindDependencies();

                foreach (var otherPackage in packages)
                {
                    var existingAsset = otherPackage.Assets.Find(assetItem.Id);

                    if (existingAsset != null)
                    {
                        log.Error($"Assets [{existingAsset.FullPath}] with id [{existingAsset.Id}] from Package [{package.FullPath}] is already loaded from package [{existingAsset.Package.FullPath}]");
                    }
                    else
                    {
                        existingAsset = otherPackage.Assets.Find(assetItem.Location);
                        if (existingAsset != null)
                        {
                            log.Error($"Assets [{existingAsset.FullPath}] with location [{existingAsset.Location}] from Package [{package.FullPath}] is already loaded from package [{existingAsset.Package.FullPath}]");
                        }
                    }
                }
            }

            var assetReferences = AssetReferenceAnalysis.Visit(assetItem.Asset);

            if (package.Session != null && parameters.IsProcessingAssetReferences)
            {
                UpdateAssetReferences(assetItem, assetReferences, log, parameters);
            }
            // Update paths for asset items

            if (parameters.IsProcessingUPaths)
            {
                // Find where this asset item was previously stored (in a different package for example)
                CommonAnalysis.UpdatePaths(assetItem, assetReferences.Where(link => link.Reference is UPath), parameters);
                // Source hashes are not processed by analysis, we need to manually indicate them to update
                SourceHashesHelper.UpdateUPaths(assetItem.Asset, assetItem.FullPath.GetParent(), parameters.ConvertUPathTo);
            }
        }

        internal static void UpdateAssetReferences(AssetItem assetItem, IEnumerable<AssetReferenceLink> assetReferences, ILogger log, AssetAnalysisParameters parameters)
        {
            var package = assetItem.Package;
            var packageName = package.FullPath?.GetFileNameWithoutExtension() ?? "(Undefined path)";
            bool shouldSetDirtyFlag = false;

            // Update reference
            foreach (var assetReferenceLink in assetReferences.Where(link => link.Reference is IReference))
            {
                var contentReference = (IReference)assetReferenceLink.Reference;
                // Update Asset references (AssetReference, AssetBase, reference)
                var id = contentReference.Id;
                var newItemReference = package.FindAsset(id);

                // If asset was not found by id try to find by its location
                if (newItemReference == null)
                {
                    newItemReference = package.FindAsset(contentReference.Location);
                    if (newItemReference != null)
                    {
                        // If asset was found by its location, just emit a warning
                        log.Warning(package, contentReference, AssetMessageCode.AssetReferenceChanged, contentReference, newItemReference.Id);
                    }
                }

                // If asset was not found, display an error or a warning
                if (newItemReference == null)
                {
                    if (parameters.IsLoggingAssetNotFoundAsError)
                    {
                        log.Error(package, contentReference, AssetMessageCode.AssetForPackageNotFound, contentReference, packageName);

                        var packageFound = package.Session.Packages.FirstOrDefault(x => x.FindAsset(contentReference.Location) != null);
                        if (packageFound != null)
                        {
                            log.Warning(package, contentReference, AssetMessageCode.AssetFoundInDifferentPackage, contentReference, packageFound.FullPath.GetFileNameWithoutExtension());
                        }
                    }
                    else
                    {
                        log.Warning(package, contentReference, AssetMessageCode.AssetForPackageNotFound, contentReference, packageName);
                    }
                    continue;
                }

                // Only update location that are actually different
                var newLocationWithoutExtension = newItemReference.Location;
                if (newLocationWithoutExtension != contentReference.Location || newItemReference.Id != contentReference.Id)
                {
                    assetReferenceLink.UpdateReference(newItemReference.Id, newLocationWithoutExtension);
                    shouldSetDirtyFlag = true;
                }
            }

            // Setting the dirty flag is an heavy operation, we want to do it only once
            if (shouldSetDirtyFlag)
            {
                assetItem.IsDirty = true;
            }
        }
    }
}
