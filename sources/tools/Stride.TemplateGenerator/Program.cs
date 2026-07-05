// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Linq;
using Mono.Options;
using Stride.Assets.Templates;
using Stride.Core.Diagnostics;

namespace Stride.TemplateGenerator;

/// <summary>
/// Release-pipeline tool that produces <c>dotnet new</c> template content for Stride. Single
/// subcommand <c>preprocess-template</c> transforms raw template input (a sample directory) into
/// a directory the dotnet new template engine can consume: stage, GUID-placeholder substitution,
/// dep collapse, asset prune, source-name rename, camera-script injection, template.json.
/// </summary>
internal static class Program
{
    private static int Main(string[] args)
    {
        if (args.Length == 0)
        {
            PrintUsage();
            return 1;
        }

        var logger = GlobalLogger.GetLogger("StrideTemplates");
        var consoleListener = new ConsoleLogListener { LogMode = ConsoleLogMode.Always };
        GlobalLogger.GlobalMessageLogged += consoleListener;
        logger.ActivateLog(LogMessageType.Info);

        try
        {
            var subcommand = args[0];
            var rest = args.Skip(1).ToArray();
            return subcommand switch
            {
                "preprocess-template" => RunPreprocess(rest, logger),
                "aggregate-sdtpls"    => RunAggregateSdtpls(rest, logger),
                "-h" or "--help" or "help" => HelpAndZero(),
                _ => UnknownSubcommand(subcommand),
            };
        }
        catch (Exception ex)
        {
            logger.Error($"Unhandled exception: {ex}");
            return 1;
        }
        finally
        {
            GlobalLogger.GlobalMessageLogged -= consoleListener;
        }

        static int HelpAndZero() { PrintUsage(); return 0; }
        static int UnknownSubcommand(string s) { Console.Error.WriteLine($"Unknown subcommand: {s}"); PrintUsage(); return 1; }
    }

    private static int RunPreprocess(string[] args, ILogger logger)
    {
        var preprocessor = new TemplatePreprocessor();
        var showHelp = false;
        var options = new OptionSet
        {
            $"Usage: Stride.TemplateGenerator preprocess-template [options]",
            { "h|help", "Show this message", v => showHelp = v != null },
            { "input-path=", "Input directory (raw template content)", v => preprocessor.InputPath = v },
            { "output-path=", "Output staging directory", v => preprocessor.OutputDirectory = v },
            { "template-name=", "dotnet new short name (e.g. stride-game)", v => preprocessor.TemplateName = v },
            { "source-name=", "Original literal name to rename to 'MyTemplate' across staged content (auto-detected from first .sdpkg's Name field if omitted)", v => preprocessor.SourceName = v },
            { "engine-version=", "Stamps this engine version into produced .csproj files: $EngineVersion$ literals plus concrete engine-family Stride.* PackageReference versions (community Stride.*-prefixed packages pass through).", v => preprocessor.EngineVersion = v },
            { "skip-prune", "Skip the asset prune step. Templates ship larger but pack runs faster; diagnostic escape hatch.", _ => preprocessor.SkipPrune = true },
            { "platform-template-path=", "Reference template (typically NewGame) whose MyTemplate.{Linux,macOS,iOS,Android,Windows} folders are copied into the staged output when the input sample doesn't ship its own. Sibling-dir csproj/sdpkg references are rewritten to match the sample's library name. Omit to disable injection.", v => preprocessor.PlatformTemplatePath = v },
        };

        var extra = options.Parse(args);
        if (showHelp) { options.WriteOptionDescriptions(Console.Out); return 0; }
        if (extra.Count > 0) { logger.Error($"Unexpected args: {string.Join(", ", extra)}"); return 1; }

        return preprocessor.Run(logger) ? 0 : 1;
    }

    /// <summary>
    /// Concatenates a set of <c>.sdtpl</c> files (one per wrapped template) into a single
    /// multi-document YAML file at <c>--output</c>. Shipped at the package root as
    /// <c>templates.sdtpls</c>; GameStudio's bridge reads it to populate the New-Project
    /// dialog without loading each template's preprocessed content.
    /// </summary>
    private static int RunAggregateSdtpls(string[] args, ILogger logger)
    {
        string? output = null;
        var sampleDirs = new System.Collections.Generic.List<string>();
        var showHelp = false;
        var options = new OptionSet
        {
            "Usage: Stride.TemplateGenerator aggregate-sdtpls --output=<file> --sample=<dir> [--sample=<dir> ...]",
            { "h|help", "Show this message", v => showHelp = v != null },
            { "output=", "Aggregated output file (multi-document YAML)", v => output = v },
            { "sample=", "Sample directory; globs *.sdtpl at the top level (repeatable)", sampleDirs.Add },
        };

        var extra = options.Parse(args);
        if (showHelp) { options.WriteOptionDescriptions(Console.Out); return 0; }
        if (extra.Count > 0) { logger.Error($"Unexpected args: {string.Join(", ", extra)}"); return 1; }
        if (string.IsNullOrEmpty(output)) { logger.Error("--output is required"); return 1; }

        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(output)!);
        using var writer = new System.IO.StreamWriter(output);
        var first = true;
        var totalAggregated = 0;
        foreach (var dir in sampleDirs)
        {
            if (!System.IO.Directory.Exists(dir)) { logger.Warning($"Skipping missing sample dir: {dir}"); continue; }
            // Top-level *.sdtpl only — there's also a sibling .sdtpl/ directory containing icons
            // and screenshots; EnumerateFiles at TopDirectoryOnly skips it.
            var sdtpls = System.IO.Directory.EnumerateFiles(dir, "*.sdtpl", System.IO.SearchOption.TopDirectoryOnly).ToList();
            if (sdtpls.Count == 0) { logger.Warning($"No .sdtpl found in {dir}; sample will lack display metadata."); continue; }
            foreach (var sdtpl in sdtpls)
            {
                if (!first) writer.WriteLine("---");
                writer.Write(System.IO.File.ReadAllText(sdtpl).TrimEnd());
                writer.WriteLine();
                first = false;
                totalAggregated++;
            }
        }
        logger.Info($"Aggregated {totalAggregated} .sdtpl file(s) → {output}");
        return 0;
    }

    private static void PrintUsage()
    {
        Console.WriteLine("Usage: Stride.TemplateGenerator <subcommand> [options]");
        Console.WriteLine();
        Console.WriteLine("Subcommands:");
        Console.WriteLine("  preprocess-template      Turn raw template input into dotnet new template content");
        Console.WriteLine("  aggregate-sdtpls         Concatenate per-template .sdtpl files into a single multi-document YAML");
        Console.WriteLine();
        Console.WriteLine("Run 'Stride.TemplateGenerator <subcommand> --help' for subcommand-specific options.");
    }
}
