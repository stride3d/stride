// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using Xenko.Core.Annotations;

namespace Xenko.Core.Presentation.Dirtiables
{
    public interface IDirtyingOperation
    {
        /// <summary>
        /// Gets whether this operation is currently realized.
        /// </summary>
        bool IsDone { get; }

        /// <summary>
        /// Gets the dirtiable objects associated to this operation, or <c>null</c> if no dirtiable is associated.
        /// </summary>
        [NotNull]
        IReadOnlyList<IDirtiable> Dirtiables { get; }
    }
}
