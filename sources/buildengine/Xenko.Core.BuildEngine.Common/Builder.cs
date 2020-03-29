// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Globalization;

using Xenko.Core;
using Xenko.Core.Storage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xenko.Core.Diagnostics;
using Xenko.Core.MicroThreading;
using Xenko.Core.IO;
using Xenko.Core.Extensions;
using Xenko.Core.Serialization.Contents;

namespace Xenko.Core.BuildEngine
{
    public class Builder : IDisposable
    {
        public const int ExpectedVersion = 4;
        public static readonly string DoNotPackTag = "DoNotPack";
        public static readonly string DoNotCompressTag = "DoNotCompress";

        #region Public Members

        /// <summary>
        /// Indicate which mode to use with this builder
        /// </summary>
        public enum Mode
        {
            /// <summary>
            /// Build the script
            /// </summary>
            Build,
            /// <summary>
            /// Clean the command cache used to determine wheither a command has already been triggered.
            /// </summary>
            Clean,
            /// <summary>
            /// Clean the command cache and delete every output objects
            /// </summary>
            CleanAndDelete,
        }

        /// <summary>
        /// Gets the <see cref="ObjectDatabase"/> in which built objects are written.
        /// </summary>
        public static ObjectDatabase ObjectDatabase { get; private set; }

        /// <summary>
        /// Logger used by the builder and the commands
        /// </summary>
        public ILogger Logger { get; }

        /// <summary>
        /// Builder name
        /// </summary>
        public string BuilderName { get; set; }

        /// <summary>
        /// The <see cref="Guid"/> assigned to the builder.
        /// </summary>
        public Guid BuilderId { get; }

        /// <summary>
        /// Number of working threads to create
        /// </summary>
        public int ThreadCount { get; set; }

        public CommandBuildStep.TryExecuteRemoteDelegate TryExecuteRemote { get; set; }

        /// <summary>
        /// The root build step of the builder defining the builds to perform.
        /// </summary>
        public ListBuildStep Root { get; private set; }

        /// <summary>
        /// Indicate whether this builder is currently running.
        /// </summary>
        public bool IsRunning { get; protected set; }

        /// <summary>
        /// Indicate whether the build has been canceled
        /// </summary>
        public bool Cancelled { get; protected set; }

        public List<string> MonitorPipeNames { get; }
        
        public const string MonitorPipeName = "net.pipe://localhost/Xenko.BuildEngine.Monitor";

        public IDictionary<string, string> InitialVariables { get; }

        public readonly ISet<ObjectId> DisableCompressionIds = new HashSet<ObjectId>();

        #endregion Public Members
        #region Private Members
        
        /// <summary>
        /// The name on the disk of the index file name.
        /// </summary>
        private readonly string indexName;

        /// <summary>
        /// The path on the disk where to perform the build
        /// </summary>
        private readonly string buildPath;

        /// <summary>
        /// Cancellation token source used for cancellation.
        /// </summary>
        private CancellationTokenSource cancellationTokenSource;

        private Scheduler scheduler;

        private readonly CommandIOMonitor ioMonitor;
        private readonly List<IBuildThreadMonitor> currentThreadMonitors = new List<IBuildThreadMonitor>();

        /// <summary>
        /// A map containing results of each commands, indexed by command hashes. When the builder is running, this map if filled with the result of the commands of the current execution.
        /// </summary>
        private ObjectDatabase resultMap;
        
        private readonly DateTime startTime;

        private readonly StepCounter stepCounter = new StepCounter();

        /// <summary>
        /// The build mode of the current run execution
        /// </summary>
        private Mode runMode;

        #endregion Private Members

        /// <summary>
        /// The full path of the index file from the build directory.
        /// </summary>
        private string IndexFileFullPath => indexName != null ? VirtualFileSystem.ApplicationDatabasePath + VirtualFileSystem.DirectorySeparatorChar + indexName : null;

        public Builder(ILogger logger, string buildPath, string indexName)
        {
            MonitorPipeNames = new List<string>();
            startTime = DateTime.Now;
            this.indexName = indexName;
            Logger = logger;
            this.buildPath = buildPath ?? throw new ArgumentNullException(nameof(buildPath));
            Root = new ListBuildStep();
            ioMonitor = new CommandIOMonitor(Logger);
            ThreadCount = Environment.ProcessorCount;
            BuilderId = Guid.NewGuid();
            InitialVariables = new Dictionary<string, string>();
        }

