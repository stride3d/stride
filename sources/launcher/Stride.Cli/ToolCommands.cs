// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.CommandLine;
using Stride;
using Stride.Cli.Core;
using Stride.Core;
using Stride.Core.Assets;

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
        var framework = new Option<string?>(DotNetHostSelector.FrameworkArg) { Description = "Run Game Studio on at least this .NET major (e.g. net11.0); a solution that needs a newer one still gets it." };
        var editor = new Option<GameStudioEditor?>("--editor") { Description = "Game Studio to open, wpf or avalonia. Defaults to WPF on Windows when the version has it, else Avalonia. Off Windows, WPF runs under Wine (wine on the PATH, its prefix set up for Stride)." };

        var command = new Command("studio", "Open Game Studio.");
        command.Arguments.Add(path);
        command.Options.Add(version);
        command.Options.Add(framework);
        command.Options.Add(editor);
        command.TreatUnmatchedTokensAsErrors = false;
        command.SetAction(parseResult =>
        {
            var resolved = CliVersion.ResolveOrReport(manager, parseResult.GetValue(version));
            if (resolved is null)
                return 1;
            var chosen = ChooseEditor(manager, resolved, parseResult.GetValue(editor));
            if (chosen is null)
                return 1;
            var underWine = !chosen.Value.RunsNatively();

            // Open the solution in the current directory by default. Game Studio expects a .sln (opening a bare
            // .csproj crashes it), so only a solution is auto-selected — never a project.
            var args = new List<string>();
            var solution = parseResult.GetValue(path) ?? manager.FindSolution(Environment.CurrentDirectory);
            if (solution is not null)
                args.Add(underWine ? Wine.ToWindowsPath(solution) : solution);
            args.AddRange(parseResult.UnmatchedTokens);
            if (parseResult.GetValue(framework) is { } explicitFramework)
                args.AddRange([DotNetHostSelector.FrameworkArg, explicitFramework]);

            var executable = manager.LocateGameStudio(resolved, chosen.Value);
            var description = $"the {chosen.Value.DisplayName()} Game Studio for Stride {resolved}";
            return underWine
                ? Tools.RunUnderWine(executable, description, args)
                : Tools.Run(executable, description, args, wait: false, sessionPath: solution);
        });

        return command;
    }

    private const string WineRequired = "runs on Windows only, or under Wine: put wine on the PATH, with its prefix set up for Stride";

    // The Game Studio to open for a version: the requested one when it is installed and can run here (natively or
    // under Wine), else the default among the installed ones. Prints why and returns null when there is none to open.
    // A version with no Game Studio installed goes on with the default, so that the error is the usual "Could not find".
    private static GameStudioEditor? ChooseEditor(StrideVersionManager manager, PackageVersion version, GameStudioEditor? requested)
    {
        var installed = manager.GetInstalledEditors(version);
        var wine = Wine.Find() is not null;
        if (requested is { } editor)
        {
            if (!editor.RunsNatively() && !wine)
            {
                Console.Error.WriteLine($"The {editor.DisplayName()} Game Studio {WineRequired}.");
                return null;
            }
            if (installed.Count > 0 && !installed.Contains(editor))
            {
                Console.Error.WriteLine($"Stride {version} has no {editor.DisplayName()} Game Studio installed, only {string.Join(" and ", installed.Select(other => other.DisplayName()))}.");
                return null;
            }
            return editor;
        }

        if (installed.Count == 0)
            return DefaultEditor(GameStudioEditors.All, wine);
        if (DefaultEditor(installed, wine) is { } fallback)
            return fallback;
        Console.Error.WriteLine($"Stride {version} has only the {string.Join(" and ", installed.Select(other => other.DisplayName()))} Game Studio installed, which {WineRequired}.");
        return null;
    }

    // The Game Studio to open when none is asked for, among the installed ones: on Windows the WPF one when it is
    // there, else the Avalonia one; elsewhere the Avalonia one, else the WPF one under Wine when wine is available.
    // Null when none of them can run.
    private static GameStudioEditor? DefaultEditor(IEnumerable<GameStudioEditor> installed, bool wine)
    {
        GameStudioEditor[] preferred = OperatingSystem.IsWindows()
            ? [GameStudioEditor.Wpf, GameStudioEditor.Avalonia]
            : [GameStudioEditor.Avalonia, GameStudioEditor.Wpf];
        var candidates = installed.ToList();
        return preferred
            .Where(editor => candidates.Contains(editor) && (editor.RunsNatively() || wine))
            .Select(editor => (GameStudioEditor?)editor)
            .FirstOrDefault();
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
                : Tools.Run(manager.LocateAssetCompiler(resolved), $"the Asset Compiler for Stride {resolved}", parseResult.UnmatchedTokens, wait: true, toolReadsHostArgs: false);
        });

        return command;
    }
}
