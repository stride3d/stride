// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Stride.Core.Annotations;

namespace Stride.Core.Extensions
{
    public static class ProcessExtensions
    {
        /// <summary>
        /// Waits asynchronously for the process to exit.
        /// </summary>
        /// <param name="process">The process to wait for cancellation.</param>
        /// <param name="cancellationToken">A cancellation token. If invoked, the task will return
        /// immediately as cancelled.</param>
        /// <returns>A Task representing waiting for the process to end.</returns>
        public static Task WaitForExitAsync([NotNull] this Process process, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Source: https://stackoverflow.com/questions/139593/processstartinfo-hanging-on-waitforexit-why/39872058#39872058
            process.EnableRaisingEvents = true;

            var taskCompletionSource = new TaskCompletionSource<object>();

            EventHandler handler = null;
            handler = (sender, args) =>
            {
                process.Exited -= handler;
                taskCompletionSource.TrySetResult(null);
            };
            process.Exited += handler;

            if (cancellationToken != default(CancellationToken))
            {
                cancellationToken.Register(
                    () =>
                    {
                        process.Exited -= handler;
                        taskCompletionSource.TrySetCanceled();
                    });
            }

            return taskCompletionSource.Task;
        }
    }
}
