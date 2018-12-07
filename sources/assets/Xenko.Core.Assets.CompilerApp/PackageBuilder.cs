// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.ServiceModel;

using Xenko.Core.Assets.Compiler;
using Xenko.Core.Assets.Diagnostics;
using Xenko.Core.BuildEngine;
using Xenko.Core;
using Xenko.Core.Diagnostics;
using Xenko.Core.IO;
using Xenko.Core.MicroThreading;
using System.Threading;
using System.Threading.Tasks;
using Xenko.Core.Assets.Analysis;
using Xenko.Core.Reflection;
using Xenko.Core.Serialization.Contents;
using Xenko;
using Xenko.Assets;
using Xenko.Graphics;
using Xenko.Core.VisualStudio;

namespace Xenko.Core.Assets.CompilerApp
{
    public class PackageBuilder
    {
        private readonly PackageBuilderOptions builderOptions;
        private Builder builder;

        public PackageBuilder(PackageBuilderOptions packageBuilderOptions)
        {
            builderOptions = packageBuilderOptions;
        }

        public BuildResultCode Build()
        {
            BuildResultCode result;

            if (builderOptions.IsValidForSlave())
            {
                // Sleeps one second so that debugger can attach
                //Thread.Sleep(1000);

                result = BuildSlave();
            }
            else
            {
                // build the project to the build path
                result = BuildMaster();
            }

            return result;
        }

        private BuildResultCode BuildMaster()
        {
            try
            {
                PackageSessionPublicHelper.FindAndSetMSBuildVersion();
            }
            catch (Exception e)
            {
                var message = "Could not find a compatible version of MSBuild.\r\n\r\n" +
                              "Check that you have a valid installation with the required workloads, or go to [www.visualstudio.com/downloads](https://www.visualstudio.com/downloads) to install a new one.\r\n\r\n" +
                              e;
                builderOptions.Logger.Error(message);
                return BuildResultCode.BuildError;
            }

            AssetCompilerContext context = null;
            PackageSession projectSession = null;
            try
            {
                var sessionLoadParameters = new PackageLoadParameters
                {
                    AutoCompileProjects = !builderOptions.DisableAutoCompileProjects,
                    ExtraCompileProperties = builderOptions.ExtraCompileProperties,
                    RemoveUnloadableObjects = true,
                    BuildConfiguration = builderOptions.ProjectConfiguration,
                };

                // Loads the root Package
                var projectSessionResult = PackageSession.Load(builderOptions.PackageFile, sessionLoadParameters);
                projectSessionResult.CopyTo(builderOptions.Logger);
                if (projectSessionResult.HasErrors)
                {
                    return BuildResultCode.BuildError;
                }

                projectSession = projectSessionResult.Session;

                // Check build configuration
                var package = projectSession.LocalPackages.Last();

                // Setup variables
                var buildDirectory = builderOptions.BuildDirectory;
                var outputDirectory = builderOptions.OutputDirectory;

                // Process game settings asset
                var gameSettingsAsset = package.GetGameSettingsAsset();
                if (gameSettingsAsset == null)
                {
                    builderOptions.Logger.Warning($"Could not find game settings asset at location [{GameSettingsAsset.GameSettingsLocation}]. Use a Default One");
                    gameSettingsAsset = GameSettingsFactory.Create();
                }

                // Create context
                context = new AssetCompilerContext
                {
                    Platform = builderOptions.Platform,
                    CompilationContext = typeof(AssetCompilationContext),
                    BuildConfiguration = builderOptions.ProjectConfiguration
                };

                // Command line properties
                foreach (var property in builderOptions.Properties)
                    context.OptionProperties.Add(property.Key, property.Value);

                // Set current game settings
                context.SetGameSettingsAsset(gameSettingsAsset);

                // Builds the project
                var assetBuilder = new PackageCompiler(new RootPackageAssetEnumerator(package));
                assetBuilder.AssetCompiled += RegisterBuildStepProcessedHandler;

                context.Properties.Set(BuildAssetNode.VisitRuntimeTypes, true);
                var assetBuildResult = assetBuilder.Prepare(context);
                assetBuildResult.CopyTo(builderOptions.Logger);
                if (assetBuildResult.HasErrors)
                    return BuildResultCode.BuildError;

                // Setup the remote process build
                var remoteBuilderHelper = new PackageBuilderRemoteHelper(projectSession.AssemblyContainer, builderOptions);

                // Create the builder
                var indexName = "index." + package.Meta.Name;
                builder = new Builder(builderOptions.Logger, buildDirectory, indexName) { ThreadCount = builderOptions.ThreadCount, TryExecuteRemote = remoteBuilderHelper.TryExecuteRemote };

                builder.MonitorPipeNames.AddRange(builderOptions.MonitorPipeNames);

                // Add build steps generated by AssetBuilder
                builder.Root.Add(assetBuildResult.BuildSteps);

                // Run builder
                var result = builder.Run(Builder.Mode.Build);
                builder.WriteIndexFile(false);

                // Fill list of bundles
                var bundlePacker = new BundlePacker();
                bundlePacker.Build(builderOptions.Logger, projectSession, indexName, outputDirectory, builder.DisableCompressionIds, context.GetCompilationMode() != CompilationMode.AppStore);

                return result;
            }
            finally
            {
                builder?.Dispose();
                // Dispose the session (in order to unload assemblies)
                projectSession?.Dispose();
                context?.Dispose();

                // Make sure that MSBuild doesn't hold anything else
                VSProjectHelper.Reset();
            }
        }

