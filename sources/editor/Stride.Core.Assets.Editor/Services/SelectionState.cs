// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Extensions;
using Stride.Core.Reflection;

namespace Stride.Core.Assets.Editor.Services
{
    /// <summary>
    /// This class represents the state of selection in several collections representing selected items.
    /// </summary>
    public class SelectionState : IEquatable<SelectionState>
    {
        /// <summary>
        /// Internal state.
        /// </summary>
        private struct State
        {
            public State([NotNull] HashSet<AbsoluteId> ids, [NotNull] Func<AbsoluteId, object> idToObject)
            {
                if (ids == null) throw new ArgumentNullException(nameof(ids));
                if (idToObject == null) throw new ArgumentNullException(nameof(idToObject));
                Ids = ids;
                GetObjectToSelect = idToObject;
            }

            /// <summary>
            /// Identifiers of the selected objects.
            /// </summary>
            [NotNull]
            public HashSet<AbsoluteId> Ids { get; }

            /// <inheritdoc cref="SelectionScope.GetObjectToSelect"/>
            /// <seealso cref="SelectionScope.GetObjectToSelect"/>
            [NotNull]
            public Func<AbsoluteId, object> GetObjectToSelect { get;  }
        }

        /// <summary>
        /// Represents a snapshot of a set of collection. The key is the collection itself, used to retrieve the snapshot by reference.
        /// The value is the snapshot, which is a copy of the key content converted to absolute identifiers.
        /// </summary>
        private readonly Dictionary<INotifyCollectionChanged, State> states = new Dictionary<INotifyCollectionChanged, State>();

        /// <summary>
        /// Gets whether this state represents a valid selection.
        /// </summary>
        /// <returns><c>true</c> if this state represents a valid selection; otherwise, <c>false</c>.</returns>
        public bool HasValidSelection()
        {
            return states.Any(s => s.Value.Ids.Select(s.Value.GetObjectToSelect).NotNull().Any());
        }

        /// <summary>
        /// Saves the state of selections from a given sequence of selection scopes.
        /// </summary>
        /// <param name="scopes">The scopes containing the collections from which to save the state.</param>
        public void SaveStates([ItemNotNull, NotNull] IReadOnlyCollection<SelectionScope> scopes)
        {
            states.Clear();
            foreach (var scope in scopes)
            {
                foreach (var collection in scope.Collections.OfType<IEnumerable>())
                {
                    states.Add((INotifyCollectionChanged)collection, new State
                    (
                        new HashSet<AbsoluteId>(collection.Cast<object>().Select(scope.GetSelectedObjectId).NotNull()),
                        scope.GetObjectToSelect
                    ));
                }
            }
        }

        /// <summary>
        /// Restores the state of selections to the selection collections that were passed to <see cref="SaveStates"/>.
        /// </summary>
        public void RestoreStates()
        {
            foreach (var kv in states)
            {
                var collection = kv.Key;
                var descriptor = TypeDescriptorFactory.Default.Find(collection.GetType()) as CollectionDescriptor;
                if (descriptor == null)
                    continue;

                descriptor.Clear(collection);
                var state = kv.Value;
                foreach (var item in state.Ids.Select(state.GetObjectToSelect).NotNull())
                {
                    descriptor.Add(collection, item);
                }
            }
        }

        /// <summary>
        /// Registers a collection to this state, creating an empty snapshot for it.
        /// </summary>
        /// <param name="scope"></param>
        /// <param name="collection">The collection to register.</param>
        public void RegisterCollection([NotNull] SelectionScope scope, [NotNull] INotifyCollectionChanged collection)
        {
            if (scope == null) throw new ArgumentNullException(nameof(scope));
            if (collection == null) throw new ArgumentNullException(nameof(collection));

            if (!states.ContainsKey(collection))
            {
                states.Add(collection, new State(new HashSet<AbsoluteId>(), scope.GetObjectToSelect));
            }
        }

        /// <summary>
        /// Unregisters a collection from this state, discarding it from the snapshot.
        /// </summary>
        /// <param name="collection">The collection to unregister.</param>
        public void UnregisterCollection([NotNull] INotifyCollectionChanged collection)
        {
            if (collection == null) throw new ArgumentNullException(nameof(collection));
            states.Remove(collection);
        }

        /// <inheritdoc/>
        public bool Equals(SelectionState other)
        {
            if (states.Count != other?.states.Count)
                return false;

            foreach (var kv in states)
            {
                State otherState;
                if (!other.states.TryGetValue(kv.Key, out otherState))
                    return false;

                if (!kv.Value.Ids.SetEquals(otherState.Ids))
                    return false;
            }
            return true;
        }
    }
}
