// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;

namespace Stride.Updater
{
    /// <summary>
    /// Implementation of <see cref="EnterChecker"/> for <see cref="IList{T}"/>.
    /// </summary>
    internal class ListEnterChecker<T> : EnterChecker
    {
        private readonly int minimumCount;

        public ListEnterChecker(int minimumCount)
        {
            this.minimumCount = minimumCount;
        }

        /// <inheritdoc/>
        public override bool CanEnter(object obj)
        {
            return minimumCount <= ((IList<T>)obj).Count;
        }
    }
}
