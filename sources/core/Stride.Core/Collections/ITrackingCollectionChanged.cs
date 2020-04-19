// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

namespace Stride.Core.Collections
{
    public interface ITrackingCollectionChanged
    {
        /// <summary>
        /// Occurs when [collection changed].
        /// </summary>
        /// Called as is when adding an item, and in reverse-order when removing an item.
        event EventHandler<TrackingCollectionChangedEventArgs> CollectionChanged;
    }
}
