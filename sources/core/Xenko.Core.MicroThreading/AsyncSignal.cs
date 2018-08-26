// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using System.Threading.Tasks;

#if NET45
using TaskEx = System.Threading.Tasks.Task;
#endif

namespace Xenko.Core.MicroThreading
{
    public class AsyncSignal
    {
        private TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();

        public Task WaitAsync()
        {
            lock (this)
            {
                tcs = new TaskCompletionSource<bool>();
                var result = tcs.Task;
                return result;
            }
        }

        public void Set()
        {
            lock (this)
            {
                tcs.TrySetResult(true);
            }
        }
    }
}
