// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
namespace Xenko.Core.Collections
{
    /// <summary>
    /// Represents a node in a priority queue, to allow O(n) removal.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class PriorityQueueNode<T>
    {
        public T Value;

        public int Index { get; internal set; }

        public PriorityQueueNode(T value)
        {
            Value = value;
            Index = -1;
        }
    }
}
