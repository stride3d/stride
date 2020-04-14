// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Xenko.Core.Assets.Compiler;
using Xenko.Core.BuildEngine;
using Xenko.Core.Annotations;
using Xenko.Core.Collections;
using Xenko.Core.Diagnostics;
using Xenko.Core.Mathematics;

namespace Xenko.Core.Assets.Editor.Services
{
    public class AssetBuilderService : IBuildService, IDisposable
    {
        private const string IndexName = "AssetBuilderServiceIndex";

        private readonly object queueLock = new object();

        private readonly DynamicBuilder builder;
        private readonly PriorityNodeQueue<AssetBuildUnit> queue = new PriorityNodeQueue<AssetBuildUnit>();

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
            builder = new DynamicBuilder(builderInstance, new AnonymousBuildStepProvider(GetNextBuildStep), "Asset Builder service thread.");
            builder.Start();
        }

        public event EventHandler<AssetBuiltEventArgs> AssetBuilt;

        public virtual void Dispose()
        {
            builder.Dispose();
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
