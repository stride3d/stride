// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.IO;
using Stride.Core.Serialization.Contents;
using Stride.Core.Streaming;

namespace Stride.Streaming
{
    /// <summary>
    /// Base class for all resources that can be dynamicly streamed.
    /// </summary>
    [DebuggerDisplay("Resource {Storage.Url}; Residency: {CurrentResidency}/{MaxResidency}")]
    public abstract class StreamableResource : ComponentBase
    {
        /// <summary>
        /// The last update time.
        /// </summary>
        internal DateTime LastUpdate;

        /// <summary>
        /// The last target residency change time.
        /// </summary>
        internal DateTime TargetResidencyChange;

        internal int LastTimeUsed;
        protected DatabaseFileProvider fileProvider;
        protected CancellationTokenSource cancellationToken;
        private Task streamingTask;

        protected StreamableResource(StreamingManager manager)
        {
            Manager = manager;
            LastUpdate = TargetResidencyChange = DateTime.MinValue;
        }

        /// <summary>
        /// Gets the manager.
        /// </summary>
        public StreamingManager Manager { get; private set; }

        /// <summary>
        /// Gets the resource storage.
        /// </summary>
        public ContentStorage Storage { get; private set; }

        /// <summary>
        /// Gets the resource object.
        /// </summary>
        public abstract object Resource { get; }

        /// <summary>
        /// Gets the current residency level.
        /// </summary>
        public abstract int CurrentResidency { get; }

        /// <summary>
        /// Gets the allocated residency level.
        /// </summary>
        public abstract int AllocatedResidency { get; }

        /// <summary>
        /// Gets the maximum residency level.
        /// </summary>
        public abstract int MaxResidency { get; }

        /// <summary>
        /// Gets the target residency level.
        /// </summary>
        public int TargetResidency { get; internal set; }

        /// <summary>
        /// Gets a value indicating whether this resource is allocated.
        /// </summary>
        public bool IsAllocated => AllocatedResidency > 0;

        /// <summary>
        /// The current streaming options of the resource.
        /// </summary>
        public StreamingOptions? StreamingOptions;

        /// <summary>
        /// Gets a value indicating whether this resource async task is active.
        /// </summary>
        /// <value>
        ///   <c>true</c> if async task is active; otherwise, <c>false</c>.
        /// </value>
        internal virtual bool IsTaskActive => streamingTask != null && !streamingTask.IsCompleted;

        /// <summary>
        /// Determines whether this instance can be updated. Which means: no async streaming, no pending action in background.
        /// </summary>
        /// <returns><c>true</c> if this instance can be updated; otherwise, <c>false</c>.</returns>
        internal virtual bool CanBeUpdated => streamingTask == null || streamingTask.IsCompleted;

        protected void Init(IDatabaseFileProviderService databaseFileProviderService, ContentStorage storage)
        {
            Storage?.RemoveDisposeBy(this);

            Storage = storage;
            fileProvider = databaseFileProviderService.FileProvider;

            Storage.DisposeBy(this);
        }

        /// <summary>
        /// Calculates the target residency level for this resource based on a given uniform quality.
        /// </summary>
        /// <param name="quality">The quality.</param>
        /// <returns>Target residency.</returns>
        public abstract int CalculateTargetResidency(StreamingQuality quality);

        /// <summary>
        /// Calculates the requested residency level for this resource based on a given target residency.
        /// Resource can control how to change it's residency up/down and if do it at once or in steps, etc..
        /// This gives more control over per resource streaming.
        /// </summary>
        /// <param name="targetResidency">The target residency.</param>
        /// <returns>Requested residency.</returns>
        public abstract int CalculateRequestedResidency(int targetResidency);

        /// <summary>
        /// Stream resource to the target residency level.
        /// </summary>
        /// <param name="residency">The target residency.</param>
        [NotNull]
        internal Task StreamAsyncInternal(int residency)
        {
            Debug.Assert(CanBeUpdated && residency <= MaxResidency, $"CanBeUpdated[{CanBeUpdated}] && residency[{residency}] <= MaxResidency[{MaxResidency}] -- Resouce={Resource}");

            cancellationToken = new CancellationTokenSource();
            return streamingTask = StreamAsync(residency);
        }

        /// <summary>
        /// Flushes the staging data and performs streamed async data synchronization during update on main thread. Safety moment with write access to engine resources.
        /// </summary>
        internal virtual void FlushSync()
        {
        }

        /// <summary>
        /// Releases this resources on StreamingManager shutdown.
        /// </summary>
        internal virtual void Release()
        {
            Dispose();
        }

        /// <inheritdoc />
        protected override void Destroy()
        {
            Manager.UnregisterResource(this);
            Manager = null;

            base.Destroy();
        }

        /// <summary>
        /// Stream resource to the target residency level.
        /// </summary>
        /// <param name="residency">The target residency.</param>
        [NotNull]
        protected abstract Task StreamAsync(int residency);

        /// <summary>
        /// Stops the resource streaming using cancellation token.
        /// </summary>
        public void StopStreaming()
        {
            if (streamingTask != null && !streamingTask.IsCompleted)
            {
                cancellationToken.Cancel();
                if (!streamingTask.IsCompleted)
                {
                    try
                    {
                        streamingTask.Wait();
                    }
                    catch (AggregateException exp)
                    {
                        if (exp.InnerExceptions.Count != 1 || !(exp.InnerExceptions[0] is TaskCanceledException))
                            throw;
                    }
                }
            }
            streamingTask = null;
        }
    }
}