        public static void OpenObjectDatabase(string buildPath, string indexName)
        {
            // Mount build path
            ((FileSystemProvider)VirtualFileSystem.ApplicationData).ChangeBasePath(buildPath);
            if (ObjectDatabase == null)
            {
                // Note: this has to be done after VFS.ChangeBasePath
                ObjectDatabase = new ObjectDatabase(VirtualFileSystem.ApplicationDatabasePath, indexName, null, false);
            }
        }

        public static void CloseObjectDatabase()
        {
            var db = ObjectDatabase;
            ObjectDatabase = null;
            db?.Dispose();
        }

        public void Dispose()
        {
            CloseObjectDatabase();
        }

        private class ExecuteContext : IExecuteContext
        {
            private readonly BuilderContext builderContext;
            private readonly BuildStep buildStep;
            private readonly BuildTransaction buildTransaction;
            private readonly Builder builder;

            public ExecuteContext(Builder builder, BuilderContext builderContext, BuildStep buildStep, Logger logger)
            {
                Logger = logger;
                this.builderContext = builderContext;
                this.builder = builder;
                this.buildStep = buildStep;
                buildTransaction = new BuildTransaction(null, buildStep.GetOutputObjectsGroups());
            }

            public Logger Logger { get; }

            public ObjectDatabase ResultMap => builder.resultMap;

            public CancellationTokenSource CancellationTokenSource => builder.cancellationTokenSource;

            public Dictionary<string, string> Variables { get; set; }

            public void ScheduleBuildStep(BuildStep step)
            {
                builder.ScheduleBuildStep(builderContext, buildStep, step, Variables);
            }

            public IEnumerable<IReadOnlyDictionary<ObjectUrl, OutputObject>> GetOutputObjectsGroups()
            {
                return buildStep.GetOutputObjectsGroups();
            }

            public ObjectId ComputeInputHash(UrlType type, string filePath)
            {
                var hash = ObjectId.Empty;

                switch (type)
                {
                    case UrlType.File:
                        hash = builderContext.InputHashes.ComputeFileHash(filePath);
                        break;
                    case UrlType.Content:
                        if (!buildTransaction.TryGetValue(filePath, out hash))
                            Logger.Warning("Location " + filePath + " does not exist currently and is required to compute the current command hash. The build cache will not work for this command!");
                        break;
                }

                return hash;
            }

            public CommandBuildStep IsCommandCurrentlyRunning(ObjectId commandHash)
            {
                lock (builderContext.CommandsInProgress)
                {
                    CommandBuildStep step;
                    builderContext.CommandsInProgress.TryGetValue(commandHash, out step);
                    return step;
                }
            }

            public void NotifyCommandBuildStepStarted(CommandBuildStep commandBuildStep, ObjectId commandHash)
            {
                lock (builderContext.CommandsInProgress)
                {
                    if (!builderContext.CommandsInProgress.ContainsKey(commandHash))
                        builderContext.CommandsInProgress.Add(commandHash, commandBuildStep);

                    builder.ioMonitor.CommandStarted(commandBuildStep);
                }
            }

            public void NotifyCommandBuildStepFinished(CommandBuildStep commandBuildStep, ObjectId commandHash)
            {
                lock (builderContext.CommandsInProgress)
                {
                    builderContext.CommandsInProgress.Remove(commandHash);
                    builder.ioMonitor.CommandEnded(commandBuildStep);
                }
            }
        }

