// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core.MicroThreading;

public class AsyncSignal
{
    private TaskCompletionSource<bool> tcs = new();
    private readonly object lockObject = new();

    public Task WaitAsync()
    {
        lock (lockObject)
        {
            tcs = new TaskCompletionSource<bool>();
            return tcs.Task;
        }
    }

    public void Set()
    {
        lock (lockObject)
        {
            tcs.TrySetResult(true);
        }
    }
}
