// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.CommandLine;
using Stride.Core;
using Stride.Cli.Core;

namespace Stride.Cli.Legacy;

// Commands kept only for older Stride versions the modern engine now handles natively. Each is top-level but
// Hidden; the visible 'legacy' entry lists them so they stay discoverable without cluttering the main help.
internal static class LegacyCommands
{
    // Shader C# codegen moved to a build-time Roslyn source generator in Stride 4.4; 4.0-4.3 produced it via the
    // VS Custom Tool, driven by Stride.VisualStudio.Commands.
    private static readonly Version SourceGeneratorFloor = new(4, 4);

    // Oldest Stride whose Commands we can drive over ServiceWire; 3.x (Xenko-era) is out of scope.
    private static readonly Version MinimumSupportedVersion = new(4, 0);

    /// <summary>
    ///   A visible entry that lists the born-legacy commands (passed in explicitly — not every Hidden command,
    ///   since some are hidden for other reasons like the advanced 'asset' escape hatch). It does nothing else.
    /// </summary>
    public static Command CreateLister(IReadOnlyList<Command> legacyCommands)
    {
        var command = new Command("legacy", "List commands kept for older (pre-4.4) Stride projects.");
        command.SetAction(_ =>
        {
            Console.WriteLine("Legacy commands (hidden from the main help; run 'stride <name> --help' for details):");
            foreach (var subcommand in legacyCommands.OrderBy(subcommand => subcommand.Name))
                Console.WriteLine($"  {subcommand.Name,-32}{subcommand.Description}");
        });
        return command;
    }

    /// <summary>
    ///   Regenerates the C# produced from .sdsl (shader keys) and .sdfx (effect code) files for a 4.0-4.3
    ///   project, the way the old VS Custom Tool did. Hidden: 4.4+ generates it at build.
    /// </summary>
    public static Command CreateGenerateLegacyShaderCode(StrideVersionManager manager)
    {
        var path = new Argument<string?>("path")
        {
            Arity = ArgumentArity.ZeroOrOne,
            Description = "Shader file (.sdsl/.sdfx), project, or solution to regenerate. Defaults to the solution or project in the current directory.",
        };
        var version = new Option<string?>("--version") { Description = "Use a specific Stride version instead of the one resolved from the project." };

        var command = new Command("generate-legacy-shader-code", "Regenerate the C# from .sdsl/.sdfx shaders (Stride 4.0-4.3; 4.4+ generates it at build).") { Hidden = true };
        command.Arguments.Add(path);
        command.Options.Add(version);
        command.SetAction(parseResult =>
        {
            var explicitInput = parseResult.GetValue(path);
            if (explicitInput is not null && !File.Exists(explicitInput))
            {
                Console.Error.WriteLine($"'{explicitInput}' does not exist.");
                return 1;
            }

            var input = explicitInput ?? manager.FindSolution(Environment.CurrentDirectory) ?? manager.FindProject(Environment.CurrentDirectory);
            if (string.IsNullOrEmpty(input))
            {
                Console.Error.WriteLine("No shader file, project, or solution found in the current directory. Run it from the project folder, or pass a path.");
                return 1;
            }

            var workingDirectory = Path.GetDirectoryName(Path.GetFullPath(input))!;
            PackageVersion? resolved;
            try
            {
                resolved = manager.ResolveVersion(parseResult.GetValue(version), workingDirectory);
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine(exception.Message);
                return 1;
            }

            if (resolved is null)
            {
                Console.Error.WriteLine("Could not determine the Stride version for this project.");
                return 1;
            }

            if (resolved.Version >= SourceGeneratorFloor)
            {
                Console.WriteLine($"Stride {resolved} generates shader C# at build (source generator); there's nothing to regenerate.");
                return 0;
            }

            if (resolved.Version < MinimumSupportedVersion)
            {
                Console.Error.WriteLine($"Regenerating shader code isn't supported for Stride {resolved}; it requires Stride {MinimumSupportedVersion} or newer.");
                return 1;
            }

            var shaders = EnumerateShaders(input);
            if (shaders.Count == 0)
            {
                Console.WriteLine("No .sdsl or .sdfx files found to regenerate.");
                return 0;
            }

            Console.WriteLine($"Regenerating shader code for {shaders.Count} file(s) with Stride {resolved}...");
            try
            {
                using var generator = LegacyShaderCodeGenerator.Start(resolved);
                var regenerated = 0;
                foreach (var shader in shaders)
                {
                    var generated = generator.Generate(shader, File.ReadAllText(shader));
                    if (generated is null || generated.Length == 0)
                    {
                        Console.Error.WriteLine($"  {Path.GetFileName(shader)}: no output");
                        continue;
                    }

                    var output = shader + ".cs";
                    File.WriteAllBytes(output, generated);
                    Console.WriteLine($"  {Path.GetFileName(output)}");
                    regenerated++;
                }

                Console.WriteLine($"Regenerated {regenerated} file(s).");
                return regenerated > 0 ? 0 : 1;
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine(exception.Message);
                for (var inner = exception.InnerException; inner is not null; inner = inner.InnerException)
                    Console.Error.WriteLine($"  -> {inner.GetType().Name}: {inner.Message}");
                return 1;
            }
        });

        return command;
    }

    private static readonly string[] ShaderExtensions = [".sdsl", ".sdfx"];

    // The shader files to regenerate: the file itself when given one, else every .sdsl/.sdfx under the
    // project/solution directory.
    private static IReadOnlyList<string> EnumerateShaders(string input)
    {
        if (ShaderExtensions.Any(extension => input.EndsWith(extension, StringComparison.OrdinalIgnoreCase)))
            return File.Exists(input) ? [Path.GetFullPath(input)] : [];

        var directory = Path.GetDirectoryName(Path.GetFullPath(input))!;
        return Directory.EnumerateFiles(directory, "*.*", SearchOption.AllDirectories)
            .Where(file => ShaderExtensions.Any(extension => file.EndsWith(extension, StringComparison.OrdinalIgnoreCase)))
            .ToList();
    }
}
