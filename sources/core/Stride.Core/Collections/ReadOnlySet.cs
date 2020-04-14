// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Stride.Core.Collections
{
    /// <summary>
    /// Represents a strongly-typed, read-only set of element.
    /// </summary>
    /// <typeparam name="T">The type of the elements.</typeparam>
    public class ReadOnlySet<T> : IReadOnlySet<T>
    {
        private readonly ISet<T> innerSet;

        public ReadOnlySet(ISet<T> innerSet)
        {
            this.innerSet = innerSet;
        }

        public bool Contains(T item)
        {
            return innerSet.Contains(item);
        }

        public int Count => innerSet.Count;

        public IEnumerator<T> GetEnumerator()
        {
            return innerSet.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return innerSet.GetEnumerator();
        }
    }
}
