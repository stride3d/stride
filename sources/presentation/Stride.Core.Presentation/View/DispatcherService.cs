// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using Stride.Core.Annotations;
using Stride.Core.Presentation.Services;

namespace Stride.Core.Presentation.View
{
    /// <summary>
    /// This class is the implementation of the <see cref="IDispatcherService"/> interface for WPF.
    /// </summary>
    public class DispatcherService : IDispatcherService
    {
        private readonly Dispatcher dispatcher;

        /// <summary>
        /// Creates a new instance of the <see cref="DispatcherService"/> class using the dispatcher of the current thread.
        /// </summary>
        /// <returns></returns>
        [NotNull]
        public static DispatcherService Create()
        {
            return new DispatcherService(Dispatcher.CurrentDispatcher);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DispatcherService"/> class using the associated dispatcher.
        /// </summary>
        /// <param name="dispatcher">The dispatcher to use for this instance of <see cref="DispatcherService"/>.</param>
        public DispatcherService([NotNull] Dispatcher dispatcher)
        {
            if (dispatcher == null) throw new ArgumentNullException(nameof(dispatcher));
            this.dispatcher = dispatcher;
        }

        /// <inheritdoc/>
        public void Invoke(Action callback)
        {
            if (CheckAccess())
            {
                callback();
            }
            else
            {
                dispatcher.Invoke(callback);
            }
        }

        /// <inheritdoc/>
        public TResult Invoke<TResult>(Func<TResult> callback)
        {
            return CheckAccess() ? callback() : dispatcher.Invoke(callback);
        }

        /// <inheritdoc/>
        public Task InvokeAsync(Action callback)
        {
            var operation = dispatcher.InvokeAsync(callback);
            return operation.Task;
        }

        /// <inheritdoc/>
        public Task LowPriorityInvokeAsync(Action callback)
        {
            var operation = dispatcher.InvokeAsync(callback, DispatcherPriority.ApplicationIdle);
            return operation.Task;
        }

        /// <inheritdoc/>
        public Task<TResult> InvokeAsync<TResult>(Func<TResult> callback)
        {
            var operation = dispatcher.InvokeAsync(callback);
            return operation.Task;
        }

        /// <inheritdoc/>
        public Task InvokeTask(Func<Task> task)
        {
            return InvokeTask(dispatcher, task);
        }

        /// <inheritdoc/>
        public Task<TResult> InvokeTask<TResult>(Func<Task<TResult>> task)
        {
            return InvokeTask(dispatcher, task);
        }

        [NotNull]
        public static Task InvokeTask([NotNull] Dispatcher dispatcher, Func<Task> task)
        {
            var tcs = new TaskCompletionSource<int>();
            dispatcher.InvokeAsync(async () => { await task(); tcs.SetResult(0); });
            return tcs.Task;
        }

        [NotNull]
        public static Task<TResult> InvokeTask<TResult>([NotNull] Dispatcher dispatcher, Func<Task<TResult>> task)
        {
            var tcs = new TaskCompletionSource<TResult>();
            dispatcher.InvokeAsync(async () => tcs.SetResult(await task()));
            return tcs.Task;
        }

        /// <inheritdoc/>
        public bool CheckAccess()
        {
            return Thread.CurrentThread == dispatcher.Thread;
        }

        /// <inheritdoc/>
        public void EnsureAccess(bool inDispatcherThread = true)
        {
            if (inDispatcherThread && Thread.CurrentThread != dispatcher.Thread)
                throw new InvalidOperationException("The current thread was expected to be the dispatcher thread.");
            if (!inDispatcherThread && Thread.CurrentThread == dispatcher.Thread)
                throw new InvalidOperationException("The current thread was expected to be different from the dispatcher thread.");
        }
    }
}
