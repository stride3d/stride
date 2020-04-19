// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Stride.Core.IO;
using Stride.Core.Serialization.Contents;

namespace Stride.Core.Assets.Analysis
{
    public static class AssetCollision
    {
        /// <summary>
        /// Cleans the specified input items.
        /// </summary>
        /// <param name="package">The package to process (optional).</param>
        /// <param name="inputItems">The input items.</param>
        /// <param name="outputItems">The output items.</param>
        /// <param name="assetResolver">The asset resolver.</param>
        /// <param name="cloneInput">if set to <c>true</c> [clone input].</param>
        /// <param name="removeUnloadableObjects">If set to <c>true</c>, assets will be cloned with <see cref="AssetClonerFlags.RemoveUnloadableObjects"/>.</param>
        /// <exception cref="System.ArgumentNullException">
        /// inputItems
        /// or
        /// outputItems
        /// or
        /// assetResolver
        /// </exception>
        /// <exception cref="System.ArgumentException">List cannot contain null items;inputItems</exception>
        public static void Clean(Package package, ICollection<AssetItem> inputItems, ICollection<AssetItem> outputItems, AssetResolver assetResolver, bool cloneInput, bool removeUnloadableObjects)
        {
            if (inputItems == null) throw new ArgumentNullException(nameof(inputItems));
            if (outputItems == null) throw new ArgumentNullException(nameof(outputItems));
            if (assetResolver == null) throw new ArgumentNullException(nameof(assetResolver));

            // Check that all items are non-null
            if (inputItems.Any(item => item == null))
            {
                throw new ArgumentException("List cannot contain null items", nameof(inputItems));
            }

            var items = inputItems;
            if (cloneInput)
            {
                items = inputItems.Select(item => item.Clone(flags: removeUnloadableObjects ? AssetClonerFlags.RemoveUnloadableObjects : AssetClonerFlags.None)).ToList();
            }

            // idRemap should contain only assets that have either 1) their id remapped or 2) their location remapped
            var idRemap = new Dictionary<AssetId, Tuple<AssetId, UFile>>();
            var itemRemap = new Dictionary<AssetItem, Tuple<AssetId, UFile>>();
            foreach (var item in items)
            {
                if (outputItems.Contains(item))
                {
                    continue;
                }

                outputItems.Add(item);

                bool changed = false;
                AssetId newId;
                if (assetResolver.RegisterId(item.Id, out newId))
                {
                    changed = true;
                }

                // Note: we ignore name collisions if asset is not referenceable
                var referenceable = item.Asset.GetType().GetCustomAttribute<AssetDescriptionAttribute>()?.Referenceable ?? true;

                UFile newLocation = null;
                if (referenceable && assetResolver.RegisterLocation(item.Location, out newLocation))
                {
                    changed = true;
                }

                var tuple = new Tuple<AssetId, UFile>(newId != AssetId.Empty ? newId : item.Id, newLocation ?? item.Location);
                if (changed)
                {
                    if (!itemRemap.ContainsKey(item))
                    {
                        itemRemap.Add(item, tuple);
                    }
                }

                if (!idRemap.ContainsKey(item.Id))
                {
                    idRemap.Add(item.Id, tuple);
                }
            }

            // Process assets
            foreach (var item in outputItems)
            {
                Tuple<AssetId, UFile> remap;
                if (itemRemap.TryGetValue(item, out remap) && (remap.Item1 != item.Asset.Id || remap.Item2 != item.Location))
                {
                    item.Asset.Id = remap.Item1;
                    item.Location = remap.Item2;
                    item.IsDirty = true;
                }

                // Fix base parts if there are any remap for them as well
                // This has to be done before the default resolver below because this fix requires to rewrite the base part completely, since the base part asset is immutable
                var assetComposite = item.Asset as IAssetComposite;
                if (assetComposite != null)
                {
                    foreach (var basePart in assetComposite.CollectParts())
                    {
                        if (basePart.Base != null && idRemap.TryGetValue(basePart.Base.BasePartAsset.Id, out remap) && IsNewReference(remap, basePart.Base.BasePartAsset))
                        {
                            var newAssetReference = new AssetReference(remap.Item1, remap.Item2);
                            basePart.UpdateBase(new BasePart(newAssetReference, basePart.Base.BasePartId, basePart.Base.InstanceId));
                            item.IsDirty = true;
                        }
                    }
                }

                // The loop is a one or two-step. 
                // - If there is no link to update, and the asset has not been cloned, we can exist immediately
                // - If there is links to update, and the asset has not been cloned, we need to clone it and re-enter the loop
                //   to perform the update of the clone asset
                var links = AssetReferenceAnalysis.Visit(item.Asset).Where(link => link.Reference is IReference).ToList();

                foreach (var assetLink in links)
                {
                    var assetReference = (IReference)assetLink.Reference;

                    var newId = assetReference.Id;
                    if (idRemap.TryGetValue(newId, out remap) && IsNewReference(remap, assetReference))
                    {
                        assetLink.UpdateReference(remap.Item1, remap.Item2);
                        item.IsDirty = true;
                    }
                }
            }

            // Process roots (until references in package are handled in general)
            if (package != null)
            {
                UpdateRootAssets(package.RootAssets, idRemap);
            }
        }

        private static void UpdateRootAssets(RootAssetCollection rootAssetCollection, IReadOnlyDictionary<AssetId, Tuple<AssetId, UFile>> idRemap)
        {
            foreach (var rootAsset in rootAssetCollection.ToArray())
            {
                var id = rootAsset.Id;
                Tuple<AssetId, UFile> remap;

                if (idRemap.TryGetValue(id, out remap) && IsNewReference(remap, rootAsset))
                {
                    var newRootAsset = new AssetReference(remap.Item1, remap.Item2);
                    rootAssetCollection.Remove(rootAsset.Id);
                    rootAssetCollection.Add(newRootAsset);
                }
            }
        }

        private static bool IsNewReference(Tuple<AssetId, UFile> newReference, IReference previousReference)
        {
            return newReference.Item1 != previousReference.Id ||
                   newReference.Item2 != previousReference.Location;
        }
    }
}
