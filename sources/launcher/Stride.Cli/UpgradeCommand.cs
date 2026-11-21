// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.CommandLine;
using Stride.Core;
using Stride.Core.Assets;
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
        var versionOptions = new CliVersion.Options($"Stride version to upgrade to (4.4.0 or newer), installed first when it isn't; {CliVersion.Options.LineHelp}. Defaults to the newest installed release (nightlies: project upgrades might not be fully tested).");
        var noBackup = new Option<bool>("--no-backup") { Description = "Skip the automatic backup of files the upgrade modifies before editing in place." };
        var report = new Option<string?>("--report") { Description = "Write what the upgrade did to each project, in Markdown, to this file (Stride 4.5 and newer). It doesn't change the upgrade-report.md of a project." };
        var verbose = new Option<bool>("--verbose", "-v") { Description = "Show the verbose log of the upgrade, with every step also when all of them are done." };

        var command = new Command("upgrade", "Upgrade a project to a newer installed Stride version.");
        command.Arguments.Add(path);
        versionOptions.AddTo(command);
        command.Options.Add(noBackup);
        command.Options.Add(report);
        command.Options.Add(verbose);
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

            // Target version: an exact --version, else the newest installed that a line and the options allow, as
            // install takes them. Upgrade is only supported to Stride 4.4.0+ (older asset compilers lack the 'upgrade' verb).
            PackageVersion target;
            var versionSpec = parseResult.GetValue(versionOptions.Version);
            var filter = versionOptions.Filter(parseResult);
            string? line = null;
            // A line by its first version: no version of 4.3 can be upgraded to
            if (!string.IsNullOrEmpty(versionSpec)
                && !StrideVersionManager.SupportsUpgrade(new PackageVersion(VersionRules.TryParseLine(versionSpec, out line) ? $"{line}.0" : versionSpec)))
            {
                Console.Error.WriteLine($"'stride upgrade' supports Stride 4.4.0 and newer only; {versionSpec} is older.");
                return 1;
            }
            if (!string.IsNullOrEmpty(versionSpec) && line is null)
            {
                target = new PackageVersion(versionSpec);
            }
            else if (manager.FindUpgradeTarget(versionSpec, filter) is { } newest)
            {
                target = newest;
            }
            else
            {
                Console.Error.WriteLine(string.IsNullOrEmpty(versionSpec) && filter == default
                    ? "No Stride 4.4.0 or newer is installed to upgrade to. Install one with 'stride sdk install', or pass --version or --prerelease / --nightly."
                    : VersionRules.NoInstalledMatch(versionSpec, filter));
                return 1;
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
                if (target.IsNightly && !SdkCommand.AddNightlyFeed(manager))
                    return 1;
                var progress = new InstallProgressConsole();
                try
                {
                    await manager.Install(target.ToString(), filter.Prerelease, filter.Nightly, filter.Tag, progress, cancellationToken);
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
            if (parseResult.GetValue(verbose))
                args.Add("--verbose");
            if (parseResult.GetValue(report) is { Length: > 0 } reportPath)
            {
                // An older asset compiler ignores options it doesn't know: one that writes the report uses UpgradeReportWriter
                if (!DotNetHostSelector.ReferencesType(compiler, "UpgradeReportWriter"))
                {
                    Console.Error.WriteLine($"The Asset Compiler of Stride {target} can't write an upgrade report: --report needs a newer version.");
                    return 1;
                }
                args.Add($"--report={Path.GetFullPath(reportPath)}");
            }

            Console.WriteLine($"Upgrading {Path.GetFileName(input)} to Stride {target}...");
            return Tools.Run(compiler, $"the Asset Compiler for Stride {target}", args, wait: true, sessionPath: input, toolReadsHostArgs: false);
        });

        return command;
    }
}
