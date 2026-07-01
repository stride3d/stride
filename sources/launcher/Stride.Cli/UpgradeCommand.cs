// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.CommandLine;
using Stride.Core;
using Stride.Cli.Core;

// upgrade: migrate a project's assets to a newer installed Stride version (what Game Studio does on open).
internal static class UpgradeCommand
{
    public static Command Create(StrideVersionManager manager)
    {
        var path = new Argument<string?>("path")
        {
            Arity = ArgumentArity.ZeroOrOne,
            Description = "Project or solution to upgrade. Defaults to the solution or project in the current directory.",
        };
        var version = new Option<string?>("--version") { Description = "Stride version to upgrade to (4.4.0 or newer). Defaults to the newest installed stable version." };
        var prerelease = new Option<bool>("--prerelease") { Description = "Consider prerelease (beta) versions when choosing the newest version to upgrade to." };
        var noBackup = new Option<bool>("--no-backup") { Description = "Skip the automatic backup of files the upgrade modifies before editing in place." };

        var command = new Command("upgrade", "Upgrade a project to a newer installed Stride version.");
        command.Arguments.Add(path);
        command.Options.Add(version);
        command.Options.Add(prerelease);
        command.Options.Add(noBackup);
        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var cwd = Environment.CurrentDirectory;
            var explicitInput = parseResult.GetValue(path);
            if (explicitInput is not null && !File.Exists(explicitInput))
            {
                Console.Error.WriteLine($"'{explicitInput}' does not exist.");
                return 1;
            }

            // Use an explicit path, else the solution or project in the current directory (like `dotnet build` —
            // current directory only, no walking up). The asset compiler resolves a bare .csproj to its solution.
            var input = explicitInput ?? manager.FindSolution(cwd) ?? manager.FindProject(cwd);
            if (string.IsNullOrEmpty(input))
            {
                Console.Error.WriteLine("No solution or project found in the current directory. Run it from the project folder, or pass a path.");
                return 1;
            }

            var projectDirectory = Path.GetDirectoryName(Path.GetFullPath(input))!;

            // Target version: explicit --version, else the newest installed (stable unless --prerelease). Upgrade
            // is only supported to Stride 4.4.0+ (older asset compilers lack the 'upgrade' verb).
            PackageVersion target;
            var explicitVersion = parseResult.GetValue(version);
            if (!string.IsNullOrEmpty(explicitVersion))
            {
                target = new PackageVersion(explicitVersion);
                if (!StrideVersionManager.SupportsUpgrade(target))
                {
                    Console.Error.WriteLine($"'stride upgrade' supports Stride 4.4.0 and newer only; {target} is older.");
                    return 1;
                }
            }
            else
            {
                var newest = manager.GetNewestUpgradeTarget(parseResult.GetValue(prerelease));
                if (newest is null)
                {
                    Console.Error.WriteLine("No Stride 4.4.0 or newer is installed to upgrade to. Install one with 'stride sdk install', or pass --prerelease / --version.");
                    return 1;
                }
                target = newest.Version;
            }

            // Skip when the project already targets the same-or-newer version (no re-reconcile, no downgrade).
            // Best-effort since the current version can't always be determined; applies to explicit and implicit.
            PackageVersion? current = null;
            try { current = manager.ResolveVersion(null, projectDirectory); } catch { /* current unknown; proceed */ }
            if (current is not null && target.CompareTo(current) <= 0)
            {
                Console.WriteLine($"Project is already on Stride {current}; nothing to upgrade (requested {target}).");
                return 0;
            }

            var compiler = manager.LocateAssetCompiler(target);
            if (compiler is null)
            {
                // Auto-install the target version's tooling, then locate its Asset Compiler again.
                Console.WriteLine($"Stride {target} isn't installed; installing it first...");
                var progress = new InstallProgressConsole();
                try
                {
                    await manager.Install(target.ToString(), parseResult.GetValue(prerelease), progress, cancellationToken);
                }
                catch (Exception exception)
                {
                    progress.Dispose();
                    Console.Error.WriteLine(exception.Message);
                    return 1;
                }

                progress.Dispose();
                compiler = manager.LocateAssetCompiler(target);
                if (compiler is null)
                {
                    Console.Error.WriteLine($"Installed Stride {target} but couldn't locate its Asset Compiler.");
                    return 1;
                }
            }

            // The asset compiler backs up the files it modifies before editing in place (default on); forward the opt-out.
            var args = new List<string> { "upgrade", input };
            if (parseResult.GetValue(noBackup))
                args.Add("--no-backup");

            Console.WriteLine($"Upgrading {Path.GetFileName(input)} to Stride {target}...");
            return Tools.Run(compiler, $"the Asset Compiler for Stride {target}", args, wait: true);
        });

        return command;
    }
}