        private void RegisterBuildStepProcessedHandler(object sender, AssetCompiledArgs e)
        {
            if (e.Result.BuildSteps == null)
                return;

            foreach (var buildStep in e.Result.BuildSteps.EnumerateRecursively())
            {
                buildStep.Tag = e.Asset;
                buildStep.StepProcessed += BuildStepProcessed;
            }
        }

        private void BuildStepProcessed(object sender, BuildStepEventArgs e)
        {
            var assetItem = (AssetItem)e.Step.Tag;
            var assetRef = assetItem.ToReference();
            var project = assetItem.Package;
            var stepLogger = e.Step.Logger;
            // TODO: Big review of the log infrastructure of CompilerApp & BuildEngine!
            if (stepLogger != null)
            {
                foreach (var message in stepLogger.Messages.Where(x => x.IsAtLeast(LogMessageType.Warning)))
                {
                    builderOptions.Logger.Log(message);
                }
            }
            switch (e.Step.Status)
            {
                // This case should never happen
                case ResultStatus.NotProcessed:
                    builderOptions.Logger.Log(new AssetLogMessage(project, assetRef, LogMessageType.Fatal, AssetMessageCode.InternalCompilerError, assetRef.Location));
                    break;
                case ResultStatus.Successful:
                    builderOptions.Logger.Log(new AssetLogMessage(project, assetRef, LogMessageType.Verbose, AssetMessageCode.CompilationSucceeded, assetRef.Location));
                    break;
                case ResultStatus.Failed:
                    builderOptions.Logger.Log(new AssetLogMessage(project, assetRef, LogMessageType.Error, AssetMessageCode.CompilationFailed, assetRef.Location));
                    break;
                case ResultStatus.Cancelled:
                    builderOptions.Logger.Log(new AssetLogMessage(project, assetRef, LogMessageType.Verbose, AssetMessageCode.CompilationCancelled, assetRef.Location));
                    break;
                case ResultStatus.NotTriggeredWasSuccessful:
                    builderOptions.Logger.Log(new AssetLogMessage(project, assetRef, LogMessageType.Verbose, AssetMessageCode.AssetUpToDate, assetRef.Location));
                    break;
                case ResultStatus.NotTriggeredPrerequisiteFailed:
                    builderOptions.Logger.Log(new AssetLogMessage(project, assetRef, LogMessageType.Error, AssetMessageCode.PrerequisiteFailed, assetRef.Location));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            e.Step.StepProcessed -= BuildStepProcessed;
        }

        private static void RegisterRemoteLogger(IProcessBuilderRemote processBuilderRemote)
        {
            // The pipe might be broken while we try to output log, so let's try/catch the call to prevent program for crashing here (it should crash at a proper location anyway if the pipe is broken/closed)
            // ReSharper disable EmptyGeneralCatchClause
            GlobalLogger.GlobalMessageLogged += logMessage =>
            {
                try
                {
                    var assetMessage = logMessage as AssetLogMessage;
                    var message = assetMessage != null ? new AssetSerializableLogMessage(assetMessage) : new SerializableLogMessage((LogMessage)logMessage);

                    processBuilderRemote.ForwardLog(message);
                }
                catch
                {
                }
            };
            // ReSharper restore EmptyGeneralCatchClause
        }

        private BuildResultCode BuildSlave()
        {
            // Mount build path
            ((FileSystemProvider)VirtualFileSystem.ApplicationData).ChangeBasePath(builderOptions.BuildDirectory);

            VirtualFileSystem.CreateDirectory(VirtualFileSystem.ApplicationDatabasePath);

            // Open WCF channel with master builder
            var namedPipeBinding = new NetNamedPipeBinding(NetNamedPipeSecurityMode.None) { SendTimeout = TimeSpan.FromSeconds(300.0), MaxReceivedMessageSize = int.MaxValue };
            var processBuilderRemote = ChannelFactory<IProcessBuilderRemote>.CreateChannel(namedPipeBinding, new EndpointAddress(builderOptions.SlavePipe));

            try
            {
                RegisterRemoteLogger(processBuilderRemote);

                // Make sure to laod all assemblies containing serializers
                // TODO: Review how costly it is to do so, and possibily find a way to restrict what needs to be loaded (i.e. only app plugins?)
                foreach (var assemblyLocation in processBuilderRemote.GetAssemblyContainerLoadedAssemblies())
                {
                    AssemblyContainer.Default.LoadAssemblyFromPath(assemblyLocation, builderOptions.Logger);
                }

                // Create scheduler
                var scheduler = new Scheduler();

                var status = ResultStatus.NotProcessed;

                // Schedule command
                string buildPath = builderOptions.BuildDirectory;

                Builder.OpenObjectDatabase(buildPath, VirtualFileSystem.ApplicationDatabaseIndexName);

                var logger = builderOptions.Logger;
                MicroThread microthread = scheduler.Add(async () =>
                {
                    // Deserialize command and parameters
                    Command command = processBuilderRemote.GetCommandToExecute();

                    // Run command
                    var inputHashes = FileVersionTracker.GetDefault();
                    var builderContext = new BuilderContext(inputHashes, null);

                    var commandContext = new RemoteCommandContext(processBuilderRemote, command, builderContext, logger);
                    MicrothreadLocalDatabases.MountDatabase(commandContext.GetOutputObjectsGroups());
                    command.PreCommand(commandContext);
                    status = await command.DoCommand(commandContext);
                    command.PostCommand(commandContext, status);

                    // Returns result to master builder
                    processBuilderRemote.RegisterResult(commandContext.ResultEntry);
                });

                while (true)
                {
                    scheduler.Run();

                    // Exit loop if no more micro threads
                    lock (scheduler.MicroThreads)
                    {
                        if (!scheduler.MicroThreads.Any())
                            break;
                    }

                    Thread.Sleep(0);
                }

                // Rethrow any exception that happened in microthread
                if (microthread.Exception != null)
                {
                    builderOptions.Logger.Fatal(microthread.Exception.ToString());
                    return BuildResultCode.BuildError;
                }

                if (status == ResultStatus.Successful || status == ResultStatus.NotTriggeredWasSuccessful)
                    return BuildResultCode.Successful;

                return BuildResultCode.BuildError;
            }
            finally
            {
                // Close WCF channel
                // ReSharper disable SuspiciousTypeConversion.Global
                ((IClientChannel)processBuilderRemote).Close();
                // ReSharper restore SuspiciousTypeConversion.Global
            }
        }

        /// <summary>
        /// Cancels this build.
        /// </summary>
        /// <returns><c>true</c> if the build was cancelled, <c>false</c> otherwise.</returns>
        public bool Cancel()
        {
            if (builder != null && builder.IsRunning)
            {
                builder.CancelBuild();
                return true;
            }
            return false;
        }
    }

