// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceModel;

using Mono.Options;
using Stride.Core.Storage;
using Stride.Core.Diagnostics;
using Stride.Core.IO;
using Stride.Core.MicroThreading;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Assets;
using Stride.Core.Serialization.Contents;
using System.Threading;

namespace Stride.Core.BuildEngine
{
    public static class BuildEngineCommands
    {
        public static BuildResultCode Build(BuilderOptions options)
        {
            BuildResultCode result;

            if (options.IsValidForSlave())
            {
                // Sleeps one second so that debugger can attach
                //Thread.Sleep(1000);

                result = BuildSlave(options);
            }
            else if (options.IsValidForMaster())
            {
                result = BuildLocal(options);

                if (!string.IsNullOrWhiteSpace(options.OutputDirectory) && options.BuilderMode == Builder.Mode.Build)
                {
                    CopyBuildToOutput(options);
                }
            }
            else
            {
                throw new OptionException("Insufficient parameters, no action taken", "build-path");
            }

            return result;
        }

        private static void PrepareDatabases(BuilderOptions options)
        {
            AssetManager.GetDatabaseFileProvider = () => IndexFileCommand.DatabaseFileProvider.Value;
        }

        public static BuildResultCode BuildLocal(BuilderOptions options)
        {
            string inputFile = options.InputFiles[0];
            string sdkDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../..");

            BuildScript buildScript = BuildScript.LoadFromFile(sdkDir, inputFile);
            buildScript.Compile(options.Plugins);

            if (buildScript.GetWarnings().FirstOrDefault() != null)
            {
                foreach (string warning in buildScript.GetWarnings())
                {
                    options.Logger.Warning(warning);
                }
            }

            if (buildScript.HasErrors)
            {
                foreach (string error in buildScript.GetErrors())
                {
                    options.Logger.Error(error);
                }
                throw new InvalidOperationException("Can't compile the provided build script.");
            }

            string inputDir = Path.GetDirectoryName(inputFile) ?? Environment.CurrentDirectory;
            options.SourceBaseDirectory = options.SourceBaseDirectory ?? Path.Combine(inputDir, buildScript.SourceBaseDirectory ?? "");
            options.BuildDirectory = options.BuildDirectory ?? Path.Combine(inputDir, buildScript.BuildDirectory ?? "");
            options.OutputDirectory = options.OutputDirectory ?? (buildScript.OutputDirectory != null ? Path.Combine(inputDir, buildScript.OutputDirectory) : "");
            options.MetadataDatabaseDirectory = options.MetadataDatabaseDirectory ?? (buildScript.MetadataDatabaseDirectory != null ? Path.Combine(inputDir, buildScript.MetadataDatabaseDirectory) : "");
            if (!string.IsNullOrWhiteSpace(options.SourceBaseDirectory))
            {
                if (!Directory.Exists(options.SourceBaseDirectory))
                {
                    string error = string.Format("Source base directory \"{0}\" does not exists.", options.SourceBaseDirectory);
                    options.Logger.Error(error);
                    throw new OptionException(error, "sourcebase");
                }
                Environment.CurrentDirectory = options.SourceBaseDirectory;
            }

            if (string.IsNullOrWhiteSpace(options.BuildDirectory))
            {
                throw new OptionException("This tool requires a build path.", "build-path");
            }

            // Mount build path
            ((FileSystemProvider)VirtualFileSystem.ApplicationData).ChangeBasePath(options.BuildDirectory);

            options.ValidateOptionsForMaster();

            // assets is always added by default
            //options.Databases.Add(new DatabaseMountInfo("/assets"));
            PrepareDatabases(options);

            try
            {
                VirtualFileSystem.CreateDirectory("/data/");
                VirtualFileSystem.CreateDirectory("/data/db/");
            }
            catch (Exception)
            {
                throw new OptionException("Invalid Build database path", "database");
            }

            // Create builder
            LogMessageType logLevel = options.Debug ? LogMessageType.Debug : (options.Verbose ? LogMessageType.Verbose : LogMessageType.Info);
            var logger = Logger.GetLogger("builder");
            logger.ActivateLog(logLevel);
            var builder = new Builder("builder", options.BuildDirectory, options.BuilderMode, logger) { ThreadCount = options.ThreadCount };
            builder.MonitorPipeNames.AddRange(options.MonitorPipeNames);
            builder.ActivateConfiguration(options.Configuration);
            foreach (var sourceFolder in buildScript.SourceFolders)
            {
                builder.InitialVariables.Add(("SourceFolder:" + sourceFolder.Key).ToUpperInvariant(), sourceFolder.Value); 
            }
            Console.CancelKeyPress += (sender, e) => Cancel(builder, e);

            buildScript.Execute(builder);

            // Run builder
            return builder.Run(options.Append == false);
        }

