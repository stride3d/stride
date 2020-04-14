// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Collections;
using Stride.Core.MicroThreading;

namespace Stride.Engine
{
    public abstract class StartupScript : ScriptComponent
    {
        internal PriorityQueueNode<SchedulerEntry> StartSchedulerNode;

        /// <summary>
        /// Called before the script enters it's update loop.
        /// </summary>
        public virtual void Start()
        {
        }
    }
}
