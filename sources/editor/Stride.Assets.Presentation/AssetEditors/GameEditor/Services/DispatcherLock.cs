// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Stride.Core.Annotations;
using Stride.Core.Extensions;
using Stride.Core.Presentation.Services;

namespace Stride.Assets.Presentation.AssetEditors.GameEditor.Services
{
    /// <summary>
    /// An object that allows to lock several dispatchers at the same-time to perform an operation in a thread-safe way.
    /// </summary>
    public class DispatcherLock : IDisposable
    {
        // TODO: we might want to move that to the plugin level at some point (or even above? in Presentation?)
        private struct DispatcherState
        {
            public DispatcherState([NotNull] IDispatcherService dispatcher)
            {
                if (dispatcher == null) throw new ArgumentNullException(nameof(dispatcher));
                if (dispatcher.CheckAccess()) throw new InvalidOperationException("A dispatcher lock must be created from a different thread that the dispatchers it should lock");
                Dispatcher = dispatcher;
                Locked = new TaskCompletionSource<int>();
                Unlocked = new TaskCompletionSource<int>();
            }

            public readonly IDispatcherService Dispatcher;
            public readonly TaskCompletionSource<int> Locked;
            public readonly TaskCompletionSource<int> Unlocked;
        }

        private readonly List<DispatcherState> dispatcherStates;

        /// <summary>
        /// Initializes a new instance of the <see cref="DispatcherLock"/> class.
        /// </summary>
        /// <param name="dispatchers">The dispatchers to lock.</param>
        private DispatcherLock([ItemNotNull, NotNull] IEnumerable<IDispatcherService> dispatchers)
        {
            dispatcherStates = dispatchers.Select(x => new DispatcherState(x)).ToList();
        }

        /// <summary>
        /// Disposes this lock. This will unlock all dispatchers.
        /// </summary>
        public void Dispose()
        {
            // Unlock all dispatchers.
            foreach (var dispatcher in dispatcherStates)
            {
                dispatcher.Unlocked.SetResult(0);
            }
        }

        /// <summary>
        /// Creates a new instance of the <see cref="DispatcherLock"/> class.
        /// </summary>
        /// <param name="dispatchers">The dispatchers to lock.</param>
        /// <returns>The newly created instance of the <see cref="DispatcherLock"/> class.</returns>
        /// <remarks>
        /// Dispatchers are unlocked when the returned <see cref="DispatcherLock"/> is disposed.
        /// </remarks>
        [ItemNotNull, NotNull]
        public static Task<DispatcherLock> Lock([ItemNotNull, NotNull] params IDispatcherService[] dispatchers)
        {
            return Lock(false, dispatchers);
        }

        /// <summary>
        /// Creates a new instance of the <see cref="DispatcherLock"/> class.
        /// </summary>
        /// <param name="dispatchers">The dispatchers to lock.</param>
        /// <param name="lockSequencially">If true, dispatchers will be locked sequencially, in the order they are enumerated. This allows to lock critical dispatchers in last.</param>
        /// <returns>The newly created instance of the <see cref="DispatcherLock"/> class.</returns>
        /// <remarks>
        /// Dispatchers are unlocked when the returned <see cref="DispatcherLock"/> is disposed.
        /// </remarks>
        [ItemNotNull, NotNull]
        public static async Task<DispatcherLock> Lock(bool lockSequencially, [ItemNotNull, NotNull]  params IDispatcherService[] dispatchers)
        {
            if (dispatchers == null) throw new ArgumentNullException(nameof(dispatchers));

            var lok = new DispatcherLock(dispatchers);
            foreach (var dispatcher in lok.dispatcherStates)
            {
                dispatcher.Dispatcher.InvokeAsync(() =>
                {
                    // This dispatcher is now locked...
                    dispatcher.Locked.SetResult(0);
                    // ... and blocked until it's unlocked
                    dispatcher.Unlocked.Task.Wait();
                }).Forget();

                if (lockSequencially)
                {
                    // Wait for this dispatcher to be locked before proceeding, if sequencial locking was requested.
                    await dispatcher.Locked.Task;
                }
            }

            // Wait for all the dispatcher to be locked.
            await Task.WhenAll(lok.dispatcherStates.Select(x => x.Locked.Task).Cast<Task>());
            return lok;
        }
    }
}
