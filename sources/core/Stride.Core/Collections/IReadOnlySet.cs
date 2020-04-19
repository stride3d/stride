// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;

namespace Stride.Core.Collections
{
    /// <summary>
    /// Represents a strongly-typed, read-only set of element.
    /// </summary>
    /// <typeparam name="T">The type of the elements.</typeparam>
    public interface IReadOnlySet<T> : IReadOnlyCollection<T>
    {
        /// <summary>
        /// Determines whether the set [contains] [the specified item].
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>
        ///   <c>true</c> if the set [contains] [the specified item]; otherwise, <c>false</c>.
        /// </returns>
        bool Contains(T item);
    }
}
