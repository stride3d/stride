// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Stride.Core.IO;
using Stride.Core.Translation.Providers;

namespace Stride.Core.Translation.Extractor
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            var dirOption = new Option<string[]>("--directory", "-D")
                { Description = "Add a directory to search for source files. Repeat for multiple directories." };
            var recursiveOption = new Option<bool>("--recursive", "-r")
                { Description = "Process all subdirectories." };
            var excludeOption = new Option<string[]>("--exclude", "-x")
                { Description = "Exclude a file mask from the input list. Repeat for multiple patterns." };
            var domainOption = new Option<string>("--domain-name", "-d")
                { Description = "Use <name>.pot as the output file (instead of messages.pot)." };
            var backupOption = new Option<bool>("--backup", "-b")
                { Description = "Create a .bak backup of any existing output file." };
            var outputOption = new Option<string>("--output", "-o")
                { Description = "Write output to the specified file." };
            var mergeOption = new Option<bool>("--merge", "-m")
                { Description = "Merge with existing file instead of overwriting it." };
            var commentsOption = new Option<bool>("--preserve-comments", "-C")
                { Description = "Keep previous comments from an existing file." };
            var verboseOption = new Option<bool>("--verbose", "-v")
                { Description = "Verbose output." };
            var patternsArg = new Argument<string[]>("patterns")
            {
                Description = "Input file masks (e.g. *.cs *.xaml). Defaults to *.cs *.xaml *.axaml.",
                Arity = ArgumentArity.ZeroOrMore,
            };

            var rootCommand = new RootCommand(
                "Extract translatable strings from C# and XAML source files into a PO template (.pot).")
            {
                dirOption, recursiveOption, excludeOption, domainOption,
                backupOption, outputOption, mergeOption, commentsOption,
                verboseOption, patternsArg,
            };

            rootCommand.SetAction(parseResult =>
            {
                var options = new Options
                {
                    Recursive        = parseResult.GetValue(recursiveOption),
                    Backup           = parseResult.GetValue(backupOption),
                    Overwrite        = !parseResult.GetValue(mergeOption),
                    PreserveComments = parseResult.GetValue(commentsOption),
                    Verbose          = parseResult.GetValue(verboseOption),
                };

                if (parseResult.GetValue(domainOption) is { } domain)
                    options.OutputFile = $"{domain}.pot";
                else if (parseResult.GetValue(outputOption) is { } output)
                    options.OutputFile = output;

                options.InputDirs.AddRange(parseResult.GetValue(dirOption) ?? []);
                options.Excludes.AddRange(parseResult.GetValue(excludeOption) ?? []);
                options.InputFiles.AddRange(parseResult.GetValue(patternsArg) ?? []);

                return Run(options);
            });

            // Show help when invoked with no arguments
            if (args.Length == 0)
                return rootCommand.Parse(["--help"]).Invoke();

            return rootCommand.Parse(args).Invoke();
        }

        private static int Run(Options options)
        {
            if (options.InputFiles.Count == 0)
            {
                options.InputFiles.Add("*.cs");
                options.InputFiles.Add("*.xaml");
                options.InputFiles.Add("*.axaml");
            }
            if (options.InputDirs.Count == 0)
                options.InputDirs.Add(Environment.CurrentDirectory);

            foreach (var dir in options.InputDirs)
            {
                if (!Directory.Exists(dir))
                {
                    Console.Error.WriteLine(Tr._("Input directory '{0}' not found"), dir);
                    return -1;
                }
            }

            try
            {
                TranslationManager.Instance.RegisterProvider(new GettextTranslationProvider());

                HashSet<UFile> inputFiles = [];
                var re = options.Excludes.Count > 0
                    ? new Regex(string.Join("|", options.Excludes.Select(x => Regex.Escape(x).Replace(@"\*", @".*"))))
                    : null;

                foreach (var path in options.InputDirs)
                {
                    foreach (var searchPattern in options.InputFiles)
                    {
                        var files = Directory.EnumerateFiles(path, searchPattern,
                                options.Recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)
                            .Where(f => !re?.IsMatch(f) ?? true);
                        foreach (var fileName in files)
                            inputFiles.Add(new UFile(fileName));
                    }
                }

                var messages = new List<Message>();
                messages.AddRange(new CSharpExtractor(inputFiles).ExtractMessages());
                messages.AddRange(new XamlExtractor(inputFiles).ExtractMessages());

                if (options.Verbose)
                    Console.WriteLine(Tr._n("Found {0} message.", "Found {0} messages.", messages.Count), messages.Count);

                var exporter = new POExporter(options);
                exporter.Merge(messages);
                exporter.Save();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(Tr._("Error during execution: {0}"), ex.Message);
                return 1;
            }

            return 0;
        }
    }
}