        private void ScheduleBuildStep(BuilderContext builderContext, BuildStep instigator, BuildStep buildStep, IDictionary<string, string> variables)
        {
            if (buildStep.ExecutionId == 0)
            {
                if (buildStep.Parent != null && buildStep.Parent != instigator)
                    throw new InvalidOperationException("Scheduling a BuildStep with a different instigator that its parent");
                if (buildStep.Parent == null)
                {
                    buildStep.Parent = instigator;
                }

                // Compute content dependencies before scheduling the build
                GenerateDependencies(buildStep);

                // TODO: Big review of the log infrastructure of CompilerApp & BuildEngine!
                // Create a logger that redirects to various places (BuildStep.Logger, timestampped log, global log, etc...)
                var buildStepLogger = new BuildStepLogger(buildStep, Logger, startTime);
                var logger = (Logger)buildStepLogger;
                // Apply user-registered callbacks to the logger
                buildStep.TransformExecuteContextLogger?.Invoke(ref logger);

                // Create execute context
                var executeContext = new ExecuteContext(this, builderContext, buildStep, logger) { Variables = new Dictionary<string, string>(variables) };
                //buildStep.ExpandStrings(executeContext);

                if (runMode == Mode.Build)
                {
                    MicroThread microThread = scheduler.Create();

                    // Set priority from this build step, if we have one.
                    if (buildStep.Priority.HasValue)
                    {
                        microThread.Priority = buildStep.Priority.Value;
                    }

                    buildStep.ExecutionId = microThread.Id;

                    foreach (var threadMonitor in currentThreadMonitors)
                    {
                        threadMonitor.RegisterBuildStep(buildStep, buildStepLogger.StepLogger);
                    }

                    microThread.Name = buildStep.ToString();

                    // Default:
                    // Schedule continuations as early as possible to help EnumerableBuildStep finish when all its task are finished.
                    // Otherwise, it would wait for all leaf to finish first before finishing parent EnumerableBuildStep.
                    // This should also reduce memory usage, and might improve cache coherency as well.
                    microThread.ScheduleMode = ScheduleMode.First;

                    microThread.Start(async () =>
                    {
                        // Wait for prerequisites
                        await Task.WhenAll(buildStep.PrerequisiteSteps.Select(x => x.ExecutedAsync()).ToArray());

                        // Check for failed prerequisites
                        var status = ResultStatus.NotProcessed;

                        if (buildStep.ArePrerequisitesSuccessful)
                        {
                            try
                            {
                                var outputObjectsGroups = executeContext.GetOutputObjectsGroups();
                                MicrothreadLocalDatabases.MountDatabase(outputObjectsGroups);

                                // Execute
                                status = await buildStep.Execute(executeContext, builderContext);
                            }
                            catch (TaskCanceledException e)
                            {
                                // Benlitz: I'm NOT SURE this is the correct explanation, it might be a more subtle race condition, but I can't manage to reproduce it again
                                executeContext.Logger.Warning("A child task of build step " + buildStep + " triggered a TaskCanceledException that was not caught by the parent task. The command has not handled cancellation gracefully.");
                                executeContext.Logger.Warning(e.Message);
                                status = ResultStatus.Cancelled;
                            }
                            catch (Exception e)
                            {
                                executeContext.Logger.Error("Exception in command " + buildStep + ": " + e);
                                status = ResultStatus.Failed;
                            }
                            finally
                            {
                                MicrothreadLocalDatabases.UnmountDatabase();

                                // Ensure the command set at least the result status
                                if (status == ResultStatus.NotProcessed)
                                    throw new InvalidDataException("The build step " + buildStep + " returned ResultStatus.NotProcessed after completion.");
                            }
                            if (microThread.Exception != null)
                            {
                                executeContext.Logger.Error("Exception in command " + buildStep + ": " + microThread.Exception);
                                status = ResultStatus.Failed;
                            }
                        }
                        else
                        {
                            status = ResultStatus.NotTriggeredPrerequisiteFailed;
                        }

                        //if (completedTask.IsCanceled)
                        //{
                        //    completedStep.Status = ResultStatus.Cancelled;
                        //}
                        var logType = LogMessageType.Info;
                        string logText = null;
                        
                        switch (status)
                        {
                            case ResultStatus.Successful:
                                logType = LogMessageType.Verbose;
                                logText = "BuildStep {0} was successful.".ToFormat(buildStep.ToString());
                                break;
                            case ResultStatus.Failed:
                                logType = LogMessageType.Error;
                                logText = "BuildStep {0} failed.".ToFormat(buildStep.ToString());
                                break;
                            case ResultStatus.NotTriggeredPrerequisiteFailed:
                                logType = LogMessageType.Error;
                                logText = "BuildStep {0} failed of previous failed prerequisites.".ToFormat(buildStep.ToString());
                                break;
                            case ResultStatus.Cancelled:
                                logType = LogMessageType.Warning;
                                logText = "BuildStep {0} cancelled.".ToFormat(buildStep.ToString());
                                break;
                            case ResultStatus.NotTriggeredWasSuccessful:
                                logType = LogMessageType.Verbose;
                                logText = "BuildStep {0} is up-to-date and has been skipped".ToFormat(buildStep.ToString());
                                break;
                            case ResultStatus.NotProcessed:
                                throw new InvalidDataException("BuildStep has neither succeeded, failed, nor been cancelled");
                        }
                        if (logText != null)
                        {
                            var logMessage = new LogMessage(null, logType, logText);
                            executeContext.Logger.Log(logMessage);
                        }

                        buildStep.RegisterResult(executeContext, status);
                        stepCounter.AddStepResult(status);
                    });
                }
                else
                {
                    buildStep.Clean(executeContext, builderContext, runMode == Mode.CleanAndDelete);
                }
            }
        }

