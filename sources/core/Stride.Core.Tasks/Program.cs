// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Build.Locator;
using Mono.Options;
using Stride.AssetCompiler.Tasks;
using Stride.Core.Diagnostics;

namespace Stride.Core.Tasks
{
    static class Program
    {
        public static int Main(string[] args)
        {
            try
            {
                MSBuildLocator.RegisterDefaults();
            }
            catch (InvalidOperationException e) when (e.Message.StartsWith("No instances of MSBuild could be detected.", StringComparison.Ordinal))
            {
                // When tasks running through build tools throw, it logs an obtuse 'The command [...]/Stride.Core.Tasks.exe [...] exited with code - x'
                // message requiring the user to dig in the output to figure out what happened.
                // The following ensures that a clear message is logged before the stuff above is shown.
                // Do note that the 'error' word in the message is essential for it to be logged,
                // direct any message about this peculiar 'feature' over to Microsoft, thanks !
                Console.Error.WriteLine($@"error {typeof(Program).Namespace}: No supported instance of MSBuild could be detected, make sure you have .Net SDK {Environment.Version.Major}.{Environment.Version.Minor} installed");
                throw;
            }
            return RealMain(args);
        }

        public static int RealMain(string[] args)
        {
            var exeName = Path.GetFileName(Assembly.GetExecutingAssembly().Location);
            var showHelp = false;
            var packHostAssemblies = new List<string>();
            var packCompanionPackages = new List<string>();
            string packPackageKind = null;
            string packPackageToolkit = null;

            var p = new OptionSet
            {
                "Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp) All Rights Reserved",
                "Stride Router Server - Version: "
                +
                String.Format(
                    "{0}.{1}.{2}",
                    typeof(Program).Assembly.GetName().Version.Major,
                    typeof(Program).Assembly.GetName().Version.Minor,
                    typeof(Program).Assembly.GetName().Version.Build) + string.Empty,
                string.Format("Usage: {0} command [options]*", exeName),
                string.Empty,
                "=== Commands ===",
                string.Empty,
                " locate-devenv <MSBuildPath>: returns devenv path",
                " pack-assets <csprojFile> <intermediatePackagePath>: copy and adjust assets for nupkg packaging",
                string.Empty,
                "=== Options ===",
                string.Empty,
                { "pack-host-assembly=", "Host-loadable assembly (package-relative path) to declare in the packed sdpkg; repeat for each", v => packHostAssemblies.Add(v) },
                { "pack-companion=", "Companion package to declare in the packed sdpkg, as Kind:Name:Version[:Replaces[:Toolkit]] (Kind = Assets or Editor; Replaces = ';'-separated package ids; Toolkit = the UI toolkit of an Editor companion's views); repeat for each", v => packCompanionPackages.Add(v) },
                { "pack-kind=", "What the packed package carries (Assets or Editor); a companion package states it", v => packPackageKind = v },
                { "pack-toolkit=", "The UI toolkit of an Editor package's views (e.g. Wpf); a view package states it", v => packPackageToolkit = v },
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
                if (commandArgs.Count == 0)
                    throw new OptionException("You need to specify a command", "");

                switch (commandArgs[0])
                {
                    case "locate-devenv":
                    {
                        if(!OperatingSystem.IsWindows())
                            throw new OptionException("This option is only available on Windows", "");
                            
                        if (commandArgs.Count != 2)
                            throw new OptionException("Need one extra argument", "");
                        var devenvPath = LocateDevenv.FindDevenv(commandArgs[1]);
                        if (devenvPath == null)
                        {
                            Console.WriteLine("Could not locate devenv");
                            return 1;
                        }
                        Console.WriteLine(devenvPath);
                        break;
                    }
                    case "pack-assets":
                    {
                        if (commandArgs.Count != 3)
                            throw new OptionException("Need two extra arguments", "");

                        var csprojFile = commandArgs[1];
                        var intermediatePackagePath = commandArgs[2];
                        var generatedItems = new List<(string SourcePath, string PackagePath)>();
                        var logger = new LoggerResult();
                        if (!PackAssetsHelper.Run(logger, csprojFile, intermediatePackagePath, generatedItems, packHostAssemblies, companionPackages: packCompanionPackages, packageKind: packPackageKind, packageToolkit: packPackageToolkit))
                        {
                            foreach (var message in logger.Messages)
                            {
                                Console.WriteLine(message);
                            }
                            return 1;
                        }
                        foreach (var generatedItem in generatedItems)
                        {
                            Console.WriteLine($"{generatedItem.SourcePath}|{generatedItem.PackagePath}");
                        }
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("{0}: {1}", exeName, e);
                if (e is OptionException)
                    p.WriteOptionDescriptions(Console.Out);
                return 1;
            }

            return 0;
        }
    }
}
