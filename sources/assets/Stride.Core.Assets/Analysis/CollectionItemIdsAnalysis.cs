// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections;
using System.Collections.Generic;
using Stride.Core.Diagnostics;
using Stride.Core.Reflection;

namespace Stride.Core.Assets.Analysis
{
    /// <summary>
    /// A static class that analyzes an <see cref="AssetItem"/> and fixes issues in <see cref="CollectionItemIdentifiers"/> of collections contained in this asset.
    /// </summary>
    public static class CollectionItemIdsAnalysis
    {
        /// <summary>
        /// Fixes up the <see cref="CollectionItemIdentifiers"/> of collections contained in the given asset. by generating new ids if there are any duplicate.
        /// </summary>
        /// <param name="assetItem">The asset to analyze.</param>
        /// <param name="logger">A logger to output fixed entries.</param>
        /// <remarks>This method doesn't handle collections in derived assets that will be desynchronized afterwards.</remarks>
        public static void FixupItemIds(AssetItem assetItem, ILogger logger)
        {
            var visitor = new CollectionItemIdsAnalysisVisitor(assetItem, logger);
            visitor.Visit(assetItem);
        }

        private class CollectionItemIdsAnalysisVisitor : DataVisitorBase
        {
            private readonly AssetItem assetItem;
            private readonly ILogger logger;

            public CollectionItemIdsAnalysisVisitor(AssetItem assetItem, ILogger logger)
            {
                this.assetItem = assetItem;
                this.logger = logger;
            }

            protected override bool CanVisit(object obj)
            {
                return !AssetRegistry.IsContentType(obj?.GetType()) && base.CanVisit(obj);
            }

            public override void VisitArray(Array array, ArrayDescriptor descriptor)
            {
                Fixup(array);
                base.VisitArray(array, descriptor);
            }

            public override void VisitCollection(IEnumerable collection, CollectionDescriptor descriptor)
            {
                Fixup(collection);
                base.VisitCollection(collection, descriptor);
            }

            public override void VisitDictionary(object dictionary, DictionaryDescriptor descriptor)
            {
                Fixup(dictionary);
                base.VisitDictionary(dictionary, descriptor);
            }

            /// <summary>
            /// Fixes up the <see cref="CollectionItemIdentifiers"/> of a collection by generating new ids if there are any duplicate.
            /// </summary>
            /// <param name="collection">The collection to fix up.</param>
            /// <remarks>This method doesn't handle collections in derived objects that will be desynchronized afterwards.</remarks>
            private void Fixup(object collection)
            {
                CollectionItemIdentifiers itemIds;
                if (CollectionItemIdHelper.TryGetCollectionItemIds(collection, out itemIds))
                {
                    var items = new HashSet<ItemId>();
                    var localCopy = new CollectionItemIdentifiers();
                    itemIds.CloneInto(localCopy, null);
                    foreach (var id in localCopy)
                    {
                        if (!items.Add(id.Value))
                        {
                            logger?.Warning($"Duplicate item identifier [{id.Value}] in collection {CurrentPath} of asset [{assetItem.Location}]. Generating a new identifier to remove the duplicate entry.");
                            itemIds[id.Key] = ItemId.New();
                        }
                    }
                }
            }
        }
    }
}
