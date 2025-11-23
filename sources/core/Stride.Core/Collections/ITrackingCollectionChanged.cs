// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stride.Core.Collections;
public interface ITrackingCollectionChanged<TValue>
{
    /// <summary>
    /// Occurs when [collection changed].
    /// </summary>
    /// Called as is when adding an item, and in reverse-order when removing an item.
    event EventHandler<TrackingCollectionChangedEventArgs<TValue>> CollectionChanged;
}
