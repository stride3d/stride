// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Stride.Core.MicroThreading
{
    public class AsyncAutoResetEvent
    {
        // Credit: http://blogs.msdn.com/b/pfxteam/archive/2012/02/11/10266923.aspx
        private static readonly Task Completed = Task.FromResult(true);
        private readonly Queue<TaskCompletionSource<bool>> waits = new Queue<TaskCompletionSource<bool>>();
        private bool signaled;

        public Task WaitAsync()
        {
            lock (waits)
            {
                if (signaled)
                {
                    signaled = false;
                    return Completed;
                }
                else
                {
                    var tcs = new TaskCompletionSource<bool>();
                    waits.Enqueue(tcs);
                    return tcs.Task;
                }
            }
        }

        public void Set()
        {
            TaskCompletionSource<bool> toRelease = null;
            lock (waits)
            {
                if (waits.Count > 0)
                    toRelease = waits.Dequeue();
                else if (!signaled)
                    signaled = true;
            }
            if (toRelease != null)
                toRelease.SetResult(true);
        }
    }
}
