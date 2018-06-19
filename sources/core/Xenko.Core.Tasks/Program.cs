// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.IO;
using System.Reflection;
using Microsoft.Build.Locator;
using Mono.Options;

namespace Xenko.Core.Tasks
{
    static class Program
    {
        public static int Main(string[] args)
        {
            MSBuildLocator.RegisterDefaults();
            return RealMain(args);
        }

        public static int RealMain(string[] args)
        {
            var exeName = Path.GetFileName(Assembly.GetExecutingAssembly().Location);
            var showHelp = false;

            var p = new OptionSet
            {
                "Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp) All Rights Reserved",
                "Xenko Router Server - Version: "
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
                if (commandArgs.Count == 0)
                    throw new OptionException("You need to specify a command", "");

                switch (commandArgs[0])
                {
                    case "locate-devenv":
                    {
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
