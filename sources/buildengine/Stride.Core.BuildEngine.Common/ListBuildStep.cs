// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Serialization.Contents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Stride.Core.Storage;

namespace Stride.Core.BuildEngine
{
    public class ListBuildStep : BuildStep
    {
        private readonly List<BuildStep> steps = new List<BuildStep>();
        private readonly List<BuildStep> executedSteps = new List<BuildStep>();
        private readonly Dictionary<ObjectUrl, InputObject> inputObjects = new Dictionary<ObjectUrl, InputObject>();
        private readonly Dictionary<ObjectUrl, OutputObject> outputObjects = new Dictionary<ObjectUrl, OutputObject>();
        private int mergeCounter;

        /// <inheritdoc />
        public override string Title => ToString();

        public IReadOnlyDictionary<ObjectUrl, InputObject> InputObjects => inputObjects;

        public IReadOnlyDictionary<ObjectUrl, OutputObject> OutputObjects => outputObjects;

        /// <inheritdoc/>
        public override IEnumerable<KeyValuePair<ObjectUrl, ObjectId>> OutputObjectIds => outputObjects.Select(x => new KeyValuePair<ObjectUrl, ObjectId>(x.Key, x.Value.ObjectId));

        public IEnumerable<BuildStep> Steps => steps;

        /// <inheritdoc/>
        public override string ToString() => $"Build step list ({Count} items)";

        public override async Task<ResultStatus> Execute(IExecuteContext executeContext, BuilderContext builderContext)
        {
            var buildStepsToWait = new List<BuildStep>();

            // Process prerequisites build steps first
            if (PrerequisiteSteps.Count > 0)
                await CompleteCommands(executeContext, PrerequisiteSteps.ToList());

            foreach (var child in Steps)
            {
                executeContext.ScheduleBuildStep(child);
                buildStepsToWait.Add(child);

                executedSteps.Add(child);
            }

            await CompleteCommands(executeContext, buildStepsToWait);

            return ComputeResultStatusFromExecutedSteps();
        }

        /// <summary>
        /// Determine the result status of an execution of enumeration of build steps.
        /// </summary>
        /// <returns>The result status of the execution.</returns>
        protected ResultStatus ComputeResultStatusFromExecutedSteps()
        {
            if (executedSteps.Count == 0)
                return ResultStatus.Successful;

            // determine the result status of the list based on the children executed steps
            // -> One or more children canceled => canceled
            // -> One or more children failed (Prerequisite or Command) and none canceled => failed
            // -> One or more children succeeded and none canceled nor failed => succeeded
            // -> All the children were successful without triggering => not triggered was successful
            var result = executedSteps[0].Status;
            foreach (var executedStep in executedSteps)
            {
                if (executedStep.Status == ResultStatus.Cancelled)
                {
                    result = ResultStatus.Cancelled;
                    break;
                }

                if (executedStep.Failed)
                    result = ResultStatus.Failed;
                else if (executedStep.Status == ResultStatus.Successful && result != ResultStatus.Failed)
                    result = ResultStatus.Successful;
            }

            return result;
        }

