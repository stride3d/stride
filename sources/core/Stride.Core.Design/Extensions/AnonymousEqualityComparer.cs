// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using Stride.Core.Annotations;

namespace Stride.Core.Extensions
{
    /// <summary>
    /// This class allows implementation of <see cref="IEqualityComparer{T}"/> using anonymous functions.
    /// </summary>
    /// <typeparam name="T">The type of object this comparer can compare.</typeparam>
    public class AnonymousEqualityComparer<T> : IEqualityComparer<T>
    {
        private readonly Func<T, T, bool> equals;
        private readonly Func<T, int> getHashCode;

        /// <summary>
        /// Initializes a new instance of the <see cref="AnonymousEqualityComparer{T}"/> class.
        /// </summary>
        /// <param name="equals">The equality function to use for this equality comparer.</param>
        /// <param name="getHashCode">The function to use to compute hash codes for the objects to compare.</param>
        public AnonymousEqualityComparer([NotNull] Func<T, T, bool> equals, [NotNull] Func<T, int> getHashCode)
        {
            if (equals == null) throw new ArgumentNullException(nameof(equals));
            if (getHashCode == null) throw new ArgumentNullException(nameof(getHashCode));
            this.equals = equals;
            this.getHashCode = getHashCode;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AnonymousEqualityComparer{T}"/> class using the default <see cref="object.GetHashCode"/> method to get hash codes.
        /// </summary>
        /// <param name="equals">The equality function to use for this equality comparer.</param>
        public AnonymousEqualityComparer([NotNull] Func<T, T, bool> equals)
            : this(equals, obj => obj.GetHashCode())
        {
        }

        /// <inheritdoc/>
        public bool Equals(T x, T y)
        {
            return equals(x, y);
        }

        /// <inheritdoc/>
        public int GetHashCode(T obj)
        {
            return getHashCode(obj);
        }
    }
}
