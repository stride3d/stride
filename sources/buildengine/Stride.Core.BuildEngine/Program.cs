// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.IO;
using Mono.Options;
using System.Reflection;
using System.Text;
using System.Diagnostics;
using Stride.Core.Diagnostics;

namespace Stride.Core.BuildEngine
{
    class Program
    {
        private static string FormatLog(LogMessage message)
        {
            var builder = new StringBuilder();
            TimeSpan timestamp = DateTime.Now - Process.GetCurrentProcess().StartTime;
            builder.Append((timestamp.TotalMilliseconds * 0.001).ToString("0.000 "));
            builder.Append(message.Module);
            builder.Append(": ");
            builder.Append(message.Text);
            return builder.ToString();
        }

        private static int Main(string[] args)
        {
            var exeName = Path.GetFileName(Assembly.GetExecutingAssembly().Location);
            var showHelp = false;
            var options = new BuilderOptions(Logger.GetLogger("BuildEngine"));

            var p = new OptionSet
                {
                    "Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp) All Rights Reserved",
                    "Stride Build Tool - Version: "
                    +
                    String.Format(
                        "{0}.{1}.{2}",
                        typeof(Program).Assembly.GetName().Version.Major,
                        typeof(Program).Assembly.GetName().Version.Minor,
                        typeof(Program).Assembly.GetName().Version.Build) + string.Empty,
                    string.Format("Usage: {0} [options]* inputfile -o outputfile", exeName),
                    string.Empty,
                    "=== Options ===",
                    string.Empty,
                    { "h|help", "Show this message and exit", v => showHelp = v != null },
                    { "v|verbose", "Show more verbose progress logs", v => options.Verbose = v != null },
                    { "d|debug", "Show debug logs (imply verbose)", v => options.Debug = v != null },
                    { "c|clean", "Clean the command cache, forcing to rebuild everything at the next build.", v => options.BuilderMode = Builder.Mode.Clean },
                    { "cd|clean-delete", "Clean the command cache and delete output objects", v => options.BuilderMode = Builder.Mode.CleanAndDelete },
                    { "b|build-path=", "Build path", v => options.BuildDirectory = v },
                    { "mdb|metadata-database=", "Optional ; indicate the directory containing the Metadata database, if used.", v => { if (!string.IsNullOrEmpty(v)) options.MetadataDatabaseDirectory = v; } },
                    { "o|output-path=", "Optional ; indicate an output path to copy the built assets in.", v => options.OutputDirectory = v },
                    { "cfg|config=", "Configuration name", v => options.Configuration = v },
                    { "log", "Enable file logging", v => options.EnableFileLogging = v != null },
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
                    { "s|sourcebase=", "Optional ; Set the base directory for the source files. Not required if all source paths are absolute", v => options.SourceBaseDirectory = v },
                    { "a|append", "If set, the existing asset mappings won't be deleted.", v => options.Append = v != null },
                    { "t|threads=", "Number of threads to create. Default value is the number of hardware threads available.", v => options.ThreadCount = int.Parse(v) },
                    { "p|plugin=", "Add plugin directory.", v =>
                        {
                            if (!string.IsNullOrEmpty(v))
                                options.Plugins.AddPluginFolder(v);
                        } },
                    { "test=", "Run a test session.", v => options.TestName = v }
                };

            TextWriterLogListener fileLogListener = null;

            // Output logs to the console with colored messages
            if (options.SlavePipe == null)
            {
                var consoleLogListener = new ConsoleLogListener { TextFormatter = FormatLog };
                GlobalLogger.MessageLogged += consoleLogListener;
            }

            // Setting up plugin manager
            options.Plugins.AddPluginFolder(Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) ?? "", "BuildPlugins"));
            options.Plugins.Register();

            BuildResultCode exitCode;

            try
            {
                options.InputFiles = p.Parse(args);

                // Also write logs from master process into a file
                if (options.SlavePipe == null)
                {
                    if (options.EnableFileLogging)
                    {
                        string logFileName = options.CustomLogFileName;
                        if (string.IsNullOrEmpty(logFileName))
                        {
                            string inputName = "NoInput";
                            if (options.InputFiles.Count > 0)
                                inputName = Path.GetFileNameWithoutExtension(options.InputFiles[0]);

                            logFileName = "Logs/Build-" + inputName + "-" + DateTime.Now.ToString("yy-MM-dd-HH-mm") + ".txt";
                        }

                        string dirName = Path.GetDirectoryName(logFileName);
                        if (dirName != null)
                            Directory.CreateDirectory(dirName);

                        fileLogListener = new TextWriterLogListener(new FileStream(logFileName, FileMode.Create)) { TextFormatter = FormatLog };
                        GlobalLogger.MessageLogged += fileLogListener;
                    }
                    options.Logger.Info("BuildEngine arguments: " + string.Join(" ", args));
                    options.Logger.Info("Starting builder.");
                }

                if (showHelp)
                {
                    p.WriteOptionDescriptions(Console.Out);
                    exitCode = BuildResultCode.Successful;
                }
                else if (!string.IsNullOrEmpty(options.TestName))
                {
                    var test = new TestSession();
                    test.RunTest(options.TestName, options.Logger);
                    exitCode = BuildResultCode.Successful;
                }
                else
                {
                    exitCode = BuildEngineCommands.Build(options);
                }
            }
            catch (OptionException e)
            {
                options.Logger.Error("{0}", e);
                exitCode = BuildResultCode.CommandLineError;
            }
            catch (Exception e)
            {
                options.Logger.Error("{0}", e);
                exitCode = BuildResultCode.BuildError;
            }
            finally
            {
                if (fileLogListener != null)
                    fileLogListener.LogWriter.Close();

            }
            return (int)exitCode;
        }
    }
}
