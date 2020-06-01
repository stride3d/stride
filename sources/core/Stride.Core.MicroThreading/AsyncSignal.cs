// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Stride.Core.MicroThreading
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