        /// <summary>
        /// Cancel the currently executing build.
        /// </summary>
        public void CancelBuild()
        {
            if (IsRunning)
            {
                Cancelled = true;
                cancellationTokenSource.Cancel();
            }
        }

        private void RunUntilEnd()
        {
            foreach (var threadMonitor in currentThreadMonitors)
                threadMonitor.RegisterThread(Thread.CurrentThread.ManagedThreadId);

            while (true)
            {
                scheduler.Run();
                
                // Exit loop if no more micro threads
                lock (scheduler.MicroThreads)
                {
                    if (scheduler.MicroThreads.Count == 0)
                        break;
                }

                // TODO: improve how we wait for work. Thread.Sleep(0) uses too much CPU.
                Thread.Sleep(1);
            }
        }

        /// <summary>
        /// Discard the current <see cref="Root"/> build step and initialize a new empty one.
        /// </summary>
        public void Reset()
        {
            Root = new ListBuildStep();
            stepCounter.Clear();
        }

        /// <summary>
        /// Write the generated objects into the index map file.
        /// </summary>
        /// <param name="mergeWithCurrentIndexFile">Indicate if old values must be deleted or merged</param>
        public void WriteIndexFile(bool mergeWithCurrentIndexFile)
        {
            if (!mergeWithCurrentIndexFile)
            {
                VirtualFileSystem.FileDelete(IndexFileFullPath);
            }

            using (var indexFile = ContentIndexMap.NewTool(indexName))
            {
                // Filter database Location
                indexFile.AddValues(
                    Root.OutputObjects.Where(x => x.Key.Type == UrlType.Content)
                        .Select(x => new KeyValuePair<string, ObjectId>(x.Key.Path, x.Value.ObjectId)));

                foreach (var outputObject in Root.OutputObjects.Where(x => x.Key.Type == UrlType.Content).Select(x => x.Value))
                {
                    if (outputObject.Tags.Contains(DoNotCompressTag))
                        DisableCompressionIds.Add(outputObject.ObjectId);
                }
            }
        }

