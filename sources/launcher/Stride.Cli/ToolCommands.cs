// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.CommandLine;
using Stride.Cli.Core;

// studio + asset: launch the per-version Stride SDK tools, resolving the version from the current directory's
// project (or an explicit --version).
internal static class ToolCommands
{
    // studio: open Game Studio (launches and returns immediately).
    public static Command CreateStudio(StrideVersionManager manager)
    {
        var path = new Argument<string?>("path")
        {
            Arity = ArgumentArity.ZeroOrOne,
            Description = "Solution to open. Defaults to the solution in the current directory.",
        };
        var version = new Option<string?>("--version") { Description = "Open with a specific Stride version instead of the one from a project in the current directory." };

        var command = new Command("studio", "Open Game Studio.");
        command.Arguments.Add(path);
        command.Options.Add(version);
        command.TreatUnmatchedTokensAsErrors = false;
        command.SetAction(parseResult =>
        {
            var resolved = CliVersion.ResolveOrReport(manager, parseResult.GetValue(version));
            if (resolved is null)
                return 1;

            // Open the solution in the current directory by default. Game Studio expects a .sln (opening a bare
            // .csproj crashes it), so only a solution is auto-selected — never a project.
            var args = new List<string>();
            var solution = parseResult.GetValue(path) ?? manager.FindSolution(Environment.CurrentDirectory);
            if (solution is not null)
                args.Add(solution);
            args.AddRange(parseResult.UnmatchedTokens);

            return Tools.Run(manager.LocateGameStudio(resolved), $"Game Studio for Stride {resolved}", args, wait: false);
        });

        return command;
    }

    // asset: advanced escape hatch — forward a verb and arguments straight to the Asset Compiler. Hidden because
    // 'stride upgrade' covers the common case and build/pack/generate-code normally run via 'dotnet build'/'pack'.
    public static Command CreateAsset(StrideVersionManager manager)
    {
        var version = new Option<string?>("--version") { Description = "Use a specific Stride version instead of the one from a project in the current directory." };

        var command = new Command("asset", "Run the Stride Asset Compiler directly (advanced; arguments are forwarded).") { Hidden = true };
        command.Options.Add(version);
        command.TreatUnmatchedTokensAsErrors = false;
        command.SetAction(parseResult =>
        {
            var resolved = CliVersion.ResolveOrReport(manager, parseResult.GetValue(version));
            return resolved is null
                ? 1
                : Tools.Run(manager.LocateAssetCompiler(resolved), $"the Asset Compiler for Stride {resolved}", parseResult.UnmatchedTokens, wait: true);
        });

        return command;
    }
}
