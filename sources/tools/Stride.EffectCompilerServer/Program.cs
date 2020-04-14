// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.IO;
using System.Reflection;
using System.Threading;
using Mono.Options;
using Stride.Engine.Network;

namespace Stride.EffectCompilerServer
{
    class Program
    {
        static int Main(string[] args)
        {
            var exeName = Path.GetFileName(Assembly.GetExecutingAssembly().Location);
            var showHelp = false;
            int exitCode = 0;

            var p = new OptionSet
                {
                    "Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp) All Rights Reserved",
                    "Stride Effect Compiler Server - Version: "
                    +
                    String.Format(
                        "{0}.{1}.{2}",
                        typeof(Program).Assembly.GetName().Version.Major,
                        typeof(Program).Assembly.GetName().Version.Minor,
                        typeof(Program).Assembly.GetName().Version.Build) + string.Empty,
                    string.Format("Usage: {0}", exeName),
                    string.Empty,
                    "=== Options ===",
                    string.Empty,
                    { "h|help", "Show this message and exit", v => showHelp = v != null },
                };

            try
            {
                var commandArgs = p.Parse(args);
                if (showHelp)
                {
                    p.WriteOptionDescriptions(Console.Out);
                    return 0;
                }

                // Make sure path exists
                if (commandArgs.Count > 0)
                    throw new OptionException("This command expect no additional arguments", "");

                var effectCompilerServer = new EffectCompilerServer();
                effectCompilerServer.TryConnect("127.0.0.1", RouterClient.DefaultPort);

                AppDomain.CurrentDomain.UnhandledException += (sender, eventArgs) =>
                {
                    var e = eventArgs.ExceptionObject as Exception;
                    if (e == null) return;

                    Console.WriteLine($"Unhandled Exception: {e.Message.ToString()}");
                };

                // Forbid process to terminate (unless ctrl+c)
                while (true)
                {
                    Console.Read();
                    Thread.Sleep(100);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("{0}: {1}", exeName, e);
                if (e is OptionException)
                    p.WriteOptionDescriptions(Console.Out);
                exitCode = 1;
            }

            return exitCode;
        }
    }
}
