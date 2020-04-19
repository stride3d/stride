// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

namespace Stride.Core.Reflection
{
    /// <summary>
    /// A helper static class to retrieve <see cref="CollectionItemIdentifiers"/> from a collection or dictionary through the <see cref="ShadowObject"/> registry.
    /// </summary>
    public static class CollectionItemIdHelper
    {
        // TODO: do we really need to pass an object to this constructor?
        private static readonly ShadowObjectPropertyKey CollectionItemIdKey = new ShadowObjectPropertyKey(new object(), false);

        public static bool HasCollectionItemIds(object instance)
        {
            return ShadowObject.Get(instance)?.ContainsKey(CollectionItemIdKey) ?? false;
        }

        public static bool TryGetCollectionItemIds(object instance, out CollectionItemIdentifiers itemIds)
        {
            var shadow = ShadowObject.Get(instance);
            if (shadow == null)
            {
                itemIds = null;
                return false;
            }

            object result;
            itemIds = shadow.TryGetValue(CollectionItemIdKey, out result) ? (CollectionItemIdentifiers)result : null;
            return result != null;
        }

        public static CollectionItemIdentifiers GetCollectionItemIds(object instance)
        {
            if (instance.GetType().IsValueType) throw new ArgumentException(@"The given instance is a value type and cannot have a item ids attached to it.", nameof(instance));

            var shadow = ShadowObject.GetOrCreate(instance);
            object result;
            if (shadow.TryGetValue(CollectionItemIdKey, out result))
            {
                return (CollectionItemIdentifiers)result;
            }

            var itemIds = new CollectionItemIdentifiers();
            shadow.Add(CollectionItemIdKey, itemIds);
            return itemIds;
        }
    }
}
