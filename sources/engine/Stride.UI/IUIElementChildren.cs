// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;

namespace Xenko.UI
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
