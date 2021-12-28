// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.IO;
using System.Reflection;
using System.Security.Principal;
using Microsoft.Win32;
using Mono.Options;

namespace Stride.StorageTool
{
    /// <summary>
    /// Tool to manage storage/bundles.
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {  var exeName = Path.GetFileName(Assembly.GetExecutingAssembly().Location);
            var showHelp = false;
            int exitCode = 0;

            var p = new OptionSet
                {
                    "Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp) All Rights Reserved",
                    "Storage Tool - Version: "
                    +
                    String.Format(
                        "{0}.{1}.{2}",
                        typeof(Program).Assembly.GetName().Version.Major,
                        typeof(Program).Assembly.GetName().Version.Minor,
                        typeof(Program).Assembly.GetName().Version.Build) + string.Empty,
                    string.Format("Usage: {0} command [options]*", exeName),
                    string.Empty,
                    "=== command ===",
                    string.Empty,
                    "view [bundleFile]",
                    "register",
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
                    Environment.Exit(0);
                }

                if (commandArgs.Count == 0)
                    throw new OptionException("Expecting a command", "");

                var command = commandArgs[0];
                switch (command)
                {
                    case "view":
                        if (commandArgs.Count != 2)
                        {
                            throw new OptionException("View command expecting a path to bundle file","");
                        }
                        StorageToolApp.View(commandArgs[1]);
                        break;
                    case "register":
                        //[HKEY_CURRENT_USER\Software\Classes\.bundle]
                        //@="bundlefile"
                        //[HKEY_CURRENT_USER\Software\Classes\bundlefile]
                        //@="Stride Bundle file Extension"
                        //[HKEY_CURRENT_USER\Software\Classes\bundlefile\shell\View\command]
                        //@="StorageTool.exe %1"

                        var classesKey = Registry.CurrentUser.OpenSubKey("Software\\Classes", RegistryKeyPermissionCheck.ReadWriteSubTree);
                        var bundleKey = classesKey.CreateSubKey(".bundle");
                        bundleKey.SetValue(null, "bundlefile");

                        var bundlefileKey = classesKey.CreateSubKey("bundlefile");
                        bundlefileKey.SetValue(null, "Stride Bundle file Extension");

                        var commandKey = bundlefileKey.CreateSubKey("shell").CreateSubKey("View").CreateSubKey("command");
                        commandKey.SetValue(null, Assembly.GetExecutingAssembly().Location + " view %1");

                        break;
                    default:
                        throw new OptionException(string.Format("Invalid command [{0}]", command), "");
                }
            }
            catch (Exception e)
            {
                LogError("{0}: {1}", exeName, e is OptionException || e is StorageAppException ? e.Message : e.ToString());
                if (e is OptionException)
                    p.WriteOptionDescriptions(Console.Out);
                
            }

            Environment.Exit(exitCode);
        }

        public static void LogError(string message, params object[] args)
        {
            var color = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(message, args);
            Console.ForegroundColor = color;
        }
    }
}
