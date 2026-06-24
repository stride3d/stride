// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

using Mono.Options;
using Stride.Core;
using Stride.Core.Assets;
using Stride.Core.Assets.Diagnostics;
using Stride.Core.Assets.Quantum;
using Stride.Core.BuildEngine;
using Stride.Core.Diagnostics;
using Stride.Core.Yaml;
using Stride.Assets.Models;
using Stride.Assets.SpriteFont;
using Stride.Particles;
using Stride.Rendering.Materials;
using Stride.Rendering.ProceduralModels;
using Stride.SpriteStudio.Offline;
using Stride.AssetCompiler.Tasks;
using Stride.Core.IO;

namespace Stride.AssetCompiler
{
    class PackageBuilderApp : IPackageBuilderApp
    {
        private enum BuilderMode
        {
            Build,
            Pack,
            UpdateGeneratedFiles,
            UpgradeAssets,
        }

        private static Stopwatch clock;

        private LogListener globalLoggerOnGlobalMessageLogged;

        private PackageBuilder builder;

        public bool IsSlave { get; private set; }

        public int Run(string[] args)
        {
            clock = Stopwatch.StartNew();

            // TODO this is hardcoded. Check how to make this dynamic instead.
            RuntimeHelpers.RunModuleConstructor(typeof(IProceduralModel).Module.ModuleHandle);
            RuntimeHelpers.RunModuleConstructor(typeof(MaterialKeys).Module.ModuleHandle);
            RuntimeHelpers.RunModuleConstructor(typeof(SpriteFontAsset).Module.ModuleHandle);
            RuntimeHelpers.RunModuleConstructor(typeof(ModelAsset).Module.ModuleHandle);
            RuntimeHelpers.RunModuleConstructor(typeof(SpriteStudioAnimationAsset).Module.ModuleHandle);
            RuntimeHelpers.RunModuleConstructor(typeof(ParticleSystem).Module.ModuleHandle);
            //var project = new Package();
            //project.Save("test.sdpkg");

            //Thread.Sleep(10000);
            //var spriteFontAsset = StaticFontAsset.New();
            //Content.Save("test.sdfnt", spriteFontAsset);
            //project.Refresh();

            //args = new string[] { "test.sdpkg", "-o:app_data", "-b:tmp", "-t:1" };

            var exeName = Path.GetFileName(Assembly.GetExecutingAssembly().Location);
            var showHelp = false;
            var mode = BuilderMode.Build;
            var buildEngineLogger = GlobalLogger.GetLogger("BuildEngine");
            var options = new PackageBuilderOptions(new ForwardingLoggerResult(buildEngineLogger));

            var p = new OptionSet
            {
                "Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp) All Rights Reserved",
                "Stride Build Tool - Version: "
                +
                String.Format(
                    "{0}.{1}.{2}",
                    typeof(Program).Assembly.GetName().Version.Major,
                    typeof(Program).Assembly.GetName().Version.Minor,
                    typeof(Program).Assembly.GetName().Version.Build) + string.Empty,
                string.Format("Usage: {0} <command> <input> [options]*", exeName),
                string.Empty,
                "Commands: build | pack | upgrade | generate-code",
                string.Empty,
                "=== Options ===",
                string.Empty,
                { "h|help", "Show this message and exit", v => showHelp = v != null },
                { "v|verbose", "Show more verbose progress logs", v => options.Verbose = v != null },
                { "d|debug", "Show debug logs (imply verbose)", v => options.Debug = v != null },
                { "log", "Enable file logging", v => options.EnableFileLogging = v != null },
                { "disable-auto-compile", "Disable auto-compile of projects", v => options.DisableAutoCompileProjects = v != null},
                { "project-configuration=", "Project configuration", v => options.ProjectConfiguration = v },
                { "platform=", "Platform name", v => options.Platform = Enum.Parse<PlatformType>(v) },
                { "solution-file=", "Solution File Name", v => options.SolutionFile = v },
                { "package-id=", "Package Id from the solution file", v => options.PackageId = Guid.Parse(v) },
                { "msbuild-uptodatecheck-filebase=", "BuildUpToDate File base for MSBuild; it will create one .inputs and one .outputs files", v => options.MSBuildUpToDateCheckFileBase = v },
                { "o|output-path=", "Output path", v => options.OutputDirectory = v },
                { "b|build-path=", "Build path", v => options.BuildDirectory = v },
                { "log-file=", "Log build in a custom file.", v =>
                {
                    options.EnableFileLogging = v != null;
                    options.CustomLogFileName = v;
                } },
                { "monitor-pipe=", "Monitor pipe.", v =>
                {
                    if (!string.IsNullOrEmpty(v))
                        options.MonitorPipeNames.Add(v);
                } },
                { "slave=", "Slave pipe", v => options.SlavePipe = v }, // Benlitz: I don't think this should be documented
                { "server=", "This Compiler is launched as a server", v => { } },
                { "pack-asset-assembly=", "Host-loadable asset assembly (package-relative path) to declare in the packed sdpkg; repeat for each", v => options.PackAssetAssemblies.Add(v) },
                { "t|threads=", "Number of threads to create. Default value is the number of hardware threads available.", v => options.ThreadCount = int.Parse(v) },
                { "test=", "Run a test session.", v => options.TestName = v },
                { "property:", "Properties. Format is name1=value1;name2=value2", v =>
                {
                    if (!string.IsNullOrEmpty(v))
                    {
                        foreach (var nameValue in v.Split(new [] { ';' }, StringSplitOptions.RemoveEmptyEntries))
                        {
                            var equalIndex = nameValue.IndexOf('=');
                            if (equalIndex == -1)
                                throw new OptionException("Expect name1=value1;name2=value2 format.", "property");

                            var name = nameValue.Substring(0, equalIndex);
                            var value = nameValue.Substring(equalIndex + 1);
                            if (value != string.Empty)
                                options.Properties.Add(name, value);
                        }
                    }
                }
                },
                { "compile-property:", "Compile properties. Format is name1=value1;name2=value2", v =>
                {
                    if (!string.IsNullOrEmpty(v))
                    {
                        if (options.ExtraCompileProperties == null)
                            options.ExtraCompileProperties = new Dictionary<string, string>();

                        foreach (var nameValue in v.Split(new [] { ';' }, StringSplitOptions.RemoveEmptyEntries))
                        {
                            var equalIndex = nameValue.IndexOf('=');
                            if (equalIndex == -1)
                                throw new OptionException("Expect name1=value1;name2=value2 format.", "property");

                            options.ExtraCompileProperties.Add(nameValue.Substring(0, equalIndex), nameValue.Substring(equalIndex + 1));
                        }
                    }
                }
                },
            };

            TextWriterLogListener fileLogListener = null;

            BuildResultCode exitCode;

            try
            {
                // First argument selects the command; the rest are options + the input file.
                var commandArgs = args;
                if (args.Length == 0)
                {
                    showHelp = true;
                }
                else if (!args[0].StartsWith('-'))
                {
                    switch (args[0])
                    {
                        case "build": mode = BuilderMode.Build; break;
                        case "pack": mode = BuilderMode.Pack; break;
                        case "upgrade": mode = BuilderMode.UpgradeAssets; break;
                        case "generate-code": mode = BuilderMode.UpdateGeneratedFiles; break;
                        case "help": showHelp = true; break;
                        default:
                            Console.Error.WriteLine($"Unknown command '{args[0]}'. Expected: build, pack, upgrade, generate-code.");
                            return (int)BuildResultCode.CommandLineError;
                    }
                    commandArgs = args[1..];
                }
                else if (args.Contains("-h") || args.Contains("--help"))
                {
                    showHelp = true;
                }
                else
                {
                    Console.Error.WriteLine("A command is required: build, pack, upgrade, generate-code.");
                    return (int)BuildResultCode.CommandLineError;
                }

                var unexpectedArgs = p.Parse(commandArgs);

                if (showHelp)
                {
                    p.WriteOptionDescriptions(Console.Out);
                    return (int)BuildResultCode.Successful;
                }

                // The lone positional argument is the input file, routed by extension.
                if (options.SlavePipe == null && unexpectedArgs.Count > 0)
                {
                    var input = unexpectedArgs[0];
                    unexpectedArgs.RemoveAt(0);
                    if (input.EndsWith(AssetBuildManifest.FileExtension, StringComparison.OrdinalIgnoreCase))
                        options.PackageManifestFile = input;
                    else
                        options.PackageFile = input;
                }

                // upgrade / generate-code bypass ValidateOptions but still need an input file.
                if ((mode == BuilderMode.UpgradeAssets || mode == BuilderMode.UpdateGeneratedFiles) && string.IsNullOrEmpty(options.PackageFile))
                {
                    Console.Error.WriteLine("This command requires an input package/project file.");
                    return (int)BuildResultCode.CommandLineError;
                }

                // Activate proper log level
                buildEngineLogger.ActivateLog(options.LoggerType);

                // Output logs to the console with colored messages
                if (options.SlavePipe == null)
                {
                    globalLoggerOnGlobalMessageLogged = new ConsoleLogListener { LogMode = ConsoleLogMode.Always };
                    globalLoggerOnGlobalMessageLogged.TextFormatter = FormatLog;
                    GlobalLogger.GlobalMessageLogged += globalLoggerOnGlobalMessageLogged;
                }

                if (mode == BuilderMode.UpdateGeneratedFiles)
                {
                    PackageSessionPublicHelper.FindAndSetMSBuildVersion();

                    var csprojFile = options.PackageFile;

                    var logger = new LoggerResult();
                    var projectDirectory = new UDirectory(Path.GetDirectoryName(csprojFile));
                    var package = Package.Load(logger, csprojFile, new PackageLoadParameters()
                    {
                        LoadMissingDependencies = false,
                        AutoCompileProjects = false,
                        LoadAssemblyReferences = false,
                        AutoLoadTemporaryAssets = true,
                        TemporaryAssetFilter = assetFile => AssetRegistry.IsProjectCodeGeneratorAssetFileExtension(assetFile.FilePath.GetFileExtension().ToLowerInvariant())
                    });

                    foreach (var assetItem in package.TemporaryAssets)
                    {
                        if (assetItem.Asset is IProjectFileGeneratorAsset projectGeneratorAsset)
                        {
                            try
                            {
                                options.Logger.Info($"Processing: {assetItem}");
                                projectGeneratorAsset.SaveGeneratedAsset(assetItem);
                            }
                            catch (Exception e)
                            {
                                options.Logger.Error($"Unhandled exception while updating generated files for {assetItem}", e);
                            }
                        }
                    }

                    return (int)BuildResultCode.Successful;
                }

                if (mode == BuilderMode.UpgradeAssets)
                {
                    PackageSessionPublicHelper.FindAndSetMSBuildVersion();

                    // Restore packages first so PackageSession.Load sees a fully-resolved dep
                    // graph. Today's narrow asset-YAML schema migration doesn't strictly require
                    // this (path-based .sdpkg AssetFolders discovery + no code transforms), but
                    // anything code-aware does — Roslyn upgrades, MSBuild item enumeration that
                    // includes items contributed by package-imported targets (.sdsl/.sdfx, etc.).
                    // Restoring up-front avoids surprises when the upgrader grows beyond schema
                    // bumps. Also cleans up Session log noise from unresolved engine asset refs.
                    // Incremental for already-restored projects, so cheap when called from the
                    // StrideUpgradeAssets MSBuild target inside a real consumer build.
                    var restoreTarget = options.PackageFile.EndsWith(".sln", StringComparison.OrdinalIgnoreCase)
                        || options.PackageFile.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)
                        ? options.PackageFile
                        : System.IO.Path.ChangeExtension(options.PackageFile, ".csproj");
                    if (System.IO.File.Exists(restoreTarget))
                    {
                        var psi = new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = "dotnet",
                            // -p:WarningsAsErrors= demotes NU1605 (package downgrade) from error back to a visible
                            // warning for this restore only: during an upgrade a dependency may already be on the new
                            // version while this project isn't yet (e.g. a shared plugin, or a dependency processed
                            // earlier in the batch), which is a transient, expected downgrade that the upgrade's
                            // reference rewrite resolves. Scoped to this upgrade-time restore — real builds keep
                            // NU1605 as a hard error. (NoWarn would hide it; WarningsNotAsErrors isn't honored by restore.)
                            ArgumentList = { "restore", restoreTarget, "--nologo", "-v", "quiet", "-p:WarningsAsErrors=" },
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            UseShellExecute = false,
                        };
                        using var proc = System.Diagnostics.Process.Start(psi)!;
                        // Drain both streams concurrently to avoid pipe-buffer deadlock.
                        var stdoutTask = proc.StandardOutput.ReadToEndAsync();
                        var stderrTask = proc.StandardError.ReadToEndAsync();
                        proc.WaitForExit();
                        if (proc.ExitCode != 0)
                        {
                            // Fail hard rather than tolerate: caller invoked UpgradeAssets expecting
                            // it to fully work, and code-aware upgrade paths (Roslyn, MSBuild item
                            // enum) silently produce wrong results on partial state. Surfacing the
                            // restore error early is the right failure mode.
                            options.Logger.Error($"dotnet restore failed for {restoreTarget} (exit {proc.ExitCode}). stderr:\n{stderrTask.Result.Trim()}");
                            return (int)BuildResultCode.BuildError;
                        }
                    }

                    // Reconcile reproduces a GameStudio open+save, so load the session the way GameStudio
                    // does — using the defaults, which compile and load the project assemblies. Without that,
                    // assets referencing user script types (e.g. a sample's own components) fail to
                    // deserialize. Reconcile also pulls archetype/base values from dependency packages (e.g.
                    // the engine's default compositor); those load read-only, so Save (LocalPackages only)
                    // never writes back to them.
                    var loadParameters = new PackageLoadParameters
                    {
                        PackageUpgradeRequested = (pkg, upgrades) => PackageUpgradeRequestedAnswer.UpgradeAll,
                        // Whole-solution upgrade may start mixed (e.g. a shared pack already bumped); tolerate the transient NU1605.
                        AllowUpgradeDowngradeRestore = true,
                    };

                    var sessionResult = PackageSession.Load(options.PackageFile, loadParameters);
                    sessionResult.CopyTo(options.Logger);
                    if (sessionResult.HasErrors || sessionResult.Session == null)
                        return (int)BuildResultCode.BuildError;

                    ReconcileBases(sessionResult.Session, options.Logger);

                    sessionResult.Session.Save(options.Logger);
                    return (int)(options.Logger.HasErrors ? BuildResultCode.BuildError : BuildResultCode.Successful);
                }

