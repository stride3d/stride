// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;

namespace Stride.Core.Assets.Editor.ViewModel
{
    public interface IDragSourceViewModel
    {
        event EventHandler<EventArgs> DragRequested;

        IEnumerable<object> GetItemsToDrag();
    }
}
