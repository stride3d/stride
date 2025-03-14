// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Reflection;

namespace Stride.Core.Yaml;

/// <summary>
/// A generic structure that implements the <see cref="IKeyWithId"/> interface for keys that are deleted.
/// </summary>
/// <typeparam name="TKey">The type of the key.</typeparam>
public readonly struct DeletedKeyWithId<TKey> : IKeyWithId
{
    /// <summary>
    /// Initializes a new instance of the <see cref="KeyWithId{TKey}"/> structure.
    /// </summary>
    /// <param name="id">The <see cref="ItemId"/> associated to the deleted key.</param>
    public DeletedKeyWithId(ItemId id)
    {
        Id = id;
    }

    /// <summary>
    /// The <see cref="ItemId"/> associated to the key.
    /// </summary>
    public readonly ItemId Id;

    /// <inheritdoc/>
    readonly ItemId IKeyWithId.Id => Id;

    /// <inheritdoc/>
    readonly object IKeyWithId.Key => default(TKey);

    /// <inheritdoc/>
    readonly bool IKeyWithId.IsDeleted => true;

    /// <inheritdoc/>
    readonly Type IKeyWithId.KeyType => typeof(TKey);
}
