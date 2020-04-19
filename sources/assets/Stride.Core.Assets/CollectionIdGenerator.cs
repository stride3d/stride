// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Stride.Core.Annotations;
using Stride.Core.Reflection;

namespace Stride.Core.Assets
{
    /// <summary>
    /// A visitor that will generate a <see cref="CollectionItemIdentifiers"/> collection for each collection or dictionary of the visited object,
    /// and attach it to the related collection.
    /// </summary>
    public class CollectionIdGenerator : DataVisitorBase
    {
        private int inNonIdentifiableType;
        private HashSet<object> nonIdentifiableCollection;

        protected override bool CanVisit(object obj)
        {
            return !AssetRegistry.IsContentType(obj?.GetType()) && base.CanVisit(obj);
        }

        public override void VisitObject(object obj, ObjectDescriptor descriptor, bool visitMembers)
        {
            var localInNonIdentifiableType = false;
            try
            {
                if (descriptor.Attributes.OfType<NonIdentifiableCollectionItemsAttribute>().Any())
                {
                    localInNonIdentifiableType = true;
                    inNonIdentifiableType++;
                }
                base.VisitObject(obj, descriptor, visitMembers);
            }
            finally
            {
                if (localInNonIdentifiableType)
                    inNonIdentifiableType--;
            }
        }

        public override void VisitObjectMember(object container, ObjectDescriptor containerDescriptor, IMemberDescriptor member, object value)
        {
            if (member.GetCustomAttributes<NonIdentifiableCollectionItemsAttribute>(true).Any())
            {
                // Value types (collection that are struct) will automatically be considered as non-identifiable.
                if (value?.GetType().IsValueType == false)
                {
                    nonIdentifiableCollection = nonIdentifiableCollection ?? new HashSet<object>();
                    nonIdentifiableCollection.Add(value);
                }
            }
            base.VisitObjectMember(container, containerDescriptor, member, value);
        }

        public override void VisitArray(Array array, ArrayDescriptor descriptor)
        {
            if (ShouldGenerateItemIdCollection(array))
            {
                var itemIds = CollectionItemIdHelper.GetCollectionItemIds(array);
                for (var i = 0; i < array.Length; ++i)
                {
                    itemIds.Add(i, ItemId.New());
                }
            }
            base.VisitArray(array, descriptor);
        }

        public override void VisitCollection(IEnumerable collection, CollectionDescriptor descriptor)
        {
            if (ShouldGenerateItemIdCollection(collection))
            {
                var itemIds = CollectionItemIdHelper.GetCollectionItemIds(collection);
                var count = descriptor.GetCollectionCount(collection);
                for (var i = 0; i < count; ++i)
                {
                    itemIds.Add(i, ItemId.New());
                }
            }
            base.VisitCollection(collection, descriptor);
        }

        public override void VisitDictionary(object dictionary, DictionaryDescriptor descriptor)
        {
            if (ShouldGenerateItemIdCollection(dictionary))
            {
                var itemIds = CollectionItemIdHelper.GetCollectionItemIds(dictionary);
                foreach (var element in descriptor.GetEnumerator(dictionary))
                {
                    itemIds.Add(element.Key, ItemId.New());
                }
            }
            base.VisitDictionary(dictionary, descriptor);
        }

        private bool ShouldGenerateItemIdCollection(object collection)
        {
            // Do not generate for value types (collections that are struct) or null
            if (collection?.GetType().IsValueType != false)
                return false;

            // Do not generate if within a type that doesn't use identifiable collections
            if (inNonIdentifiableType > 0)
                return false;

            // Do not generate if item id collection already exists
            if (CollectionItemIdHelper.HasCollectionItemIds(collection))
                return false;

            // Do not generate if the collection has been flagged to not be identifiable
            if (nonIdentifiableCollection?.Contains(collection) == true)
                return false;

            return true;
        }
    }
}
