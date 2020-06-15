// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Stride.Core.Annotations;
using Stride.Core.Collections;
using Stride.Core.Reflection;

namespace Stride.Core.Quantum.References
{
    /// <summary>
    /// A class representing an enumeration of references to multiple objects.
    /// </summary>
    public sealed class ReferenceEnumerable : IReferenceInternal, IEnumerable<ObjectReference>
    {
        private HybridDictionary<NodeIndex, ObjectReference> items;

        internal ReferenceEnumerable(IEnumerable enumerable, [NotNull] Type enumerableType)
        {
            Reference.CheckReferenceCreationSafeGuard();
            ObjectValue = enumerable;

            if (enumerableType.HasInterface(typeof(IDictionary<,>)))
                ElementType = enumerableType.GetInterface(typeof(IDictionary<,>)).GetGenericArguments()[1];
            else if (enumerableType.HasInterface(typeof(IEnumerable<>)))
                ElementType = enumerableType.GetInterface(typeof(IEnumerable<>)).GetGenericArguments()[0];
            else
                ElementType = typeof(object);
        }

        /// <inheritdoc/>
        public object ObjectValue { get; private set; }

        public Type ElementType { get; }

        /// <summary>
        /// Gets whether this reference enumerates a dictionary collection.
        /// </summary>
        public bool IsDictionary => ObjectValue is IDictionary || ObjectValue.GetType().HasInterface(typeof(IDictionary<,>));

        /// <inheritdoc/>
        public int Count => items?.Count ?? 0;

        /// <summary>
        /// Gets the indices of each reference in this instance.
        /// </summary>
        internal IReadOnlyCollection<NodeIndex> Indices { get; private set; }

        /// <inheritdoc/>
        public ObjectReference this[NodeIndex index] => items[index];

        /// <summary>
        /// Indicates whether the reference contains the given index.
        /// </summary>
        /// <param name="index">The index to check.</param>
        /// <returns><c>True</c> if the reference contains the given index, <c>False</c> otherwise.</returns>
        /// <remarks>If it is an <see cref="ObjectReference"/> it will return true only for <c>null</c>.</remarks>
        public bool HasIndex(NodeIndex index)
        {
            return items?.ContainsKey(index) ?? false;
        }

        public void Refresh(IGraphNode ownerNode, NodeContainer nodeContainer)
        {
            var newObjectValue = ownerNode.Retrieve();
            if (!(newObjectValue is IEnumerable)) throw new ArgumentException(@"The object is not an IEnumerable", nameof(newObjectValue));

            ObjectValue = newObjectValue;

            var newReferences = new HybridDictionary<NodeIndex, ObjectReference>();
            if (IsDictionary)
            {
                foreach (var item in (IEnumerable)ObjectValue)
                {
                    var key = GetKey(item);
                    var value = (ObjectReference)Reference.CreateReference(GetValue(item), ElementType, key, true);
                    newReferences.Add(key, value);
                }
            }
            else
            {
                var i = 0;
                foreach (var item in (IEnumerable)ObjectValue)
                {
                    var key = new NodeIndex(i);
                    var value = (ObjectReference)Reference.CreateReference(item, ElementType, key, true);
                    newReferences.Add(key, value);
                    ++i;
                }
            }

            // The reference need to be updated if it has never been initialized, if the number of items is different, or if any index or any value is different.
            var needUpdate = items == null || newReferences.Count != items.Count || !AreItemsEqual(items, newReferences);
            if (needUpdate)
            {
                // We create a mapping values of the old list of references to their corresponding target node. We use a list because we can have multiple times the same target in items.
                var oldReferenceMapping = new List<KeyValuePair<object, ObjectReference>>();
                if (items != null)
                {
                    var existingIndices = GraphNodeBase.GetIndices(ownerNode).ToList();
                    foreach (var item in items)
                    {
                        var boxedTarget = item.Value.TargetNode as BoxedNode;
                        // For collection of struct, we need to update the target nodes first so equity comparer will work. Careful tho, we need to skip removed items!
                        if (boxedTarget != null && existingIndices.Contains(item.Key))
                        {
                            // If we are boxing a struct, we reuse the same nodes if they are type-compatible and just overwrite the struct value.
                            var value = ownerNode.Retrieve(item.Key);
                            if (value?.GetType() == item.Value.TargetNode?.Type)
                            {
                                boxedTarget.UpdateFromOwner(ownerNode.Retrieve(item.Key));
                            }
                        }
                        if (item.Value.ObjectValue != null)
                        {
                            oldReferenceMapping.Add(new KeyValuePair<object, ObjectReference>(item.Value.ObjectValue, item.Value));
                        }
                    }
                }

                foreach (var newReference in newReferences)
                {
                    if (newReference.Value.ObjectValue != null)
                    {
                        var found = false;
                        var i = 0;
                        foreach (var item in oldReferenceMapping)
                        {
                            if (Equals(newReference.Value.ObjectValue, item.Key))
                            {
                                // If this value was already present in the old list of reference, just use the same target node in the new list.
                                newReference.Value.SetTarget(item.Value.TargetNode);
                                // Remove consumed existing reference so if there is a second entry with the same "key", it will be the other reference that will be used.
                                oldReferenceMapping.RemoveAt(i);
                                found = true;
                                break;
                            }
                            ++i;
                        }
                        if (!found)
                        {
                            // Otherwise, do a full update that will properly initialize the new reference.
                            newReference.Value.Refresh(ownerNode, nodeContainer, newReference.Key);
                        }
                    }
                }
                items = newReferences;
                // Remark: this works because both KeyCollection and List implements IReadOnlyCollection. Any internal change to HybridDictionary might break this!
                Indices = (IReadOnlyCollection<NodeIndex>)newReferences.Keys;
            }
        }

