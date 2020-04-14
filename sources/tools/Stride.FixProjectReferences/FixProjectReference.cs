// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using Mono.Options;
using Stride.Core.Diagnostics;
using Stride.Core.VisualStudio;

namespace Stride.FixProjectReferences
{
    public static class FixProjectReference
    {
        [STAThread]
        static int Main(string[] args)
        {
            var exeName = Path.GetFileName(Assembly.GetExecutingAssembly().Location);
            var showHelp = false;
            var isSavingMode = false;
            int exitCode = 0;

            var p = new OptionSet
                {
                    "Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp) All Rights Reserved",
                    "Stride Fix Project References - Version: "
                    +
                    String.Format(
                        "{0}.{1}.{2}",
                        typeof(FixProjectReference).Assembly.GetName().Version.Major,
                        typeof(FixProjectReference).Assembly.GetName().Version.Minor,
                        typeof(FixProjectReference).Assembly.GetName().Version.Build) + string.Empty,
                    string.Format("Usage: {0} [options]* inputSlnFile", exeName),
                    string.Empty,
                    "=== Options ===",
                    string.Empty,
                    { "h|help", "Show this message and exit", v => showHelp = v != null },
                    { "s|save", "Save mode. By default doesn't save projects", v => isSavingMode = v != null },
                    string.Empty,
                    "Return codes: 0 (success), 1 (error), 2 (project needs to be updated)",
                };

            try
            {
                var inputFiles = p.Parse(args);
                if (showHelp)
                {
                    p.WriteOptionDescriptions(Console.Out);
                    return 0;
                }

                if (inputFiles.Count != 1)
                    throw new OptionException("Expect only one input file", "");

                var inputFile = inputFiles[0];

                var consoleLogListener = new ConsoleLogListener { LogMode = ConsoleLogMode.Always };
                GlobalLogger.GlobalMessageLogged += consoleLogListener;

                var log = GlobalLogger.GetLogger("FixProjectReference");
                if (!ProcessCopyLocals(log, inputFile, isSavingMode))
                    exitCode = 2; // Project needs to be updated
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

        public static bool ProcessCopyLocals(ILogger log, string inputFile, bool isSavingMode)
        {
            // Read .sln
            var solution = Solution.FromFile(inputFile);
            var result = true;

            // Process each project and select the one that will be processed
            foreach (var solutionProject in solution.Projects.ToArray())
            {
                // Is it really a project?
                if (!solutionProject.FullPath.EndsWith(".csproj") && !solutionProject.FullPath.EndsWith(".vcxproj"))
                    continue;

                // Load XML project
                var doc = XDocument.Load(solutionProject.FullPath);
                var ns = doc.Root.Name.Namespace;
                var allElements = doc.DescendantNodes().OfType<XElement>().ToList();

                var hasOutputPath = allElements.Any(element => element.Name.LocalName == "OutputPath" || element.Name.LocalName == "StrideOutputPath");
                var isTest = allElements.Any(element => element.Name.LocalName == "StrideOutputFolder" && element.Value.StartsWith("Tests"));
                if (!hasOutputPath && !isTest)
                {
                    bool projectUpdated = false;
                    //doc.Save(solutionProject.FullPath);
                    log.Info($"Check project [{solutionProject.FullPath}]");

                    foreach (var referenceNode in allElements.Where(element => element.Name.LocalName == "ProjectReference"))
                    {
                        var attr = referenceNode.Attribute("Include");
                        if (attr != null && (attr.Value.EndsWith(".csproj") || attr.Value.EndsWith(".vcxproj")))
                        {
                            var isPrivate = referenceNode.DescendantNodes().OfType<XElement>().FirstOrDefault(element => element.Name.LocalName == "Private");
                            bool referenceUpdated = false;
                            if (isPrivate == null)
                            {
                                referenceNode.Add(new XElement(XName.Get("Private", ns.NamespaceName)) { Value = "False" });
                                referenceUpdated = true;
                                projectUpdated = true;
                            }
                            else if (!string.IsNullOrEmpty(isPrivate.Value) && string.Compare(isPrivate.Value, "false", true, CultureInfo.InvariantCulture) != 0)
                            {
                                referenceUpdated = true;
                                isPrivate.Value = "False";
                                projectUpdated = true;
                            }
                            if (referenceUpdated)
                            {
                                var logMessage = $"[{solutionProject.Name}] -> Set Private to False [{Path.GetFileNameWithoutExtension(attr.Value)}]";
                                if (isSavingMode)
                                    log.Info(logMessage);
                                else
                                    log.Error(logMessage);
                            }
                        }
                    }

                    if (projectUpdated)
                    {
                        if (isSavingMode)
                        {
                            doc.Save(solutionProject.FullPath);
                            log.Info($"Project Updated [{solutionProject.Name}]");
                        }
                        else
                        {
                            log.Info($"Project [{solutionProject.Name}] needs to be updated. Run this command with -s switch");
                            result = false;
                        }
                    }
                }
            }
            return result;
        }
    }
}
