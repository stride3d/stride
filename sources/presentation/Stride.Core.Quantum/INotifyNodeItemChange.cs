// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

namespace Stride.Core.Quantum
{
    /// <summary>
    /// An interface representing an object notifying changes when an item in the value of a related node is modified, added or removed.
    /// </summary>
    public interface INotifyNodeItemChange
    {
        /// <summary>
        /// Raised just before a change to the related node occurs.
        /// </summary>
        event EventHandler<ItemChangeEventArgs> ItemChanging;

        /// <summary>
        /// Raised when a change to the related node has occurred.
        /// </summary>
        event EventHandler<ItemChangeEventArgs> ItemChanged;
    }
}
