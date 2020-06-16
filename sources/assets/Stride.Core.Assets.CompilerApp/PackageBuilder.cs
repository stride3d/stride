
// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

using Stride.Core.Assets.Compiler;
using Stride.Core.Assets.Diagnostics;
using Stride.Core.BuildEngine;
using Stride.Core;
using Stride.Core.Diagnostics;
using Stride.Core.IO;
using Stride.Core.MicroThreading;
using System.Threading;
using System.Threading.Tasks;
using Stride.Core.Assets.Analysis;
using Stride.Core.Reflection;
using Stride.Core.Serialization.Contents;
using Stride;
using Stride.Assets;
using Stride.Graphics;
using Stride.Core.VisualStudio;
using ServiceWire.NamedPipes;
using System.IO;
using Stride.Core.Storage;
using System.Text;

namespace Stride.Core.Assets.CompilerApp
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

                // Find loaded package (either sdpkg or csproj) -- otherwise fallback to first one
                var packageFile = (UFile)builderOptions.PackageFile;
                var package = projectSession.Packages.FirstOrDefault(x => x.FullPath == packageFile || (x.Container is SolutionProject project && project.FullPath == packageFile))
                    ?? projectSession.LocalPackages.FirstOrDefault()
                    ?? projectSession.Packages.FirstOrDefault();

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
                    BuildConfiguration = builderOptions.ProjectConfiguration,
                    Package = package,
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

                var indexName = $"index.{package.Meta.Name}.{builderOptions.Platform}";
                // Add runtime identifier (if any) to avoid clash when building multiple at the same time (this happens when using ExtrasBuildEachRuntimeIdentifier feature of MSBuild.Sdk.Extras)
                if (builderOptions.Properties.TryGetValue("RuntimeIdentifier", out var runtimeIdentifier))
                    indexName += $".{runtimeIdentifier}";
                if (builderOptions.ExtraCompileProperties != null && builderOptions.ExtraCompileProperties.TryGetValue("StrideGraphicsApi", out var graphicsApi))
                    indexName += $".{graphicsApi}";

                // Create the builder
                builder = new Builder(builderOptions.Logger, buildDirectory, indexName) { ThreadCount = builderOptions.ThreadCount, TryExecuteRemote = remoteBuilderHelper.TryExecuteRemote };

                builder.MonitorPipeNames.AddRange(builderOptions.MonitorPipeNames);

                // Add build steps generated by AssetBuilder
                builder.Root.Add(assetBuildResult.BuildSteps);

                // Run builder
                var result = builder.Run(Builder.Mode.Build);
                builder.WriteIndexFile(false);

                // Fill list of bundles
                var bundlePacker = new BundlePacker();
                var bundleFiles = new List<string>();
                bundlePacker.Build(builderOptions.Logger, projectSession, package, indexName, outputDirectory, builder.DisableCompressionIds, context.GetCompilationMode() != CompilationMode.AppStore, bundleFiles);

                if (builderOptions.MSBuildUpToDateCheckFileBase != null)
                    SaveBuildUpToDateFile(builderOptions.MSBuildUpToDateCheckFileBase, builderOptions.PackageFile, package, bundleFiles);

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

        private void SaveBuildUpToDateFile(string msbuildUpToDateCheckFileBase, string packageFile, Package rootPackage, List<string> bundleFiles)
        {
            var inputs = new List<string>();
            var outputs = new List<string>();

            // List asset folders from projects
            foreach (var package in rootPackage.Session.Packages)
            {
                // Note: check if file exists (since it could be an "implicit package" from csproj)
                if (File.Exists(package.FullPath))
                    inputs.Add(package.FullPath.ToWindowsPath());

                // TODO: optimization: for nuget packages, directly use sha512 file rather than individual assets for faster checking

                // List assets
                foreach (var assetFolder in package.AssetFolders)
                {
                    if (Directory.Exists(assetFolder.Path))
                        inputs.Add(assetFolder.Path.ToWindowsPath() + @"\**\*.*");
                }

                // List project assets
                foreach (var assetItem in package.Assets)
                {
                    // Note: we skip .cs files, only serialization code hash should hopefully be enough (otherwise it would skip fast path at each code change)
                    // Let's see if it's robust enough or if some more data need to be hashed or files added
                    if (assetItem.Asset is IProjectAsset && !(assetItem.Asset is Stride.Assets.Scripts.ScriptSourceFileAsset))
                    {
                        // Make sure it is not already covered by one of the previously registered asset folders
                        if (!package.AssetFolders.Any(assetFolder => assetFolder.Path.Contains(assetItem.FullPath)))
                            inputs.Add(assetItem.FullPath.ToWindowsPath());
                    }
                }

                // Hash serialization code
                if (package.Container is SolutionProject project
                    && project.AssemblyProcessorSerializationHashFile != null
                    && File.Exists(project.AssemblyProcessorSerializationHashFile))
                {
                    inputs.Add(project.AssemblyProcessorSerializationHashFile);
                }
            }

            // List input files
            foreach (var inputObject in builder.Root.InputObjects)
            {
                if (inputObject.Key.Type == UrlType.File)
                {
                    inputs.Add(new UFile(inputObject.Key.Path).ToWindowsPath());
                }
            }

            foreach (var bundleFile in bundleFiles)
            {
                outputs.Add(bundleFile);
            }

            // Generate MSBuild up-to-date check property files
            File.WriteAllLines(msbuildUpToDateCheckFileBase + ".inputs", inputs, Encoding.UTF8);
            File.WriteAllLines(msbuildUpToDateCheckFileBase + ".outputs", outputs, Encoding.UTF8);

            // Touch bundle files so that up-to-date check can work
            // We do that after touching the msbuildUpToDateCheckFile
            foreach (var bundleFile in bundleFiles)
            {
                File.SetLastWriteTimeUtc(bundleFile, DateTime.UtcNow);
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

        private static void RegisterRemoteLogger(NpClient<IProcessBuilderRemote> processBuilderRemote)
        {
            // The pipe might be broken while we try to output log, so let's try/catch the call to prevent program for crashing here (it should crash at a proper location anyway if the pipe is broken/closed)
            // ReSharper disable EmptyGeneralCatchClause
            GlobalLogger.GlobalMessageLogged += logMessage =>
            {
                try
                {
                    var assetMessage = logMessage as AssetLogMessage;
                    var message = assetMessage != null ? new AssetSerializableLogMessage(assetMessage) : new SerializableLogMessage((LogMessage)logMessage);

                    processBuilderRemote.Proxy.ForwardLog(message);
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

            // Open ServiceWire Client Channel
            using (var client = new NpClient<IProcessBuilderRemote>(new NpEndPoint(builderOptions.SlavePipe), new StrideServiceWireSerializer()))
            {
                RegisterRemoteLogger(client);

                // Make sure to laod all assemblies containing serializers
                // TODO: Review how costly it is to do so, and possibily find a way to restrict what needs to be loaded (i.e. only app plugins?)
                foreach (var assemblyLocation in client.Proxy.GetAssemblyContainerLoadedAssemblies())
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
                    Command command = client.Proxy.GetCommandToExecute();

                    // Run command
                    var inputHashes = FileVersionTracker.GetDefault();
                    var builderContext = new BuilderContext(inputHashes, null);

                    var commandContext = new RemoteCommandContext(client.Proxy, command, builderContext, logger);
                    MicrothreadLocalDatabases.MountDatabase(commandContext.GetOutputObjectsGroups());
                    command.PreCommand(commandContext);
                    status = await command.DoCommand(commandContext);
                    command.PostCommand(commandContext, status);

                    // Returns result to master builder
                    client.Proxy.RegisterResult(commandContext.ResultEntry);
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

            var address = "Stride/CompilerApp/PackageBuilderApp/" + Guid.NewGuid();
            var arguments = $"--slave=\"{address}\" --build-path=\"{builderOptions.BuildDirectory}\"";

            using (var debugger = VisualStudioDebugger.GetAttached())
            {
                if (debugger != null)
                {
                    arguments += $" --reattach-debugger={debugger.ProcessId}";
                }
            }

            // Start ServiceWire pipe for communication with process
            var processBuilderRemote = new ProcessBuilderRemote(assemblyContainer, commandContext, command);
            var host = new NpHost(address,null,null, new StrideServiceWireSerializer());
            host.AddService<IProcessBuilderRemote>(processBuilderRemote);

            var startInfo = new ProcessStartInfo
            {
                // Note: try to get exec server if it exists, otherwise use CompilerApp.exe
                FileName = Path.ChangeExtension(typeof(PackageBuilder).Assembly.Location, ".exe"),
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
