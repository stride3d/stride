// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Stride.Core.Collections
{
    /// <summary>
    /// Represents a sorted queue, with logarithmic time insertion and deletion.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the queue.</typeparam>
    public class PriorityQueue<T>
    {
        /// <summary>
        /// Underlying list.
        /// </summary>
        private readonly List<T> items = new List<T>();

        /// <summary>
        /// Used to sort and compare elements.
        /// </summary>
        private readonly IComparer<T> comparer;

        public PriorityQueue(IComparer<T> comparer)
        {
            this.comparer = comparer;
        }

        public PriorityQueue()
        {
            this.comparer = Comparer<T>.Default;
        }

        /// <inheritdoc/>
        public void Clear()
        {
            items.Clear();
        }

        /// <inheritdoc/>
        public void Remove(T item)
        {
            // Find index of item to remove.
            var index = items.IndexOf(item);
            if (index == -1)
                return;

            // Remove requested element and place last one instead
            var maxCount = items.Count - 1;
            items[index] = items[maxCount];
            items.RemoveAt(maxCount);

            // Bubble elements so that order is respected again
            var parentIndex = index;
            while (true)
            {
                var childIndex = parentIndex * 2;

                // Check if there is any child, otherwise we're done
                if (childIndex >= maxCount)
                    break;

                // Check which of the two child we need to swap
                if (childIndex + 1 < maxCount && comparer.Compare(items[childIndex + 1], items[childIndex]) < 0)
                    childIndex++;

                // Order might already be OK, if yes we're done
                if (comparer.Compare(items[parentIndex], items[childIndex]) <= 0)
                    break;

                // Need swap
                var tmp = items[childIndex];
                items[childIndex] = items[parentIndex];
                items[parentIndex] = tmp;

                // Continue with child
                parentIndex = childIndex;
            }
        }

        /// <summary>
        /// Adds an object to the <see cref="PriorityQueue{T}"/> and sorts underlying container.
        /// </summary>
        /// <param name="item">The object to add to the queue.</param>
        public void Enqueue(T item)
        {
            // Add element at end of queue
            var childIndex = items.Count;
            items.Add(item);

            // Bubble elements so that order is respected again
            while (childIndex != 0)
            {
                var parentIndex = childIndex / 2;

                // Should we swap with parent?
                if (comparer.Compare(items[childIndex], items[parentIndex]) >= 0)
                    break;

                // Need swap
                var tmp = items[childIndex];
                items[childIndex] = items[parentIndex];
                items[parentIndex] = tmp;

                // Continue with parent
                childIndex = parentIndex;
            }
        }

        /// <inheritdoc/>
        public int Count => items.Count;

        /// <inheritdoc/>
        public bool Empty => items.Count == 0;

        /// <summary>
        /// Returns the object at the beginning of the <see cref="PriorityQueue{T}"/>, without removing it.
        /// </summary>
        /// <returns>The object at the beginning of the <see cref="PriorityQueue{T}"/>.</returns>
        public T Peek()
        {
            return items[0];
        }

        /// <summary>
        /// Removes and returns the object at the beginning of the <see cref="PriorityQueue{T}"/>.
        /// </summary>
        /// <returns>The object at the beginning of the <see cref="PriorityQueue{T}"/>.</returns>
        public T Dequeue()
        {
            // Remove first element and place last one instead
            var val = items[0];
            var maxCount = items.Count - 1;
            items[0] = items[maxCount];
            items.RemoveAt(maxCount);

            // Bubble elements so that order is respected again
            var parentIndex = 0;
            while (true)
            {
                // Check if there is any child, otherwise we're done
                var childIndex = parentIndex * 2;
                if (childIndex >= maxCount)
                    break;

                // Check which of the two child we need to swap
                if (childIndex + 1 < maxCount && comparer.Compare(items[childIndex + 1], items[childIndex]) < 0)
                    childIndex++;

                // Order might already be OK, if yes we're done
                if (comparer.Compare(items[parentIndex], items[childIndex]) <= 0)
                    break;

                // Need swap
                var tmp = items[childIndex];
                items[childIndex] = items[parentIndex];
                items[parentIndex] = tmp;

                // Continue with child
                parentIndex = childIndex;
            }

            // Return first element
            return val;
        }
    }
}