        /// <summary>
        /// Runs this instance.
        /// </summary>
        public BuildResultCode Run(Mode mode, bool writeIndexFile = true, List<IBuildThreadMonitor> threadMonitors = null)
        {
            // When we setup the database ourself we have to take responsibility to close it after
            var shouldCloseDatabase = ObjectDatabase == null;
            OpenObjectDatabase(buildPath, indexName);

            PreRun();

            runMode = mode;

            if (IsRunning)
                throw new InvalidOperationException("An instance of this Builder is already running.");

            // reset build cache from previous build run
            cancellationTokenSource = new CancellationTokenSource();
            Cancelled = false;
            IsRunning = true;
            DisableCompressionIds.Clear();

            currentThreadMonitors.Clear();
            if (threadMonitors != null)
            {
                foreach (var threadMonitor in threadMonitors)
                    currentThreadMonitors.Add(threadMonitor);
            }

            // Reseting result map
            var inputHashes = FileVersionTracker.GetDefault();
            {
                var builderContext = new BuilderContext(inputHashes, TryExecuteRemote);

                resultMap = ObjectDatabase;

                scheduler = new Scheduler();
                foreach (var threadMonitor in currentThreadMonitors)
                    threadMonitor.Start();

                // Schedule the build
                ScheduleBuildStep(builderContext, null, Root, InitialVariables);

                // Create threads
                var threads = Enumerable.Range(0, ThreadCount).Select(x => new Thread(SafeAction.Wrap(RunUntilEnd)) { IsBackground = true }).ToArray();

                // Start threads
                int threadId = 0;
                foreach (var thread in threads)
                {
                    thread.Name = (BuilderName ?? "Builder") + " worker thread " + (++threadId);
                    thread.Start();
                }

                // Wait for all threads to finish
                foreach (var thread in threads)
                {
                    thread.Join();
                }

                foreach (var threadMonitor in currentThreadMonitors)
                    threadMonitor.Finish();

                foreach (var threadMonitor in currentThreadMonitors)
                    threadMonitor.Join();
            }

            currentThreadMonitors.Clear();
            BuildResultCode result;

            if (runMode == Mode.Build)
            {
                if (cancellationTokenSource.IsCancellationRequested)
                {
                    Logger.Error("Build cancelled.");
                    result = BuildResultCode.Cancelled;

                }
                else if (stepCounter.Get(ResultStatus.Failed) > 0 || stepCounter.Get(ResultStatus.NotTriggeredPrerequisiteFailed) > 0)
                {
                    Logger.Error($"Build finished in {stepCounter.Total} steps. Command results: {stepCounter.Get(ResultStatus.Successful)} succeeded, {stepCounter.Get(ResultStatus.NotTriggeredWasSuccessful)} up-to-date, {stepCounter.Get(ResultStatus.Failed)} failed, {stepCounter.Get(ResultStatus.NotTriggeredPrerequisiteFailed)} not triggered due to previous failure.");
                    Logger.Error("Build failed.");
                    result = BuildResultCode.BuildError;
                }
                else
                {
                    Logger.Info($"Build finished in {stepCounter.Total} steps. Command results: {stepCounter.Get(ResultStatus.Successful)} succeeded, {stepCounter.Get(ResultStatus.NotTriggeredWasSuccessful)} up-to-date, {stepCounter.Get(ResultStatus.Failed)} failed, {stepCounter.Get(ResultStatus.NotTriggeredPrerequisiteFailed)} not triggered due to previous failure.");
                    Logger.Info("Build is successful.");
                    result = BuildResultCode.Successful;
                }
            }
            else
            {
                string modeName;
                switch (runMode)
                {
                    case Mode.Clean:
                        modeName = "Clean";
                        break;
                    case Mode.CleanAndDelete:
                        modeName = "Clean-and-delete";
                        break;
                    default:
                        throw new InvalidOperationException("Builder executed in unknown mode.");
                }

                if (cancellationTokenSource.IsCancellationRequested)
                {
                    Logger.Error(modeName + " has been cancelled.");
                    result = BuildResultCode.Cancelled;

                }
                else if (stepCounter.Get(ResultStatus.Failed) > 0 || stepCounter.Get(ResultStatus.NotTriggeredPrerequisiteFailed) > 0)
                {
                    Logger.Error(modeName + " has failed.");
                    result = BuildResultCode.BuildError;
                }
                else
                {
                    Logger.Error(modeName + " has been successfully completed.");
                    result = BuildResultCode.Successful;
                }
            }
            scheduler = null;
            resultMap = null;
            IsRunning = false;

            if (shouldCloseDatabase)
            {
                CloseObjectDatabase();
            }

            return result;
        }

        private void PreRun()
        {
            var objectDatabase = Builder.ObjectDatabase;

            // Check current database version, and erase it if too old
            int currentVersion = ExpectedVersion;
            var versionFile = Path.Combine(VirtualFileSystem.GetAbsolutePath(VirtualFileSystem.ApplicationDatabasePath), "version");
            if (File.Exists(versionFile))
            {
                try
                {
                    var versionText = File.ReadAllText(versionFile);
                    currentVersion = int.Parse(versionText);
                }
                catch (Exception e)
                {
                    e.Ignore();
                    currentVersion = 0;
                }
            }

            // Prepare data base directories
            var databasePathSplits = VirtualFileSystem.ApplicationDatabasePath.Split('/');
            var accumulatorPath = "/";
            foreach (var pathPart in databasePathSplits.Where(x => x != ""))
            {
                accumulatorPath += pathPart + "/";
                VirtualFileSystem.CreateDirectory(accumulatorPath);
            }

            if (currentVersion != ExpectedVersion)
            {
                var looseObjects = objectDatabase.EnumerateLooseObjects().ToArray();

                if (looseObjects.Length > 0)
                {
                    Logger.Info($"Database version number has been updated from {currentVersion} to {ExpectedVersion}, erasing all objects...");

                    // Database version has been updated, let's clean it
                    foreach (var objectId in looseObjects)
                    {
                        try
                        {
                            objectDatabase.Delete(objectId);
                        }
                        catch (IOException)
                        {
                        }
                    }
                }

                // Create directory
                File.WriteAllText(versionFile, ExpectedVersion.ToString(CultureInfo.InvariantCulture));
            }
        }

