// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.CommandLine;
using System.CommandLine.Help;
using System.CommandLine.Invocation;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Edge.Template;
using Stride.Assets.Templates;
using Stride.Core;
using Stride.Launcher.Core;

// new: create a project from an installed Stride version's bundled templates.
internal static class NewCommand
{
    public static Command Create(StrideVersionManager manager)
    {
        var templateArgument = new Argument<string?>("template")
        {
            Arity = ArgumentArity.ZeroOrOne,
            Description = "Template short name (e.g. stride-game). Lists the available templates if omitted.",
        };
        var nameOption = new Option<string?>("--name", "-n") { Description = "Name of the generated project." };
        var outputOption = new Option<string?>("--output", "-o") { Description = "Output directory (defaults to ./<name>)." };
        var packageOption = new Option<string[]>("--package", "-p")
        {
            Description = "Additional template package to include (a package id, or a path to a local .nupkg). Repeatable.",
            Arity = ArgumentArity.ZeroOrMore,
            AllowMultipleArgumentsPerToken = true,
        };
        var versionOption = new Option<string?>("--version") { Description = "Use a specific Stride version instead of the newest installed one." };
        var forceOption = new Option<bool>("--force") { Description = "Create the project even if the output directory already exists and is not empty (overwrites)." };

        var command = new Command("new", "Create a project from an installed Stride version's templates.");
        command.Arguments.Add(templateArgument);
        command.Options.Add(nameOption);
        command.Options.Add(outputOption);
        command.Options.Add(packageOption);
        command.Options.Add(versionOption);
        command.Options.Add(forceOption);
        // Any remaining --key value / --key=value tokens are forwarded as template parameters.
        command.TreatUnmatchedTokensAsErrors = false;
        // `stride new <template> --help` lists that template's parameters after the command's own help.
        var helpOption = new HelpOption();
        helpOption.Action = new TemplateHelpAction((HelpAction)helpOption.Action!, manager, templateArgument, versionOption);
        command.Options.Add(helpOption);

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            // `new` starts from no project, so use --version or the newest installed version (not the directory).
            var versionSpec = parseResult.GetValue(versionOption);
            var version = versionSpec is not null ? new PackageVersion(versionSpec) : manager.GetDefault()?.Version;
            if (version is null)
            {
                Console.Error.WriteLine("No Stride version is installed. Run 'stride sdk install' first.");
                return 1;
            }

            // Loading the registry installs/scans the template packages, which can take several seconds. Show a
            // transient status on stderr so it never mixes into the listing on stdout.
            var showStatus = !Console.IsErrorRedirected;
            var status = $"Scanning Stride {version} templates...";
            Console.Error.Write(showStatus ? status : status + Environment.NewLine);
            void ClearStatus()
            {
                if (showStatus)
                    Console.Error.Write('\r' + new string(' ', status.Length) + '\r');
            }

            DotNetNewTemplateRegistry? registry;
            try
            {
                registry = await manager.OpenTemplateRegistry(version, parseResult.GetValue(packageOption));
            }
            catch (Exception exception)
            {
                ClearStatus();
                Console.Error.WriteLine(exception.Message);
                return 1;
            }

            if (registry is null)
            {
                ClearStatus();
                Console.Error.WriteLine($"Stride {version} does not ship project templates.");
                return 1;
            }

            using (registry)
            {
                var templates = await registry.GetTemplatesAsync(cancellationToken);
                ClearStatus();
                var templateName = parseResult.GetValue(templateArgument);

                // No template given: list what this version offers, grouped by source package.
                if (string.IsNullOrEmpty(templateName))
                {
                    Console.WriteLine($"Templates available in Stride {version}:");
                    // Size the name column to the longest short name so descriptions line up across all groups,
                    // capped so one very long template name can't push every description far to the right.
                    var width = Math.Min(templates.Select(info => info.ShortNameList.FirstOrDefault()?.Length ?? 0).DefaultIfEmpty(0).Max() + 2, 30);
                    foreach (var group in templates.GroupBy(SourcePackage).OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase))
                    {
                        Console.WriteLine();
                        Console.WriteLine($"{group.Key}:");
                        foreach (var info in group.OrderBy(info => info.ShortNameList.FirstOrDefault(), StringComparer.OrdinalIgnoreCase))
                            Console.WriteLine($"  {(info.ShortNameList.FirstOrDefault() ?? string.Empty).PadRight(width)}{info.Name}");
                    }
                    return 0;
                }

                var template = ResolveTemplate(templates, templateName);
                if (template is null)
                {
                    Console.Error.WriteLine($"No template '{templateName}' in Stride {version}. Run 'stride new' to list templates.");
                    return 1;
                }

                var name = parseResult.GetValue(nameOption) ?? template.DefaultName ?? templateName;
                var output = parseResult.GetValue(outputOption) ?? Path.Combine(Environment.CurrentDirectory, name);

                // Refuse to write into an existing non-empty directory unless --force (mirrors `dotnet new`),
                // so a repeated command doesn't silently overwrite a project.
                if (!parseResult.GetValue(forceOption) && Directory.Exists(output) && Directory.EnumerateFileSystemEntries(output).Any())
                {
                    Console.Error.WriteLine($"The output directory '{output}' already exists and is not empty. Choose another name with --name, a different --output, or pass --force to overwrite.");
                    return 1;
                }

                // Forward any extra --key value tokens as template parameters (e.g. --platforms windows,linux).
                var parameters = new Dictionary<string, string>();
                if (!TryParseParameters(parseResult.UnmatchedTokens, template, parameters, out var parameterError))
                {
                    Console.Error.WriteLine(parameterError);
                    return 1;
                }

                var resolvedName = template.ShortNameList.FirstOrDefault() ?? templateName;
                Console.WriteLine($"Creating '{name}' from {resolvedName} (Stride {version})...");
                var result = await registry.InstantiateAsync(template, name, output, parameters, cancellationToken);
                if (result.Status != CreationResultStatus.Success)
                {
                    Console.Error.WriteLine($"Template creation failed: {result.ErrorMessage}");
                    return 1;
                }

                Console.WriteLine($"Created '{name}' in {output}.");

                // Upgrade to the resolved version when the template targets an older engine; skipped (no
                // restore) when it already matches.
                var solutionFile = Directory.EnumerateFiles(output, "*.sln", SearchOption.TopDirectoryOnly).FirstOrDefault();
                var templateVersion = ReadGeneratedEngineVersion(output);
                if (solutionFile is not null && templateVersion is not null && IsOlderThan(templateVersion, version))
                {
                    var compiler = manager.LocateAssetCompiler(version);
                    if (compiler is null)
                        Console.Error.WriteLine($"Warning: no Asset Compiler for Stride {version} is installed; leaving the project on {templateVersion}.");
                    else
                    {
                        Console.WriteLine($"Upgrading the project from Stride {templateVersion} to {version}...");
                        Tools.Run(compiler, $"the Asset Compiler for Stride {version}", ["upgrade", solutionFile], wait: true);
                    }
                }

                return 0;
            }
        });

        return command;
    }

    // Parses dotnet-new-style "--key value", "--key=value" and bare "--flag" tokens into template parameters
    // (keyed by symbol name; multi-value accepts comma/'|'/repeats). Unknown names are rejected.
    private static bool TryParseParameters(
        IReadOnlyList<string> tokens, ITemplateInfo template, Dictionary<string, string> parameters, out string? error)
    {
        error = null;
        var known = UserParameters(template).ToDictionary(parameter => parameter.Name, StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < tokens.Count; i++)
        {
            var token = tokens[i];
            if (!token.StartsWith("--", StringComparison.Ordinal))
            {
                error = $"Unexpected argument '{token}'. Pass template parameters as --name value.";
                return false;
            }

            var body = token[2..];
            var separator = body.IndexOf('=');
            string key;
            string? value;
            if (separator >= 0)
            {
                key = body[..separator];
                value = body[(separator + 1)..];
            }
            else
            {
                key = body;
                // The next token is the value unless it is another option; a lone token is a bool flag.
                value = i + 1 < tokens.Count && !tokens[i + 1].StartsWith("--", StringComparison.Ordinal) ? tokens[++i] : null;
            }

            if (!known.TryGetValue(key, out var definition))
            {
                var described = Describe(template);
                error = described.Count == 0
                    ? $"Unknown template parameter '--{key}'. This template has no parameters."
                    : $"Unknown template parameter '--{key}'.{Environment.NewLine}Available parameters:{Environment.NewLine}{string.Join(Environment.NewLine, described)}";
                return false;
            }

            if (value is null)
            {
                if (!string.Equals(definition.DataType, "bool", StringComparison.OrdinalIgnoreCase))
                {
                    error = $"Parameter '--{key}' requires a value.";
                    return false;
                }
                value = "true";
            }

            // Normalize comma-separated multi-values to '|', and merge a repeated option into one value.
            value = value.Replace(',', '|');
            parameters[definition.Name] = parameters.TryGetValue(definition.Name, out var existing) ? $"{existing}|{value}" : value;
        }

        return true;
    }

    // The package a template came from, used as its listing group header — grouping by provenance without
    // hard-coding the known package set. Derived from the mount point file name, which is "<PackageId>.<Version>"
    // (e.g. "Stride.Templates.Games.4.4.0-dev4.nupkg" -> "Stride.Templates.Games"), by keeping the leading
    // non-numeric segments and dropping the version.
    private static string SourcePackage(ITemplateInfo template)
    {
        var name = Path.GetFileNameWithoutExtension(template.MountPointUri ?? string.Empty);
        if (string.IsNullOrEmpty(name))
            return "Other templates";

        var packageId = string.Join('.', name.Split('.').TakeWhile(segment => segment.Length == 0 || !char.IsDigit(segment[0])));
        return string.IsNullOrEmpty(packageId) ? name : packageId;
    }

    // Resolves a template by short name, with or without the redundant "stride-" prefix. Tries the name
    // as-is first, so a non-prefixed third-party template still resolves.
    private static ITemplateInfo? ResolveTemplate(IReadOnlyList<ITemplateInfo> templates, string name)
    {
        return Find(name) ?? (name.StartsWith("stride-", StringComparison.OrdinalIgnoreCase) ? null : Find($"stride-{name}"));

        ITemplateInfo? Find(string candidate)
            => templates.FirstOrDefault(info => info.ShortNameList.Contains(candidate, StringComparer.OrdinalIgnoreCase));
    }

    // The user-facing parameters of a template (excludes the implicit name, non-parameter symbols, and
    // single-choice tag symbols like language/type).
    private static IEnumerable<ITemplateParameter> UserParameters(ITemplateInfo template)
        => template.ParameterDefinitions.Where(parameter =>
            string.Equals(parameter.Type, "parameter", StringComparison.Ordinal)
            && !parameter.IsName
            && !string.Equals(parameter.Name, "name", StringComparison.Ordinal)
            && !(string.Equals(parameter.DataType, "choice", StringComparison.OrdinalIgnoreCase) && parameter.Choices is { Count: <= 1 }));

    // One "  --name <type> [choices] (default: x)" line per user-facing parameter (empty list if none).
    private static IReadOnlyList<string> Describe(ITemplateInfo template)
        => UserParameters(template).Select(parameter =>
        {
            var choices = parameter.Choices is { Count: > 0 } ? $" [{string.Join("|", parameter.Choices.Keys)}]" : "";
            var fallback = string.IsNullOrEmpty(parameter.DefaultValue) ? "" : $" (default: {parameter.DefaultValue})";
            return $"  --{parameter.Name} <{parameter.DataType}>{choices}{fallback}";
        }).ToList();

    // True if the template's engine version is semantically older than requested (so an upgrade is warranted);
    // equal or newer is left alone.
    private static bool IsOlderThan(string templateVersion, PackageVersion requested)
    {
        try
        {
            return new PackageVersion(templateVersion).CompareTo(requested) < 0;
        }
        catch
        {
            return !string.Equals(templateVersion, requested.ToString(), StringComparison.OrdinalIgnoreCase);
        }
    }

    // The Stride engine version a freshly generated project references (the template's build-time engine),
    // read from the first project that pins Stride.Engine. Null if none is found.
    private static string? ReadGeneratedEngineVersion(string output)
    {
        foreach (var project in Directory.EnumerateFiles(output, "*.csproj", SearchOption.AllDirectories))
        {
            var match = System.Text.RegularExpressions.Regex.Match(File.ReadAllText(project), "Stride\\.Engine\"\\s+Version=\"([^\"]+)\"");
            if (match.Success)
                return match.Groups[1].Value;
        }

        return null;
    }

    // Help action for `stride new`: runs the standard help, then appends the chosen template's parameters.
    // Best-effort — omits the section if templates can't be loaded.
    private sealed class TemplateHelpAction : SynchronousCommandLineAction
    {
        private readonly HelpAction defaultHelp;
        private readonly StrideVersionManager manager;
        private readonly Argument<string?> templateArgument;
        private readonly Option<string?> versionOption;

        public TemplateHelpAction(HelpAction defaultHelp, StrideVersionManager manager,
            Argument<string?> templateArgument, Option<string?> versionOption)
        {
            this.defaultHelp = defaultHelp;
            this.manager = manager;
            this.templateArgument = templateArgument;
            this.versionOption = versionOption;
        }

        public override int Invoke(ParseResult parseResult)
        {
            defaultHelp.Invoke(parseResult);

            var templateName = parseResult.GetValue(templateArgument);
            if (string.IsNullOrEmpty(templateName))
                return 0;

            var versionSpec = parseResult.GetValue(versionOption);
            var version = versionSpec is not null ? new PackageVersion(versionSpec) : manager.GetDefault()?.Version;
            if (version is null)
                return 0;

            try
            {
                using var registry = manager.OpenTemplateRegistry(version).GetAwaiter().GetResult();
                var templates = registry?.GetTemplatesAsync().GetAwaiter().GetResult();
                var template = templates is null ? null : ResolveTemplate(templates, templateName);
                if (template is null)
                    return 0;

                var lines = Describe(template);
                Console.WriteLine();
                Console.WriteLine(lines.Count == 0 ? $"Template '{templateName}' has no parameters." : $"Parameters for '{templateName}':");
                foreach (var line in lines)
                    Console.WriteLine(line);
            }
            catch
            {
                // Omit the parameter section if templates can't be loaded.
            }

            return 0;
        }
    }
}
