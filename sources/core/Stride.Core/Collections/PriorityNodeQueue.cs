// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using Xenko.Core.Annotations;

namespace Xenko.Core.Collections
{
    /// <summary>
    /// Implements a priority queue of type T.
    /// 
    /// Elements may be added to the queue in any order, but when we pull
    /// elements out of the queue, they will be returned in 'ascending' order.
    /// Adding new elements into the queue may be done at any time, so this is
    /// useful to implement a dynamically growing and shrinking queue. Both adding
    /// an element and removing the first element are log(N) operations. 
    /// 
    /// The queue is implemented using a priority-heap data structure. For more 
    /// details on this elegant and simple data structure see "Programming Pearls"
    /// in our library. The tree is implemented atop a list, where 2N and 2N+1 are
    /// the child nodes of node N. The tree is balanced and left-aligned so there
    /// are no 'holes' in this list.
    /// </summary>
    /// <typeparam name="T">Type T.</typeparam>
    public class PriorityNodeQueue<T>
    {
        /// <summary>The List we use for implementation.</summary>
        private readonly List<PriorityQueueNode<T>> items = new List<PriorityQueueNode<T>>();

        // Used for comparing and sorting elements.
        private readonly IComparer<T> comparer;

        public PriorityNodeQueue(IComparer<T> comparer)
        {
            this.comparer = comparer;
        }

        public PriorityNodeQueue()
        {
            this.comparer = Comparer<T>.Default;
        }

        /// <summary>Clear all the elements from the priority queue</summary>
        public void Clear()
        {
            items.Clear();
        }

        /// <summary>
        /// Removes the specified item.
        /// </summary>
        /// <param name="item">The item to remove.</param>
        public void Remove([NotNull] PriorityQueueNode<T> item)
        {
            var index = item.Index;
            if (index == -1)
                return;

            // The element to return is of course the first element in the array, 
            // or the root of the tree. However, this will leave a 'hole' there. We
            // fill up this hole with the last element from the array. This will 
            // break the heap property. So we bubble the element downwards by swapping
            // it with it's lower child until it reaches it's correct level. The lower
            // child (one of the orignal elements with index 1 or 2) will now be at the
            // head of the queue (root of the tree).
            var nMax = items.Count - 1;
            var itemMax = items[nMax];
            itemMax.Index = index;
            var itemToRemove = items[index];
            itemToRemove.Index = -1;
            items[index] = itemMax;
            items.RemoveAt(nMax);  // Move the last element to the top

            var p = index;
            while (true)
            {
                // c is the child we want to swap with. If there
                // is no child at all, then the heap is balanced
                var c = p * 2;
                if (c >= nMax) break;

                // If the second child is smaller than the first, that's the one
                // we want to swap with this parent.
                if (c + 1 < nMax && comparer.Compare(items[c + 1].Value, items[c].Value) < 0) c++;
                // If the parent is already smaller than this smaller child, then
                // we are done
                if (comparer.Compare(items[p].Value, items[c].Value) <= 0)
                    break;

                // Othewise, swap parent and child, and follow down the parent
                var tmp = items[p];
                var tmp2 = items[c];
                tmp.Index = c;
                tmp2.Index = p;
                items[p] = tmp2;
                items[c] = tmp;
                p = c;
            }

            item.Index = -1;
        }

        /// <summary>Add an element to the priority queue - O(log(n)) time operation.</summary>
        /// <param name="item">The item to be added to the queue</param>
        /// <returns>A node representing the item.</returns>
        [NotNull]
        public PriorityQueueNode<T> Enqueue(T item)
        {
            var result = new PriorityQueueNode<T>(item);
            Enqueue(result);
            return result;
        }

        /// <summary>Add an element to the priority queue - O(log(n)) time operation.</summary>
        /// <param name="item">The item to be added to the queue</param>
        public void Enqueue([NotNull] PriorityQueueNode<T> item)
        {
            if (item.Index != -1)
                throw new InvalidOperationException("Item belongs to another PriorityNodeQueue.");

            // We add the item to the end of the list (at the bottom of the
            // tree). Then, the heap-property could be violated between this element
            // and it's parent. If this is the case, we swap this element with the 
            // parent (a safe operation to do since the element is known to be less
            // than it's parent). Now the element move one level up the tree. We repeat
            // this test with the element and it's new parent. The element, if lesser
            // than everybody else in the tree will eventually bubble all the way up
            // to the root of the tree (or the head of the list). It is easy to see 
            // this will take log(N) time, since we are working with a balanced binary
            // tree.
            var n = items.Count;
            items.Add(item);
            item.Index = n;
            while (n != 0)
            {
                var p = n / 2;    // This is the 'parent' of this item
                if (comparer.Compare(items[n].Value, items[p].Value) >= 0)
                    break;  // Item >= parent

                // Swap item and parent
                var tmp = items[n];
                var tmp2 = items[p];
                tmp2.Index = n;
                tmp.Index = p;
                items[n] = tmp2;
                items[p] = tmp;

                n = p;            // And continue
            }
        }

        /// <summary>Returns the number of elements in the queue.</summary>
        public int Count => items.Count;

        /// <summary>Returns true if the queue is empty.</summary>
        /// Trying to call Peek() or Next() on an empty queue will throw an exception.
        /// Check using Empty first before calling these methods.
        public bool Empty => items.Count == 0;

        /// <summary>Allows you to look at the first element waiting in the queue, without removing it.</summary>
        /// This element will be the one that will be returned if you subsequently call Next().
        public T Peek()
        {
            return items[0].Value;
        }

        /// <summary>Removes and returns the first element from the queue (least element)</summary>
        /// <returns>The first element in the queue, in ascending order.</returns>
        public T Dequeue()
        {
            // The element to return is of course the first element in the array, 
            // or the root of the tree. However, this will leave a 'hole' there. We
            // fill up this hole with the last element from the array. This will 
            // break the heap property. So we bubble the element downwards by swapping
            // it with it's lower child until it reaches it's correct level. The lower
            // child (one of the orignal elements with index 1 or 2) will now be at the
            // head of the queue (root of the tree).
            var nMax = items.Count - 1;
            var itemToRemove = items[0];
            var itemMax = items[nMax];
            itemMax.Index = 0;
            var val = itemToRemove.Value;
            itemToRemove.Index = -1;
            items[0] = itemMax;
            items.RemoveAt(nMax);  // Move the last element to the top

            var p = 0;
            while (true)
            {
                // c is the child we want to swap with. If there
                // is no child at all, then the heap is balanced
                var c = p * 2;
                if (c >= nMax) break;

                // If the second child is smaller than the first, that's the one
                // we want to swap with this parent.
                if (c + 1 < nMax && comparer.Compare(items[c + 1].Value, items[c].Value) < 0) c++;
                // If the parent is already smaller than this smaller child, then
                // we are done
                if (comparer.Compare(items[p].Value, items[c].Value) <= 0)
                    break;

                // Othewise, swap parent and child, and follow down the parent
                var tmp = items[p];
                var tmp2 = items[c];
                tmp.Index = c;
                tmp2.Index = p;
                items[p] = tmp2;
                items[c] = tmp;
                
                p = c;
            }

            return val;
        }
    }
}
