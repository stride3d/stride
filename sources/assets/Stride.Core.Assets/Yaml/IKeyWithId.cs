// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Core.Reflection;

namespace Stride.Core.Yaml
{
    /// <summary>
    /// An interface representing an association between an <see cref="ItemId"/> and the key of a dictionary.
    /// </summary>
    public interface IKeyWithId
    {
        /// <summary>
        /// The <see cref="ItemId"/> associated to the key.
        /// </summary>
        ItemId Id { get; }
        /// <summary>
        /// The key of the dictionary.
        /// </summary>
        object Key { get; }
        /// <summary>
        /// The type of the key.
        /// </summary>
        Type KeyType { get; }
        /// <summary>
        /// Indicates whether this key is considered to be deleted in the dictionary, and kept around for reconcilation purpose.
        /// </summary>
        bool IsDeleted { get; }
    }
}
