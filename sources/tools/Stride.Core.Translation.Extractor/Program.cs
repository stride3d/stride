// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Stride.Core.Annotations;
using Stride.Core.IO;
using Stride.Core.Translation.Providers;

namespace Stride.Core.Translation.Extractor
{
    internal static class Program
    {
        private static int Main([NotNull] string[] args)
        {
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
                HashSet<UFile> inputFiles = [];
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
                    options.InputFiles.Add("*.axaml");
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
                bool endOfOptions = false;
                for (int i = 0; i < args.Length; i++)
                {
                    var arg = args[i];

                    if (endOfOptions || !arg.StartsWith('-') || arg.Length == 1)
                    {
                        options.InputFiles.Add(arg);
                        continue;
                    }

                    if (arg == "--")
                    {
                        endOfOptions = true;
                        continue;
                    }

                    if (arg.StartsWith("--"))
                    {
                        // Long option: --name or --name=value
                        var name = arg[2..];
                        string? value = null;
                        var eq = name.IndexOf('=');
                        if (eq >= 0) { value = name[(eq + 1)..]; name = name[..eq]; }

                        switch (name)
                        {
                            case "directory":
                                value ??= NextArg(args, ref i, name, message);
                                if (value == null) return false;
                                options.InputDirs.Add(value);
                                break;
                            case "recursive":      options.Recursive = true; break;
                            case "exclude":
                                value ??= NextArg(args, ref i, name, message);
                                if (value == null) return false;
                                options.Excludes.Add(value);
                                break;
                            case "domain-name":
                                value ??= NextArg(args, ref i, name, message);
                                if (value == null) return false;
                                options.OutputFile = $"{value}.pot";
                                break;
                            case "backup":         options.Backup = true; break;
                            case "output":
                                value ??= NextArg(args, ref i, name, message);
                                if (value == null) return false;
                                options.OutputFile = value;
                                break;
                            case "merge":          options.Overwrite = false; break;
                            case "preserve-comments": options.PreserveComments = true; break;
                            case "verbose":        options.Verbose = true; break;
                            case "help":           options.ShowUsage = true; return true;
                            default:
                                message.AppendLine(string.Format(Tr._("Invalid option '{0}'"), $"--{name}"));
                                return false;
                        }
                    }
                    else
                    {
                        // Short option(s): -x or -xVALUE (for options with required args)
                        for (int j = 1; j < arg.Length; j++)
                        {
                            char opt = arg[j];
                            switch (opt)
                            {
                                case 'D':
                                case 'x':
                                case 'd':
                                case 'o':
                                    // Remaining chars are the value, or next arg
                                    string? val = j + 1 < arg.Length ? arg[(j + 1)..] : NextArg(args, ref i, opt.ToString(), message);
                                    if (val == null) return false;
                                    switch (opt)
                                    {
                                        case 'D': options.InputDirs.Add(val); break;
                                        case 'x': options.Excludes.Add(val); break;
                                        case 'd': options.OutputFile = $"{val}.pot"; break;
                                        case 'o': options.OutputFile = val; break;
                                    }
                                    j = arg.Length; // consumed rest of token
                                    break;
                                case 'r': options.Recursive = true; break;
                                case 'b': options.Backup = true; break;
                                case 'm': options.Overwrite = false; break;
                                case 'C': options.PreserveComments = true; break;
                                case 'v': options.Verbose = true; break;
                                case 'h': options.ShowUsage = true; return true;
                                default:
                                    message.AppendLine(string.Format(Tr._("Invalid option '{0}'"), $"-{opt}"));
                                    return false;
                            }
                        }
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

        private static string? NextArg(string[] args, ref int i, string optName, StringBuilder message)
        {
            if (++i < args.Length)
                return args[i];
            message.AppendLine(string.Format(Tr._("Option '{0}' requires an argument"), optName));
            return null;
        }

        private static void ShowUsage()
        {
            var nl = Environment.NewLine;
            var name = Assembly.GetExecutingAssembly().GetName().Name;
            Console.Write(
                $"Extract strings from C# or XAML source code files and then creates or updates PO template file{nl}{nl}" +
                $"Usage:{nl}" +
                $"    {name}[.exe] [options] [inputfile | filemask] ...{nl}{nl}" +
                $"   -D directory, --directory=directory    Add directory to the list of directories. Source files are searched relative to this list of directories{nl}" +
                $"                                          Use multiples options to specify more directories{nl}{nl}" +
                $"   -r, --recursive                        Process all subdirectories{nl}{nl}" +
                $"   -x, --exclude=filemask                 Exclude a filemask from the list of input{nl}{nl}" +
                $"   -d, --domain-name=name                 Use name.pot for output (instead of messages.pot){nl}{nl}" +
                $"   -b, --backup                           Create a backup file (.bak) in case of an existing file{nl}{nl}" +
                $"   -o file, --output=file                 Write output to specified file (instead of name.po or messages.po) {nl}{nl}" +
                $"   -m, --merge                            Merge with existing file instead of overwriting it{nl}{nl}" +
                $"   -C, --preserve-comments                Keep previous comments from existing file{nl}{nl}" +
                $"   -v, --verbose                          Verbose output{nl}{nl}" +
                $"   -h, --help                             Display this help and exit{nl}"
            );
        }
    }
}