                if (unexpectedArgs.Any())
                {
                    throw new OptionException("Unexpected arguments [{0}]".ToFormat(string.Join(", ", unexpectedArgs)), "args");
                }
                try
                {
                    options.ValidateOptions();
                }
                catch (ArgumentException ex)
                {
                    throw new OptionException(ex.Message, ex.ParamName);
                }

                if (mode == BuilderMode.Pack)
                {
                    PackageSessionPublicHelper.FindAndSetMSBuildVersion();

                    var csprojFile = options.PackageFile;
                    var intermediatePackagePath = options.BuildDirectory;
                    var generatedItems = new List<(string SourcePath, string PackagePath)>();
                    var logger = new LoggerResult();
                    if (!PackAssetsHelper.Run(logger, csprojFile, intermediatePackagePath, generatedItems, options.PackAssetAssemblies))
                    {
                        foreach (var message in logger.Messages)
                        {
                            Console.WriteLine(message);
                        }
                        return (int)BuildResultCode.BuildError;
                    }
                    foreach (var generatedItem in generatedItems)
                    {
                        Console.WriteLine($"{generatedItem.SourcePath}|{generatedItem.PackagePath}");
                    }
                    return (int)BuildResultCode.Successful;
                }

                // Also write logs from master process into a file
                if (options.SlavePipe == null)
                {
                    if (options.EnableFileLogging)
                    {
                        string logFileName = options.CustomLogFileName;
                        if (string.IsNullOrEmpty(logFileName))
                        {
                            string inputName = Path.GetFileNameWithoutExtension(options.PackageFile);
                            logFileName = "Logs/Build-" + inputName + "-" + DateTime.Now.ToString("yy-MM-dd-HH-mm") + ".txt";
                        }

                        string dirName = Path.GetDirectoryName(logFileName);
                        if (dirName != null)
                            Directory.CreateDirectory(dirName);

                        fileLogListener = new TextWriterLogListener(new FileStream(logFileName, FileMode.Create)) { TextFormatter = FormatLog };
                        GlobalLogger.GlobalMessageLogged += fileLogListener;
                    }

                    options.Logger.Info("BuildEngine arguments: " + string.Join(" ", args));
                    options.Logger.Info("Starting builder.");
                }
                else
                {
                    IsSlave = true;
                }

