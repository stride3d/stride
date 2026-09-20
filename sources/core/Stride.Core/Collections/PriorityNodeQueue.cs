// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core.Collections;

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
/// in our library. The tree is implemented atop a list, where 2N+1 and 2N+2 are
/// the child nodes of node N. The tree is balanced and left-aligned so there
/// are no 'holes' in this list.
/// </summary>
/// <typeparam name="T">Type T.</typeparam>
public class PriorityNodeQueue<T>
{
    /// <summary>The List we use for implementation.</summary>
    private readonly List<PriorityQueueNode<T>> items = [];

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
        foreach (var item in items)
            item.Index = -1;

        items.Clear();
    }

    /// <summary>
    /// Removes the specified item.
    /// </summary>
    /// <param name="item">The item to remove.</param>
    public void Remove(PriorityQueueNode<T> item)
    {
        var index = item.Index;
        if (index == -1)
            return;

        // The item leaves a 'hole', which the last item of the list fills up
        var lastIndex = items.Count - 1;
        var lastItem = items[lastIndex];
        items.RemoveAt(lastIndex);
        item.Index = -1;

        if (index == lastIndex)
            return;

        items[index] = lastItem;
        lastItem.Index = index;

        // The item taken from the end can be smaller than its new parent, or bigger than its children
        if (!MoveUp(index))
            MoveDown(index);
    }

    /// <summary>Add an element to the priority queue - O(log(n)) time operation.</summary>
    /// <param name="item">The item to be added to the queue</param>
    /// <returns>A node representing the item.</returns>
    public PriorityQueueNode<T> Enqueue(T item)
    {
        var result = new PriorityQueueNode<T>(item);
        Enqueue(result);
        return result;
    }

    /// <summary>Add an element to the priority queue - O(log(n)) time operation.</summary>
    /// <param name="item">The item to be added to the queue</param>
    public void Enqueue(PriorityQueueNode<T> item)
    {
        if (item.Index != -1)
            throw new InvalidOperationException("Item belongs to another PriorityNodeQueue.");

        // The item goes at the bottom of the tree, then moves up to its level
        item.Index = items.Count;
        items.Add(item);

        MoveUp(item.Index);
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
        var item = items[0];
        var value = item.Value;

        Remove(item);

        return value;
    }

    /// <summary>
    /// Moves an item towards the head of the queue, while it is smaller than its parent.
    /// </summary>
    /// <param name="index">The index of the item to move.</param>
    /// <returns><c>true</c> if the item moved; otherwise, <c>false</c>.</returns>
    private bool MoveUp(int index)
    {
        var moved = false;
        while (index > 0)
        {
            var parent = (index - 1) / 2;
            if (comparer.Compare(items[index].Value, items[parent].Value) >= 0)
                break;

            Swap(index, parent);
            index = parent;
            moved = true;
        }

        return moved;
    }

    /// <summary>
    /// Moves an item towards the tail of the queue, while it is bigger than one of its children.
    /// </summary>
    /// <param name="index">The index of the item to move.</param>
    private void MoveDown(int index)
    {
        while (true)
        {
            // Stop when the item has no child left
            var child = (index * 2) + 1;
            if (child >= items.Count)
                break;

            // Of the two children, the smaller one takes the place of the parent
            var rightChild = child + 1;
            if (rightChild < items.Count && comparer.Compare(items[rightChild].Value, items[child].Value) < 0)
                child = rightChild;

            if (comparer.Compare(items[index].Value, items[child].Value) <= 0)
                break;

            Swap(index, child);
            index = child;
        }
    }

    private void Swap(int first, int second)
    {
        (items[first], items[second]) = (items[second], items[first]);
        items[first].Index = first;
        items[second].Index = second;
    }
}