        /// <summary>
        /// Wait for given build steps to finish, then processes their inputs and outputs.
        /// </summary>
        /// <param name="executeContext">The execute context.</param>
        /// <param name="buildStepsToWait">The build steps to wait.</param>
        /// <returns></returns>
        protected async Task CompleteCommands(IExecuteContext executeContext, List<BuildStep> buildStepsToWait)
        {
            await WaitCommands(buildStepsToWait);

            // TODO: Merge results of sub lists
            foreach (var buildStep in buildStepsToWait)
            {
                var enumerableBuildStep = buildStep as ListBuildStep;
                if (enumerableBuildStep != null)
                {
                    // Merge results from sub list

                    // Step1: Check inputs/outputs conflicts
                    foreach (var inputObject in enumerableBuildStep.inputObjects)
                    {
                        CheckInputObject(executeContext, inputObject.Key, inputObject.Value.Command);
                    }

                    foreach (var outputObject in enumerableBuildStep.OutputObjects)
                    {
                        CheckOutputObject(executeContext, outputObject.Key, outputObject.Value.Command);
                    }

                    // Step2: Add inputs/outputs
                    foreach (var inputObject in enumerableBuildStep.inputObjects)
                    {
                        AddInputObject(inputObject.Key, inputObject.Value.Command);
                    }

                    foreach (var outputObject in enumerableBuildStep.OutputObjects)
                    {
                        var newOutputObject = AddOutputObject(executeContext, outputObject.Key, outputObject.Value.ObjectId, outputObject.Value.Command);

                        // Merge tags
                        foreach (var tag in outputObject.Value.Tags)
                        {
                            newOutputObject.Tags.Add(tag);
                        }
                    }
                }

                var commandBuildStep = buildStep as CommandBuildStep;
                if (commandBuildStep != null)
                {
                    // Merge results from spawned step
                    ProcessCommandBuildStepResult(executeContext, commandBuildStep);
                }
            }

            buildStepsToWait.Clear();
            mergeCounter++;
        }

        protected internal static async Task WaitCommands(List<BuildStep> buildStepsToWait)
        {
            // Wait for steps to be finished
            if (buildStepsToWait.Count > 0)
                await Task.WhenAll(buildStepsToWait.Select(x => x.ExecutedAsync()));
        }

        /// <summary>
        /// Processes the results from a <see cref="CommandBuildStep"/>.
        /// </summary>
        /// <param name="executeContext">The execute context.</param>
        /// <param name="buildStep">The build step.</param>
        private void ProcessCommandBuildStepResult(IExecuteContext executeContext, CommandBuildStep buildStep)
        {
            foreach (var resultInputObject in buildStep.Command.GetInputFiles())
            {
                AddInputObject(resultInputObject, buildStep.Command);
            }

            if (buildStep.Result != null)
            {
                // Step1: Check inputs/outputs conflicts
                foreach (var resultInputObject in buildStep.Result.InputDependencyVersions)
                {
                    CheckInputObject(executeContext, resultInputObject.Key, buildStep.Command);
                }

                foreach (var resultOutputObject in buildStep.Result.OutputObjects)
                {
                    CheckOutputObject(executeContext, resultOutputObject.Key, buildStep.Command);
                }

                // Step2: Add inputs/outputs
                foreach (var resultInputObject in buildStep.Result.InputDependencyVersions)
                {
                    AddInputObject(resultInputObject.Key, buildStep.Command);
                }

                foreach (var resultOutputObject in buildStep.Result.OutputObjects)
                {
                    AddOutputObject(executeContext, resultOutputObject.Key, resultOutputObject.Value, buildStep.Command);
                }
            }

            // Forward logs
            buildStep.Logger.CopyTo(Logger);

            if (buildStep.Result != null)
            {
                // Resolve tags from TagSymbol
                // TODO: Handle removed tags
                foreach (var tag in buildStep.Result.TagSymbols)
                {
                    var url = tag.Key;

                    // TODO: Improve search complexity?
                    if (outputObjects.TryGetValue(url, out var outputObject))
                    {
                        outputObject.Tags.Add(tag.Value);
                    }
                }
            }
        }

        /// <summary>
        /// Adds the input object. Will try to detect input/output conflicts.
        /// </summary>
        /// <param name="executeContext">The execute context.</param>
        /// <param name="inputObjectUrl">The input object URL.</param>
        /// <param name="command">The command.</param>
        /// <exception cref="System.InvalidOperationException"></exception>
        private void CheckInputObject(IExecuteContext executeContext, ObjectUrl inputObjectUrl, Command command)
        {
            OutputObject outputObject;
            if (outputObjects.TryGetValue(inputObjectUrl, out outputObject)
                && outputObject.Command != command
                && outputObject.Counter == mergeCounter)
            {
                var error = $"Command {outputObject.Command} is writing {inputObjectUrl} while command {command} is reading it";
                executeContext.Logger.Error(error);
                throw new InvalidOperationException(error);
            }
        }