                if (!string.IsNullOrEmpty(options.TestName))
                {
                    var test = new TestSession();
                    test.RunTest(options.TestName, options.Logger);
                    exitCode = BuildResultCode.Successful;
                }
                else
                {
                    builder = new PackageBuilder(options);
                    if (!IsSlave)
                    {
                        Console.CancelKeyPress += OnConsoleOnCancelKeyPress;
                    }
                    exitCode = builder.Build();
                }
            }
            catch (OptionException e)
            {
                options.Logger.Error($"Command option '{e.OptionName}': {e.Message}");
                exitCode = BuildResultCode.CommandLineError;
            }
            catch (Exception e)
            {
                options.Logger.Error($"Unhandled exception", e);
                exitCode = BuildResultCode.BuildError;
            }
            finally
            {
                if (fileLogListener != null)
                {
                    GlobalLogger.GlobalMessageLogged -= fileLogListener;
                    fileLogListener.LogWriter.Close();
                }

                // Output logs to the console with colored messages
                if (globalLoggerOnGlobalMessageLogged != null)
                {
                    GlobalLogger.GlobalMessageLogged -= globalLoggerOnGlobalMessageLogged;
                }
                if (builder != null && !IsSlave)
                {
                    Console.CancelKeyPress -= OnConsoleOnCancelKeyPress;
                }

                // Reset cache hold by YamlSerializer
                YamlSerializer.Default.ResetCache();
            }
            return (int)exitCode;
        }

        private void OnConsoleOnCancelKeyPress(object _, ConsoleCancelEventArgs e)
        {
            e.Cancel = builder.Cancel();
        }

        // Re-applies changed archetype/base values to derived assets, the way GameStudio does on load.
        // Asset format migration alone only updates the serialized format; opening + saving in the editor
        // additionally runs Quantum's ReconcileWithBase, which is what materialises base-propagated changes
        // (e.g. an engine default-compositor change) into the derived sample assets.
        private static void ReconcileBases(PackageSession session, ILogger logger)
        {
            var nodeContainer = new AssetNodeContainer { NodeBuilder = { NodeFactory = new AssetNodeFactory() } };
            var graphContainer = new AssetPropertyGraphContainer(nodeContainer);

            // First pass: build + register every asset's graph so cross-asset archetype links resolve.
            var graphs = new List<(AssetPropertyGraph Graph, AssetItem AssetItem)>();
            foreach (var package in session.Packages)
            {
                foreach (var assetItem in package.Assets)
                {
                    try
                    {
                        var graph = graphContainer.InitializeAsset(assetItem, logger);
                        if (graph != null)
                            graphs.Add((graph, assetItem));
                    }
                    catch (Exception ex)
                    {
                        logger.Warning($"Could not build property graph for [{assetItem.Location}]: {ex.Message}");
                    }
                }
            }

            // Second pass: reconcile each graph; a content change flags the asset dirty so Save persists it.
            var changedCount = 0;
            foreach (var (graph, assetItem) in graphs)
            {
                var changed = false;
                graph.Changed += (_, _) => { assetItem.IsDirty = true; changed = true; };
                graph.ItemChanged += (_, _) => { assetItem.IsDirty = true; changed = true; };
                try
                {
                    graph.Initialize();
                }
                catch (Exception ex)
                {
                    logger.Warning($"Could not reconcile [{assetItem.Location}] with its base: {ex.Message}");
                }
                if (changed)
                    changedCount++;
            }
            logger.Info($"Reconciled {changedCount} asset(s) with their base out of {graphs.Count}.");
        }

        private static string FormatLog(ILogMessage message)
        {
            //$filename($row,$column): $error_type $error_code: $error_message
            //C:\Code\Stride\sources\assets\Stride.AssetCompiler\PackageBuilder.cs(89,13,89,70): warning CS1717: Assignment made to same variable; did you mean to assign something else?
            var builder = new StringBuilder();
            var assetLogMessage = message as AssetLogMessage;
            // Location
            if (assetLogMessage != null)
                builder.Append($"{assetLogMessage.File}({assetLogMessage.Line + 1},{assetLogMessage.Character + 1}): ");
            // Message type
            builder.Append(message.Type.ToString().ToLowerInvariant()).Append(" ");
            builder.Append((clock.ElapsedMilliseconds * 0.001).ToString("0.000"));
            builder.Append("s: ");
            builder.Append($"[{message.Module ?? "AssetCompiler"}] ");
            builder.Append(message.Text);
            var exceptionInfo = message.ExceptionInfo;
            if (exceptionInfo != null)
            {
                builder.Append(". Exception: ");
                builder.Append(exceptionInfo);
            }
            return builder.ToString();
        }
    }
}
