// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;

namespace Stride.UI
{
    /// <summary>
    /// Interfaces representing an <see cref="UIElement"/> containing child elements.
    /// </summary>
    public interface IUIElementChildren
    {
        /// <summary>
        /// Gets the children of this element.
        /// </summary>
        IEnumerable<IUIElementChildren> Children { get; }
    }
}
