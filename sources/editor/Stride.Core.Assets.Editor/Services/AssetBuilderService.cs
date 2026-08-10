// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Threading;
using Stride.Core.Assets.Compiler;
using Stride.Core.BuildEngine;
using Stride.Core.Annotations;
using Stride.Core.Collections;
using Stride.Core.Diagnostics;
using Stride.Core.Mathematics;
using Stride.Core.Serialization.Contents;
using Stride.Core.Storage;

namespace Stride.Core.Assets.Editor.Services
{
    public class AssetBuilderService : IBuildService, IDisposable
    {
        private const string IndexName = "AssetBuilderServiceIndex";

        private readonly object queueLock = new object();

        private readonly DynamicBuilder builder;
        private readonly PriorityNodeQueue<AssetBuildUnit> queue = new PriorityNodeQueue<AssetBuildUnit>();
        private readonly Timer retouchTimer;

        // TODO: this is temporary until we have thread local databases (and a better solution for databases used in standard tasks)
        public static readonly object OutOfMicrothreadDatabaseLock = new object();      

        public AssetBuilderService([NotNull] string buildDirectory)
        {
            if (buildDirectory == null) throw new ArgumentNullException(nameof(buildDirectory));

            // We want at least 2 threads, since one will be used for DynamicBuildStep (which is a special blocking step)
            var processorCount = Environment.ProcessorCount;
            var threadCount = MathUtil.Clamp(3*processorCount/4, 2, processorCount - 1);

            // Mount database (otherwise it will be mounted by DynamicBuilder thread, and it might happen too late)
            Builder.OpenObjectDatabase(buildDirectory, IndexName);
            
            var builderInstance = new Builder(GlobalLogger.GetLogger("AssetBuilderService"), buildDirectory, IndexName)
            {
                BuilderName = "AssetBuilderService Builder",
                ThreadCount = threadCount,
            };
            builderInstance.StepProcessed += OnStepProcessed;
            builder = new DynamicBuilder(builderInstance, new AnonymousBuildStepProvider(GetNextBuildStep), "Asset Builder service thread.");
            builder.Start();

            // Re-touch the session's live blobs periodically so a long-idle session (no rebuilds to
            // touch them on-hit) doesn't let its working set age out and get swept by a later GC.
            retouchTimer = new Timer(_ => RetouchLiveSet(), null, FileOdbBackend.TouchThrottle, FileOdbBackend.TouchThrottle);
        }

        public event EventHandler<AssetBuiltEventArgs> AssetBuilt;

        /// <summary>
        ///   The number of build units waiting in the queue to be picked up. Test harnesses use this
        ///   to detect a quiescent state; production code should treat it as informational only.
        /// </summary>
        public int QueuedBuildUnitCount
        {
            get { lock (queueLock) { return queue.Count; } }
        }

        public virtual void Dispose()
        {
            retouchTimer.Dispose();
            builder.Dispose();
        }

        /// <summary>
        /// On each processed command, touches its cache entry + output blobs so the editor's working set stays
        /// warm for mtime-LRU GC and is recorded for periodic re-touch (see <see cref="ObjectDatabase.Touch"/>).
        /// </summary>
        private static void OnStepProcessed(BuildStep step)
        {
            if (step is not CommandBuildStep command || command.Result == null)
                return;

            var database = Builder.ObjectDatabase;
            if (database == null)
                return;

            if (command.CommandHash != ObjectId.Empty)
                database.Touch(command.CommandHash);
            foreach (var output in command.Result.OutputObjects)
            {
                if (output.Key.Type == UrlType.Content)
                    database.Touch(output.Value);
            }
        }

        /// <summary>
        /// Re-touches the objects hit this session (recorded by <see cref="OnStepProcessed"/>), keeping the
        /// working set warm for mtime-LRU GC. Best-effort; throttled per file by <see cref="FileOdbBackend.TouchThrottle"/>.
        /// </summary>
        private void RetouchLiveSet()
        {
            try
            {
                Builder.ObjectDatabase?.RetouchWorkingSet();
            }
            catch (Exception)
            {
                // Best-effort maintenance; never surface on the timer thread.
            }
        }

        private BuildStep GetNextBuildStep(int maxPriority)
        {
            while (true)
            {
                AssetBuildUnit unit;
                lock (queueLock)
                {
                    if (queue.Empty)
                    {
                        return null;
                    }
                    unit = queue.Dequeue();
                }

                // Check that priority is good enough
                if (unit.PriorityMajor > maxPriority)
                    return null;

                var buildStep = unit.GetBuildStep();
                
                // If this build step couldn't be built, let's find another one
                if (buildStep == null)
                    continue;

                // Forward priority to build engine (still very coarse, but should help)
                buildStep.Priority = unit.PriorityMajor;

                foreach (var step in buildStep.EnumerateRecursively())
                {
                    var assetStep = step as AssetBuildStep;
                    if (assetStep != null)
                    {
						assetStep.Priority = unit.PriorityMajor;
                        assetStep.StepProcessed += (s, e) => NotifyAssetBuilt(assetStep.AssetItem, assetStep.Logger);
                    }
                }

                return buildStep;
            }
        }

        public PriorityQueueNode<AssetBuildUnit> PushBuildUnit(AssetBuildUnit unit)
        {
            PriorityQueueNode<AssetBuildUnit> result;

            lock (queueLock)
            {
                result = queue.Enqueue(unit);
            }

            builder.NotifyBuildStepAvailable();

            return result;
        }

        public void RemoveBuildUnit(PriorityQueueNode<AssetBuildUnit> node)
        {
            lock (queueLock)
            {
                queue.Remove(node);
            }
        }

        private void NotifyAssetBuilt(AssetItem assetItem, LoggerResult buildLog)
        {
            AssetBuilt?.Invoke(this, new AssetBuiltEventArgs(assetItem, buildLog));
        }
    }
}
