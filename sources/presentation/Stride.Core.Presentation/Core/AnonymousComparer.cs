// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using Stride.Core.Annotations;

namespace Stride.Core.Presentation.Core
{
    /// <summary>
    /// This class allows implementation of <see cref="IComparer{T}"/> using an anonymous function.
    /// </summary>
    /// <typeparam name="T">The type of object this comparer can compare.</typeparam>
    public class AnonymousComparer<T> : IComparer<T>
    {
        private readonly Func<T, T, int> compare;

        /// <summary>
        /// Initializes a new instance of the <see cref="AnonymousComparer{T}"/> class.
        /// </summary>
        /// <param name="compare">The comparison function to use for this comparer.</param>
        public AnonymousComparer([NotNull] Func<T, T, int> compare)
        {
            if (compare == null) throw new ArgumentNullException(nameof(compare));
            this.compare = compare;
        }

        /// <inheritdoc/>
        public int Compare(T x, T y)
        {
            return compare(x, y);
        }
    }
}
