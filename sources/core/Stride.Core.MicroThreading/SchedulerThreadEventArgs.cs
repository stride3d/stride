// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Stride.Core.MicroThreading
{
    /// <summary>
    /// Provides data for the <see cref="Scheduler.MicroThreadStarted"/>, <see cref="Scheduler.MicroThreadEnded"/>, <see cref="Scheduler.MicroThreadCallbackStart"/> and <see cref="Scheduler.MicroThreadCallbackEnd"/> events.
    /// </summary>
    public class SchedulerThreadEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the <see cref="MicroThread"/> this event concerns.
        /// </summary>
        /// <value>
        /// The micro thread.
        /// </value>
        public MicroThread MicroThread { get; private set; }

        /// <summary>
        /// Gets the <see cref="System.Threading.Thread.ManagedThreadId"/> active when this event happened.
        /// </summary>
        /// <value>
        /// The managed thread identifier.
        /// </value>
        public int ThreadId { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SchedulerThreadEventArgs"/> class.
        /// </summary>
        /// <param name="microThread">The micro thread.</param>
        /// <param name="threadId">The managed thread identifier.</param>
        public SchedulerThreadEventArgs(MicroThread microThread, int threadId)
        {
            MicroThread = microThread;
            ThreadId = threadId;
        }
    }
}
