// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Stride.Core;
using Stride.Core.MicroThreading;

namespace Stride.Engine
{
    /// <summary>
    /// A script which can be implemented as an async microthread.
    /// </summary>
    public abstract class AsyncScript : ScriptComponent
    {
        [DataMemberIgnore]
        internal MicroThread MicroThread;

        [DataMemberIgnore]
        internal CancellationTokenSource CancellationTokenSource;

        /// <summary>
        /// Gets a token indicating if the script execution was canceled.
        /// </summary>
        public CancellationToken CancellationToken => MicroThread.CancellationToken;

        /// <summary>
        /// Called once, as a microthread
        /// </summary>
        /// <returns></returns>
        public abstract Task Execute();

        protected internal override void PriorityUpdated()
        {
            base.PriorityUpdated();

            // Update micro thread priority
            if (MicroThread != null)
                MicroThread.Priority = Priority;
        }
    }
}
