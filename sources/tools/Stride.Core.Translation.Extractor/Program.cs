// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using GNU.Getopt;
using Stride.Core.Annotations;
using Stride.Core.IO;
using Stride.Core.Translation.Providers;

namespace Stride.Core.Translation.Extractor
{
    internal static class Program
    {
        private static readonly LongOpt[] LOpts = {
            new LongOpt("directory", Argument.Required, null, 'D'),
            new LongOpt("recursive", Argument.No, null, 'r'),
            new LongOpt("exclude", Argument.Required, null, 'x'),
            new LongOpt("domain-name", Argument.Required, null, 'd'),
            new LongOpt("backup", Argument.No, null, 'b'),
            new LongOpt("output", Argument.Required, null, 'o'),
            new LongOpt("merge", Argument.No, null, 'm'),
            new LongOpt("preserve-comments", Argument.No, null, 'C'),
            new LongOpt("verbose", Argument.No, null, 'v'),
            new LongOpt("help", Argument.No, null, 'h'),
        };
        private static readonly string SOpts = "-:D:rx:d:bo:mCvh";

        private static int Main([NotNull] string[] args)
        {
#if DEBUG
            // Allow to attach debugger
            Console.ReadLine();
#endif // DEBUG
            if (args.Length == 0)
            {
                ShowUsage();
                return -1;
            }

            if (!ParseOptions(args, out var options, out var message))
            {
                Console.WriteLine(message.ToString());
                return -1;
            }

            if (options.ShowUsage)
            {
                ShowUsage();
                return 0;
            }

            if (!CheckOptions(options, out message))
            {
                Console.WriteLine(message.ToString());
                return -1;
            }

            try
            {
                // Initialize translation
                TranslationManager.Instance.RegisterProvider(new GettextTranslationProvider());

                // Compute the list of input files
                ISet<UFile> inputFiles = new HashSet<UFile>();
                var re = options.Excludes.Count > 0 ? new Regex(string.Join("|", options.Excludes.Select(x => Regex.Escape(x).Replace(@"\*", @".*")))) : null;
                foreach (var path in options.InputDirs)
                {
                    foreach (var searchPattern in options.InputFiles)
                    {
                        var files = Directory.EnumerateFiles(path, searchPattern, options.Recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)
                            .Where(f => !re?.IsMatch(f) ?? true);
                        foreach (var fileName in files)
                        {
                            inputFiles.Add(new UFile(fileName));
                        }
                    }
                }
                // Extract all messages from the input files
                var messages = new List<Message>();
                messages.AddRange(new CSharpExtractor(inputFiles).ExtractMessages());
                messages.AddRange(new XamlExtractor(inputFiles).ExtractMessages());
                if (options.Verbose)
                    Console.WriteLine(Tr._n("Found {0} message.", "Found {0} messages.", messages.Count), messages.Count);
                // Export/merge messages
                var exporter = new POExporter(options);
                exporter.Merge(messages);
                exporter.Save();
            }
            catch (Exception ex)
            {
                Console.WriteLine(Tr._("Error during execution: {0}"), ex.Message);
                return 1;
            }

            return 0;
        }

        private static bool CheckOptions([NotNull] Options options, [NotNull] out StringBuilder message)
        {
            message = new StringBuilder();
            try
            {
                if (options.InputFiles.Count == 0)
                {
                    // Add all supported formats
                    options.InputFiles.Add("*.cs");
                    options.InputFiles.Add("*.xaml");
                }
                if (options.InputDirs.Count == 0)
                {
                    options.InputDirs.Add(Environment.CurrentDirectory);
                }

                foreach (var dir in options.InputDirs)
                {
                    if (!Directory.Exists(dir))
                    {
                        message.AppendLine(string.Format(Tr._("Input directory '{0}' not found"), dir));
                        return false;
                    }
                }
            }
            catch (Exception e)
            {
                message.Append(e.Message);
                return false;
            }

            return true;
        }

        private static bool ParseOptions([NotNull] string[] args, [NotNull] out Options options, [NotNull] out StringBuilder message)
        {
            options = new Options();
            message = new StringBuilder();

            try
            {
                var getopt = new Getopt(Assembly.GetExecutingAssembly().GetName().Name, args, SOpts, LOpts) { Opterr = false };
                int option;
                while ((option = getopt.getopt()) != -1)
                {
                    switch (option)
                    {
                        case 1:
                            options.InputFiles.Add(getopt.Optarg);
                            break;

                        case 'D':
                            options.InputDirs.Add(getopt.Optarg);
                            break;

                        case 'r':
                            options.Recursive = true;
                            break;

                        case 'x':
                            options.Excludes.Add(getopt.Optarg);
                            break;

                        case 'd':
                            options.OutputFile = $"{getopt.Optarg}.pot";
                            break;

                        case 'b':
                            options.Backup = true;
                            break;

                        case 'o':
                            options.OutputFile = getopt.Optarg;
                            break;

                        case 'm':
                            options.Overwrite = false;
                            break;

                        case 'C':
                            options.PreserveComments = true;
                            break;

                        case 'v':
                            options.Verbose = true;
                            break;

                        case 'h':
                            options.ShowUsage = true;
                            return true;

                        case ':':
                            message.AppendLine(string.Format(Tr._("Option '{0}' requires an argument"), getopt.OptoptStr));
                            return false;

                        case '?':
                            message.AppendLine(string.Format(Tr._("Invalid option '{0}'"), getopt.OptoptStr));
                            return false;

                        default:
                            ShowUsage();
                            return false;
                    }
                }

                if (getopt.Opterr)
                {
                    message.AppendLine();
                    message.Append(Tr._("Error in the command line options. Use -h to display the options usage."));
                    return false;
                }
            }
            catch (Exception e)
            {
                message.Append(e.Message);
                return false;
            }

            return true;
        }

        private static void ShowUsage()
        {
            var newLine = Environment.NewLine;
            Console.Write(
                $"Extract strings from C# or XAML source code files and then creates or updates PO template file{newLine}{newLine}" +
                $"Usage:{newLine}" +
                $"    {Assembly.GetExecutingAssembly().GetName().Name}[.exe] [options] [inputfile | filemask] ...{newLine}{newLine}" +
                $"   -D directory, --directory=directory    Add directory to the list of directories. Source files are searched relative to this list of directories{newLine}" +
                $"                                          Use multiples options to specify more directories{newLine}{newLine}" +
                $"   -r, --recursive                        Process all subdirectories{newLine}{newLine}" +
                $"   -x, --exclude=filemask                 Exclude a filemask from the list of input{newLine}{newLine}" +
                $"   -d, --domain-name=name                 Use name.pot for output (instead of messages.pot){newLine}{newLine}" +
                $"   -b, --backup                           Create a backup file (.bak) in case of an existing file{newLine}{newLine}" +
                $"   -o file, --output=file                 Write output to specified file (instead of name.po or messages.po) {newLine}{newLine}" +
                $"   -m, --merge                            Merge with existing file instead of overwriting it{newLine}{newLine}" +
                $"   -C, --preserve-comments                Keep previous comments from existing file{newLine}{newLine}" +
                $"   -v, --verbose                          Verbose output{newLine}{newLine}" +
                $"   -h, --help                             Display this help and exit{newLine}"
            );
        }
    }
}