        /// <summary>
        /// Collects dependencies between <see cref="BuildStep.OutputLocation"/> and fill the <see cref="BuildStep.PrerequisiteSteps"/> accordingly.
        /// </summary>
        /// <param name="rootStep">The root BuildStep</param>
        private void GenerateDependencies(BuildStep rootStep)
        {
            // TODO: Support proper incremental dependecies
            if (rootStep.ProcessedDependencies)
                return;

            rootStep.ProcessedDependencies = true;

            var contentBuildSteps = new Dictionary<string, KeyValuePair<BuildStep, HashSet<string>>>();
            PrepareDependencyGraph(rootStep, contentBuildSteps);
            ComputeDependencyGraph(contentBuildSteps);
        }

        /// <summary>
        /// Collects dependencies between <see cref="BuildStep.OutputLocation"/> BuildStep. See remarks.
        /// </summary>
        /// <param name="step">The step to compute the dependencies for</param>
        /// <param name="contentBuildSteps">A cache of content reference location to buildsteps </param>
        /// <remarks>
        /// Each BuildStep inheriting from <see cref="BuildStep.OutputLocation"/> is considered as a top-level dependency step that can have depedencies
        /// on other top-level dependency. We are collecting all of them here.
        /// </remarks>
        private static void PrepareDependencyGraph(BuildStep step, Dictionary<string, KeyValuePair<BuildStep, HashSet<string>>> contentBuildSteps)
        {
            step.ProcessedDependencies = true;

            var outputLocation = step.OutputLocation;
            if (outputLocation != null)
            {
                var dependencies = new HashSet<string>();
                if (!contentBuildSteps.ContainsKey(outputLocation))
                {
                    contentBuildSteps.Add(outputLocation, new KeyValuePair<BuildStep, HashSet<string>>(step, dependencies));
                    CollectContentReferenceDependencies(step, dependencies);
                    foreach (var prerequisiteStep in step.PrerequisiteSteps)
                    {
                        PrepareDependencyGraph(prerequisiteStep, contentBuildSteps);
                    }
                }

                // If we have a reference, we don't need to iterate further
                return;
            }

            // NOTE: We assume that only ListBuildStep is the base class for sub-steps and that ContentReferencable BuildStep are accessible from them (not through dynamic build step)
            var enumerateBuildStep = step as ListBuildStep;
            if (enumerateBuildStep?.Steps != null)
            {
                foreach (var subStep in enumerateBuildStep.Steps)
                {
                    PrepareDependencyGraph(subStep, contentBuildSteps);
                }
            }
        }

        private void ComputeDependencyGraph(Dictionary<string, KeyValuePair<BuildStep, HashSet<string>>> contentBuildSteps)
        {
            foreach(var item in contentBuildSteps)
            {
                var step = item.Value.Key;
                var dependencies = item.Value.Value;
                foreach (var dependency in dependencies)
                {
                    KeyValuePair<BuildStep, HashSet<string>> deps;
                    if (contentBuildSteps.TryGetValue(dependency, out deps))
                    {
                        BuildStep.LinkBuildSteps(deps.Key, step);
                    }
                    else
                    {
                        // TODO: Either something is wrong, or it's because dependencies added afterwise (incremental) are not supported yet
                        Logger.Error($"BuildStep [{step}] depends on [{dependency}] but nothing that generates it could be found (or maybe incremental dependencies need to be implemented)");
                    }
                }
            }
        }

        private static void CollectContentReferenceDependencies(BuildStep step, HashSet<string> locations)
        {
            // For each CommandStep for the current build step, collects all dependencies to ContenrReference-BuildStep
            foreach (var commandStep in CollectCommandSteps(step))
            {
                foreach (var inputFile in commandStep.Command.GetInputFiles())
                {
                    if (inputFile.Type == UrlType.Content)
                    {
                        locations.Add(inputFile.Path);
                    }
                }
            }
        }

        private static IEnumerable<CommandBuildStep> CollectCommandSteps(BuildStep step)
        {
            var commandBuildStep = step as CommandBuildStep;
            if (commandBuildStep != null)
            {
                yield return commandBuildStep;
            }

            // NOTE: We assume that only EnumerableBuildStep is the base class for sub-steps and that ContentReferencable BuildStep are accessible from them (not through dynamic build step)
            var enumerateBuildStep = step as ListBuildStep;
            if (enumerateBuildStep?.Steps != null)
            {
                foreach (var subStep in enumerateBuildStep.Steps)
                {
                    foreach (var command in CollectCommandSteps(subStep))
                    {
                        yield return command;
                    }
                }
            }
        }
    }
}
