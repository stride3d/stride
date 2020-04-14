// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Core.Reflection;

namespace Stride.Core.Yaml
{
    /// <summary>
    /// A generic structure that implements the <see cref="IKeyWithId"/> interface for keys that are not deleted.
    /// </summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    public struct KeyWithId<TKey> : IKeyWithId
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="KeyWithId{TKey}"/> structure.
        /// </summary>
        /// <param name="id">The <see cref="ItemId"/> associated to the key.</param>
        /// <param name="key">The key of the dictionary.</param>
        public KeyWithId(ItemId id, TKey key)
        {
            Id = id;
            Key = key;
        }

        /// <summary>
        /// The <see cref="ItemId"/> associated to the key.
        /// </summary>
        public readonly ItemId Id;
        /// <summary>
        /// The key of the dictionary.
        /// </summary>
        public readonly TKey Key;
        /// <inheritdoc/>
        ItemId IKeyWithId.Id => Id;
        /// <inheritdoc/>
        object IKeyWithId.Key => Key;
        /// <inheritdoc/>
        bool IKeyWithId.IsDeleted => false;
        /// <inheritdoc/>
        Type IKeyWithId.KeyType => typeof(TKey);
    }
}
