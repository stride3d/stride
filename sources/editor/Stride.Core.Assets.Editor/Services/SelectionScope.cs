// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.Specialized;
using Stride.Core;
using Stride.Core.Annotations;

namespace Stride.Core.Assets.Editor.Services
{
    /// <summary>
    /// Defines a selection scope that might contains multiple non-exclusive selection collections.
    /// </summary>
    public class SelectionScope
    {
        internal SelectionScope([ItemNotNull, NotNull] IEnumerable<INotifyCollectionChanged> collections, [NotNull] Func<AbsoluteId, object> idToObject, [NotNull] Func<object, AbsoluteId?> objectToId)
        {
            Collections = collections.ToImmutableArray();
            GetObjectToSelect = idToObject;
            GetSelectedObjectId = objectToId;
        }

        /// <summary>
        /// The list of collections contained in this scope.
        /// </summary>
        [ItemNotNull]
        public ImmutableArray<INotifyCollectionChanged> Collections { get; }

        /// <summary>
        /// Gets the function used to resolve an identifier to an object to select when restoring the selection.
        /// </summary>
        [NotNull]
        public Func<AbsoluteId, object> GetObjectToSelect { get; }

        /// <summary>
        /// Gets the function used to retrieve the identifier of an object that is part of the current selection.
        /// </summary>
        [NotNull]
        public Func<object, AbsoluteId?> GetSelectedObjectId { get; }
    }
}