        private void AddInputObject(ObjectUrl inputObjectUrl, Command command)
        {
            OutputObject outputObject;
            if (outputObjects.TryGetValue(inputObjectUrl, out outputObject)
                && mergeCounter > outputObject.Counter)
            {
                // Object was outputed by ourself, so reading it as input should be ignored.
                return;
            }

            inputObjects[inputObjectUrl] = new InputObject { Command = command, Counter = mergeCounter };
        }

        /// <summary>
        /// Adds the output object. Will try to detect input/output conflicts, and output with different <see cref="ObjectId" /> conflicts.
        /// </summary>
        /// <param name="executeContext">The execute context.</param>
        /// <param name="outputObjectUrl">The output object URL.</param>
        /// <param name="command">The command that produced the output object.</param>
        /// <exception cref="System.InvalidOperationException">Two CommandBuildStep with same inputs did output different results.</exception>
        private void CheckOutputObject(IExecuteContext executeContext, ObjectUrl outputObjectUrl, Command command)
        {
            InputObject inputObject;
            if (inputObjects.TryGetValue(outputObjectUrl, out inputObject)
                && inputObject.Command != command
                && inputObject.Counter == mergeCounter)
            {
                var error = $"Command {command} is writing {outputObjectUrl} while command {inputObject.Command} is reading it";
                executeContext.Logger.Error(error);
                throw new InvalidOperationException(error);
            }
        }

        private OutputObject AddOutputObject(IExecuteContext executeContext, ObjectUrl outputObjectUrl, ObjectId outputObjectId, Command command)
        {
            OutputObject outputObject;

            if (!outputObjects.TryGetValue(outputObjectUrl, out outputObject))
            {
                // New item?
                outputObject = new OutputObject(outputObjectUrl, outputObjectId);
                outputObjects.Add(outputObjectUrl, outputObject);
            }
            else
            {
                // ObjectId should be similar (if no Wait happened), otherwise two tasks spawned with same parameters did output different results
                if (outputObject.ObjectId != outputObjectId && outputObject.Counter == mergeCounter)
                {
                    var error = $"Commands {command} and {outputObject.Command} are both writing {outputObjectUrl} at the same time";
                    executeContext.Logger.Error(error);
                    throw new InvalidOperationException(error);
                }

                // Update new ObjectId
                outputObject.ObjectId = outputObjectId;
            }

            // Update Counter so that we know if a wait happened since this output object has been merged.
            outputObject.Counter = mergeCounter;
            outputObject.Command = command;

            return outputObject;
        }

        public struct InputObject
        {
            public Command Command;
            public int Counter;
        }
        
        /// <inheritdoc/>
        public int Count => steps.Count;

        /// <inheritdoc/>
        public IEnumerator<BuildStep> GetEnumerator()
        {
            return steps.GetEnumerator();
        }

        public CommandBuildStep Add(Command command)
        {
            var commandBuildStep = new CommandBuildStep(command);
            Add(commandBuildStep);
            return commandBuildStep;
        }

        public IEnumerable<CommandBuildStep> Add(IEnumerable<Command> commands)
        {
            var commandBuildSteps = commands.Select(x => new CommandBuildStep(x) ).ToArray();
            foreach (var commandBuildStep in commandBuildSteps)
            {
                Add(commandBuildStep);
            }
            return commandBuildSteps;
        }

        /// <inheritdoc/>
        public void Add(BuildStep buildStep)
        {
            if (Status != ResultStatus.NotProcessed)
                throw new InvalidOperationException("Unable to add a build step to an already processed ListBuildStep.");

            buildStep.Parent = this;
            // Propagate priority if we have one
            if (Priority.HasValue)
            {
                // Overwrite only if the new priority is smaller or if we didn't have a priority before
                buildStep.Priority = buildStep.Priority.HasValue ? Math.Min(buildStep.Priority.Value, Priority.Value) : Priority.Value;
            }
            steps.Add(buildStep);
        }
    }
}