        private static void RegisterRemoteLogger(IProcessBuilderRemote processBuilderRemote)
        {
            // The pipe might be broken while we try to output log, so let's try/catch the call to prevent program for crashing here (it should crash at a proper location anyway if the pipe is broken/closed)
            // ReSharper disable EmptyGeneralCatchClause
            GlobalLogger.MessageLogged += msg => { try { processBuilderRemote.ForwardLog(msg); } catch { } };
            // ReSharper restore EmptyGeneralCatchClause
        }

        public static BuildResultCode BuildSlave(BuilderOptions options)
        {
            // Mount build path
            ((FileSystemProvider)VirtualFileSystem.ApplicationData).ChangeBasePath(options.BuildDirectory);

            PrepareDatabases(options);

            try
            {
                VirtualFileSystem.CreateDirectory("/data/");
                VirtualFileSystem.CreateDirectory("/data/db/");
            }
            catch (Exception)
            {
                throw new OptionException("Invalid Build database path", "database");
            }

            // Open WCF channel with master builder
            var namedPipeBinding = new NetNamedPipeBinding(NetNamedPipeSecurityMode.None) { SendTimeout = TimeSpan.FromSeconds(300.0) };
            var processBuilderRemote = ChannelFactory<IProcessBuilderRemote>.CreateChannel(namedPipeBinding, new EndpointAddress(options.SlavePipe));

            try
            {
                RegisterRemoteLogger(processBuilderRemote);

                // Create scheduler
                var scheduler = new Scheduler();

                var status = ResultStatus.NotProcessed;

                // Schedule command
                string buildPath = options.BuildDirectory;
                Logger logger = options.Logger;
                MicroThread microthread = scheduler.Add(async () =>
                    {
                        // Deserialize command and parameters
                        Command command = processBuilderRemote.GetCommandToExecute();
                        BuildParameterCollection parameters = processBuilderRemote.GetBuildParameters();

                        // Run command
                        var inputHashes = new DictionaryStore<InputVersionKey, ObjectId>(VirtualFileSystem.OpenStream("/data/db/InputHashes", VirtualFileMode.OpenOrCreate, VirtualFileAccess.ReadWrite, VirtualFileShare.ReadWrite));
                        var builderContext = new BuilderContext(buildPath, inputHashes, parameters, 0, null);

                        var commandContext = new RemoteCommandContext(processBuilderRemote, command, builderContext, logger);
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
                    options.Logger.Fatal(microthread.Exception.ToString());
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

        public static void CopyBuildToOutput(BuilderOptions options)
        {
            throw new InvalidOperationException();
        }

        private static void Collect(HashSet<ObjectId> objectIds, ObjectId objectId, IAssetIndexMap assetIndexMap)
        {
            // Already added?
            if (!objectIds.Add(objectId))
                return;

            using (var stream = AssetManager.FileProvider.OpenStream(DatabaseFileProvider.ObjectIdUrl + objectId, VirtualFileMode.Open, VirtualFileAccess.Read))
            {
                // Read chunk header
                var streamReader = new BinarySerializationReader(stream);
                var header = ChunkHeader.Read(streamReader);

                // Only process chunks
                if (header != null)
                {
                    if (header.OffsetToReferences != -1)
                    {
                        // Seek to where references are stored and deserialize them
                        streamReader.NativeStream.Seek(header.OffsetToReferences, SeekOrigin.Begin);

                        List<ChunkReference> references = null;
                        streamReader.Serialize(ref references, ArchiveMode.Deserialize);

                        foreach (var reference in references)
                        {
                            ObjectId refObjectId;
                            var databaseFileProvider = DatabaseFileProvider.ResolveObjectId(reference.Location, out refObjectId);
                            if (databaseFileProvider != null)
                            {
                                Collect(objectIds, refObjectId, databaseFileProvider.AssetIndexMap);
                            }
                        }
                    }
                }
            }
        }

        public static void Cancel(Builder builder, ConsoleCancelEventArgs e)
        {
            if (builder != null && builder.IsRunning)
            {
                e.Cancel = true;
                builder.CancelBuild();
            }
        }
    }
}
