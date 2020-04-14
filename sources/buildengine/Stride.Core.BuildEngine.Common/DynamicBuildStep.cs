// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Xenko.Core.BuildEngine
{
    public class DynamicBuildStep : BuildStep
    {
        private readonly IBuildStepProvider buildStepProvider;

        /// <summary>
        /// The <see cref="AutoResetEvent"/> used to notify the dynamic build step that new work is requested.
        /// </summary>
        private readonly AutoResetEvent newWorkAvailable = new AutoResetEvent(false);

        public DynamicBuildStep(IBuildStepProvider buildStepProvider, int maxParallelSteps)
        {
            this.buildStepProvider = buildStepProvider;
            MaxParallelSteps = maxParallelSteps;
            Priority = int.MinValue; // Highest priority
        }

        /// <summary>
        /// Gets or sets the maximum number of steps that can run at the same time in parallel.
        /// </summary>
        public int MaxParallelSteps { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of steps slots that are kept specifically for high priority steps (negative)
        /// </summary>
        public int MaxHighPriorityParallelSteps { get; set; }

        /// <inheritdoc />
        public override string Title => ToString();

        /// <summary>
        /// Notify the dynamic build step new work is available.
        /// </summary>
        public void NotifyNewWorkAvailable()
        {
            newWorkAvailable.Set();
        }

        public override async Task<ResultStatus> Execute(IExecuteContext executeContext, BuilderContext builderContext)
        {
            var buildStepsToWait = new List<BuildStep>();

            while (true)
            {
                // interrupt the build if cancellation is required.
                if (executeContext.CancellationTokenSource.Token.IsCancellationRequested)
                    return ResultStatus.Cancelled;

                // Clean completed build steps
                for (int index = buildStepsToWait.Count - 1; index >= 0; index--)
                {
                    var buildStepToWait = buildStepsToWait[index];
                    if (buildStepToWait.Processed)
                        buildStepsToWait.RemoveAt(index);
                }

                // wait for a task to complete
                if (buildStepsToWait.Count >= MaxParallelSteps)
                    await CompleteOneBuildStep(executeContext, buildStepsToWait);

                // Should we check for all tasks or only high priority tasks? (priority < 0)
                bool checkOnlyForHighPriorityTasks = buildStepsToWait.Count >= MaxParallelSteps;

                // Transform item into build step
                var buildStep = buildStepProvider.GetNextBuildStep(checkOnlyForHighPriorityTasks ? -1 : int.MaxValue);

                // No job => passively wait
                if (buildStep == null)
                {
                    newWorkAvailable.WaitOne();
                    continue;
                }

                // Safeguard if the provided build step is already processed
                if (buildStep.Processed)
                    continue;

                // Schedule build step
                executeContext.ScheduleBuildStep(buildStep);
                buildStepsToWait.Add(buildStep);
            }
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return "DynamicBuildStep";
        }

        private async Task CompleteOneBuildStep(IExecuteContext executeContext, List<BuildStep> buildStepsToWait)
        {
            // Too many build steps, wait for one to finish
            var waitHandles = buildStepsToWait.Select(x => ((IAsyncResult)x.ExecutedAsync()).AsyncWaitHandle);

            // Should we listen for new high priority tasks?
            if (buildStepsToWait.Count >= MaxParallelSteps && buildStepsToWait.Count < MaxParallelSteps + MaxHighPriorityParallelSteps)
            {
                waitHandles = waitHandles.Concat(new[] { newWorkAvailable });
            }

            var completedItem = WaitHandle.WaitAny(waitHandles.ToArray());

            var completeBuildStep = completedItem < buildStepsToWait.Count ? buildStepsToWait[completedItem] : null;
            if (completeBuildStep == null)
            {
                // Not an actual task completed, but we've got to check if there is a new high priority task so exit immediatly
                return;
            }

            // wait for completion of all its spawned and dependent steps
            // (probably instant most of the time, but it would be good to have a better ExecutedAsync to check that together as well)
            await ListBuildStep.WaitCommands(new List<BuildStep> { completeBuildStep });

            // Remove from list of build step to wait
            buildStepsToWait.Remove(completeBuildStep);
        }
    }
}