        /// <inheritdoc/>
        public ReferenceEnumerator GetEnumerator() => new ReferenceEnumerator(this);
        
        IEnumerator<ObjectReference> IEnumerable<ObjectReference>.GetEnumerator()
        {
            return new ReferenceEnumerator(this);
        }

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <inheritdoc/>
        public bool Equals(IReference other)
        {
            var otherEnumerable = other as ReferenceEnumerable;
            if (otherEnumerable == null)
                return false;

            return ReferenceEquals(this, otherEnumerable) || AreItemsEqual(items, otherEnumerable.items);
        }

        private static bool AreItemsEqual(HybridDictionary<NodeIndex, ObjectReference> items1, HybridDictionary<NodeIndex, ObjectReference> items2)
        {
            if (ReferenceEquals(items1, items2))
                return true;

            if (items1 == null || items2 == null)
                return false;

            if (items1.Count != items2.Count)
                return false;

            foreach (var item in items1)
            {
                ObjectReference otherValue;
                if (!items2.TryGetValue(item.Key, out otherValue))
                    return false;

                if (!otherValue.Index.Equals(item.Value.Index))
                    return false;

                if (otherValue.ObjectValue == null && item.Value.ObjectValue != null)
                    return false;

                if (otherValue.ObjectValue != null && !otherValue.ObjectValue.Equals(item.Value.ObjectValue))
                    return false;
            }

            return true;

        }

        /// <inheritdoc/>
        public override string ToString()
        {
            string text = "(" + items.Count + " references";
            if (items.Count > 0)
            {
                text += ": ";
                text += string.Join(", ", items.Values);
            }
            text += ")";
            return text;
        }

        private static NodeIndex GetKey([NotNull] object keyValuePair)
        {
            var type = keyValuePair.GetType();
            if (!type.IsGenericType || type.GetGenericTypeDefinition() != typeof(KeyValuePair<,>)) throw new ArgumentException("The given object is not a KeyValuePair.");
            var keyProperty = type.GetProperty(nameof(KeyValuePair<object, object>.Key));
            return new NodeIndex(keyProperty.GetValue(keyValuePair));
        }

        private static object GetValue([NotNull] object keyValuePair)
        {
            var type = keyValuePair.GetType();
            if (!type.IsGenericType || type.GetGenericTypeDefinition() != typeof(KeyValuePair<,>)) throw new ArgumentException("The given object is not a KeyValuePair.");
            var valueProperty = type.GetProperty(nameof(KeyValuePair<object, object>.Value));
            return valueProperty.GetValue(keyValuePair);
        }

        /// <summary>
        /// An enumerator for <see cref="ReferenceEnumerable"/> that enumerates in proper item order.
        /// </summary>
        public struct ReferenceEnumerator : IEnumerator<ObjectReference>
        {
            private readonly IEnumerator<NodeIndex> indexEnumerator;
            private ReferenceEnumerable obj;

            public ReferenceEnumerator([NotNull] ReferenceEnumerable obj)
            {
                this.obj = obj;
                indexEnumerator = obj.Indices.GetEnumerator();
            }

            public void Dispose()
            {
                obj = null;
                indexEnumerator.Dispose();
            }

            public bool MoveNext()
            {
                return indexEnumerator.MoveNext();
            }

            public void Reset()
            {
                indexEnumerator.Reset();
            }

            public ObjectReference Current => obj.items[indexEnumerator.Current];

            object IEnumerator.Current => obj.items[indexEnumerator.Current];
        }
    }
}