    public class PackageBuilderRemoteHelper
    {
        private readonly AssemblyContainer assemblyContainer;
        private readonly PackageBuilderOptions builderOptions;
        private int spawnedProcessCount;

        public PackageBuilderRemoteHelper(AssemblyContainer assemblyContainer, PackageBuilderOptions builderOptions)
        {
            this.assemblyContainer = assemblyContainer;
            this.builderOptions = builderOptions;
        }

        public async Task<ResultStatus> TryExecuteRemote(Command command, BuilderContext builderContext, IExecuteContext executeContext, LocalCommandContext commandContext)
        {
            while (!CanSpawnParallelProcess())
            {
                await Task.Delay(1, command.CancellationToken);
            }

            var address = "net.pipe://localhost/" + Guid.NewGuid();
            var arguments = $"--slave=\"{address}\" --build-path=\"{builderOptions.BuildDirectory}\"";

            using (var debugger = VisualStudioDebugger.GetAttached())
            {
                if (debugger != null)
                {
                    arguments += $" --reattach-debugger={debugger.ProcessId}";
                }
            }

            // Start WCF pipe for communication with process
            var processBuilderRemote = new ProcessBuilderRemote(assemblyContainer, commandContext, command);
            var host = new ServiceHost(processBuilderRemote);
            host.AddServiceEndpoint(typeof(IProcessBuilderRemote), new NetNamedPipeBinding(NetNamedPipeSecurityMode.None) { MaxReceivedMessageSize = int.MaxValue }, address);

            var startInfo = new ProcessStartInfo
            {
                // Note: try to get exec server if it exists, otherwise use CompilerApp.exe
                FileName = (string)AppDomain.CurrentDomain.GetData("RealEntryAssemblyFile") ?? typeof(PackageBuilder).Assembly.Location,
                Arguments = arguments,
                WorkingDirectory = Environment.CurrentDirectory,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            host.Open();

            var output = new List<string>();

            var process = new Process { StartInfo = startInfo };
            process.Start();
            process.OutputDataReceived += (_, args) => LockProcessAndAddDataToList(process, output, args);
            process.ErrorDataReceived += (_, args) => LockProcessAndAddDataToList(process, output, args);
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            // Note: we don't want the thread to schedule another job since the CPU core will be in use by the process, so we do a blocking WaitForExit.
            process.WaitForExit();

            host.Close();

            NotifyParallelProcessEnded();

            if (process.ExitCode != 0)
            {
                executeContext.Logger.Error($"Remote command crashed with output:{Environment.NewLine}{string.Join(Environment.NewLine, output)}");
            }

            if (processBuilderRemote.Result != null)
            {
                // Register results back locally
                foreach (var outputObject in processBuilderRemote.Result.OutputObjects)
                {
                    commandContext.RegisterOutput(outputObject.Key, outputObject.Value);
                }

                // Register log messages
                foreach (var logMessage in processBuilderRemote.Result.LogMessages)
                {
                    commandContext.Logger.Log(logMessage);
                }

                // Register tags
                foreach (var tag in processBuilderRemote.Result.TagSymbols)
                {
                    commandContext.AddTag(tag.Key, tag.Value);
                }
            }

            return command.CancellationToken.IsCancellationRequested ? ResultStatus.Cancelled : (process.ExitCode == 0 ? ResultStatus.Successful : ResultStatus.Failed);
        }

        public bool CanSpawnParallelProcess()
        {
            if (Interlocked.Increment(ref spawnedProcessCount) > builderOptions.ThreadCount)
            {
                Interlocked.Decrement(ref spawnedProcessCount);
                return false;
            }
            return true;
        }

        public void NotifyParallelProcessEnded()
        {
            Interlocked.Decrement(ref spawnedProcessCount);
        }


        private static void LockProcessAndAddDataToList(Process process, List<string> output, DataReceivedEventArgs args)
        {
            if (!string.IsNullOrEmpty(args.Data))
            {
                lock (process)
                {
                    output.Add(args.Data);
                }
            }
        }
    }
}
