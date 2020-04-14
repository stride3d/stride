// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Collections;
using Stride.Core.MicroThreading;

namespace Stride.Engine
{
    /// <summary>
    /// A script whose <see cref="Update"/> will be called every frame.
    /// </summary>
    public abstract class SyncScript : StartupScript
    {
        internal PriorityQueueNode<SchedulerEntry> UpdateSchedulerNode;

        /// <summary>
        /// Called every frame.
        /// </summary>
        public abstract void Update();
    }
}
