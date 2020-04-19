// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Threading.Tasks;

namespace TopDownRPG.Core
{
    public static class TaskExtension
    {
        public static async Task<T> InterruptedBy<T>(this Task<T> mainTask, Task interruptingTask, Action<Task> interruptionAction)
        {
            var firstCompleted = await Task.WhenAny(mainTask, interruptingTask);
            if (firstCompleted != mainTask)
            {
                // Interrupted, run action
                interruptionAction(firstCompleted);
                // And return a task that will never complete
                return await new TaskCompletionSource<T>().Task;
            }
            return mainTask.Result;
        }
    }
}
