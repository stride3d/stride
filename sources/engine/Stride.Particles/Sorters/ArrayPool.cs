// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stride.Particles.Sorters
{
    public class ArrayPool<T> where T : struct
    {
        private readonly Dictionary<int, Stack<T[]>> _pool = new Dictionary<int, Stack<T[]>>();

        public readonly T[] Empty = new T[0];

        public virtual void Clear()
        {
            _pool.Clear();
        }

        public virtual T[] Allocate(int size)
        {
            if (size < 0) throw new ArgumentOutOfRangeException(nameof(size), "Must be positive.");

            if (size == 0) return Empty;

            Stack<T[]> candidates;
            return _pool.TryGetValue(size, out candidates) && candidates.Count > 0 ? candidates.Pop() : new T[size];
        }

        public virtual void Free(T[] array)
        {
            if (array == null) throw new ArgumentNullException(nameof(array));

            if (array.Length == 0) return;

            Stack<T[]> candidates;
            if (!_pool.TryGetValue(array.Length, out candidates))
                _pool.Add(array.Length, candidates = new Stack<T[]>());
            candidates.Push(array);
        }
    }

    public class ConcurrentArrayPool<T> : ArrayPool<T> where T : struct
    {
        public override T[] Allocate(int size)
        {
            lock (this)
            {
                return base.Allocate(size);
            }
        }

        public override void Free(T[] array)
        {
            lock (this)
            {
                base.Free(array);
            }
        }

        public override void Clear()
        {
            lock (this)
            {
                base.Clear();
            }
        }
    }
}
