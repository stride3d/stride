// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.CommandLine;
using System.Diagnostics;
using System.Reflection;
using Stride.Core;
using Stride.Launcher.Core;

var manager = new StrideVersionManager();

// Version resolution restores the project first (so a stale project.assets.json can't report an out-of-date
// version); surface a spinner if a restore is slow.
var restoreIndicator = new RestoreIndicator();
manager.RestoreStarting += restoreIndicator.Start;
manager.RestoreFinished += restoreIndicator.Stop;

// Resolve the version for the current directory (restoring the project first). Prints any error and
// returns null on failure.
PackageVersion? ResolveOrReport(string? explicitVersion)
{
    try
    {
        var resolved = manager.ResolveVersion(explicitVersion, Environment.CurrentDirectory);
        if (resolved is null)
            Console.Error.WriteLine("No Stride version is installed.");
        return resolved;
    }
    catch (Exception exception)
    {
        Console.Error.WriteLine(exception.Message);
        return null;
    }
}

// list: print the installed Stride versions.
var listCommand = new Command("list", "List installed Stride versions.");
listCommand.SetAction(_ =>
{
    var versions = manager.GetInstalled();
    if (versions.Count == 0)
    {
        Console.WriteLine("No Stride versions installed.");
        return;
    }

    foreach (var version in versions)
        Console.WriteLine($"{version.Version}  ({version.PackageId})");
});

// install: add a Stride version (newest, a major.minor line, or an exact version).
var installVersion = new Argument<string?>("version")
{
    Arity = ArgumentArity.ZeroOrOne,
    Description = "Version or major.minor line to install. Installs the newest stable version if omitted.",
};
var prereleaseOption = new Option<bool>("--prerelease") { Description = "Include prerelease (beta) versions when resolving the newest version or line." };
var installCommand = new Command("install", "Install a Stride version (additive; never removes an existing one).");
installCommand.Arguments.Add(installVersion);
installCommand.Options.Add(prereleaseOption);
installCommand.SetAction(async (parseResult, cancellationToken) =>
{
    var spec = parseResult.GetValue(installVersion);
    // No version given but inside a project: install the version that project needs (sets you up to work on it),
    // rather than the newest available. The restore that resolves it also pulls the project's engine packages.
    if (spec is null && manager.FindProjectVersion(Environment.CurrentDirectory) is { } projectVersion)
    {
        spec = projectVersion.ToString();
        Console.WriteLine($"Installing Stride {spec} required by the project in this directory.");
    }

    var progress = new InstallProgressConsole();
    try
    {
        var installed = await manager.Install(spec, parseResult.GetValue(prereleaseOption), progress, cancellationToken);
        progress.Dispose();
        Console.WriteLine($"Installed Stride {installed.Version}.");
        return 0;
    }
    catch (Exception exception)
    {
        progress.Dispose();
        Console.Error.WriteLine(exception.Message);
        return 1;
    }
});

// uninstall: remove a Stride version.
var uninstallVersion = new Argument<string>("version") { Description = "Version to uninstall." };
var uninstallCommand = new Command("uninstall", "Uninstall a Stride version.");
uninstallCommand.Arguments.Add(uninstallVersion);
uninstallCommand.SetAction(async (parseResult, _) =>
{
    var spec = parseResult.GetValue(uninstallVersion)!;
    var progress = new InstallProgressConsole();
    try
    {
        var removed = await manager.Uninstall(spec, progress);
        progress.Dispose();
        if (removed)
        {
            Console.WriteLine($"Uninstalled Stride {spec}.");
            return 0;
        }

        Console.Error.WriteLine($"Stride {spec} is not installed.");
        return 1;
    }
    catch (Exception exception)
    {
        progress.Dispose();
        Console.Error.WriteLine(exception.Message);
        return 1;
    }
});

// update: roll installed lines to their newest patch, retiring the previously managed patch.
var updateLine = new Argument<string?>("line")
{
    Arity = ArgumentArity.ZeroOrOne,
    Description = "Major.minor line to update (e.g. 4.4). Updates all installed lines if omitted.",
};
var updateCommand = new Command("update", "Update installed Stride lines to their newest patch.");
updateCommand.Arguments.Add(updateLine);
updateCommand.Options.Add(prereleaseOption);
updateCommand.SetAction(async (parseResult, cancellationToken) =>
{
    var progress = new InstallProgressConsole();
    try
    {
        var updated = await manager.Update(parseResult.GetValue(updateLine), parseResult.GetValue(prereleaseOption), progress, cancellationToken);
        progress.Dispose();
        if (updated.Count == 0)
        {
            Console.WriteLine("Everything is already up to date.");
            return 0;
        }

        foreach (var version in updated)
            Console.WriteLine($"Updated to Stride {version.Version}.");
        return 0;
    }
    catch (Exception exception)
    {
        progress.Dispose();
        Console.Error.WriteLine(exception.Message);
        return 1;
    }
});

// --version overrides the version that would otherwise come from a project in the current directory.
var versionOption = new Option<string?>("--version") { Description = "Use a specific Stride version, bypassing a project in the current directory." };

// upgrade: migrate a project's assets to a newer installed Stride version (what Game Studio does on open).
var upgradePath = new Argument<string?>("path")
{
    Arity = ArgumentArity.ZeroOrOne,
    Description = "Project or solution to upgrade. Defaults to the solution or project in the current directory.",
};
var noBackupOption = new Option<bool>("--no-backup") { Description = "Skip the automatic backup of modified files the upgrade makes before editing in place." };
var upgradeCommand = new Command("upgrade", "Upgrade a project to a newer installed Stride version.");
upgradeCommand.Arguments.Add(upgradePath);
upgradeCommand.Options.Add(versionOption);
upgradeCommand.Options.Add(prereleaseOption);
upgradeCommand.Options.Add(noBackupOption);
upgradeCommand.SetAction(parseResult =>
{
    var cwd = Environment.CurrentDirectory;
    var explicitInput = parseResult.GetValue(upgradePath);
    if (explicitInput is not null && !File.Exists(explicitInput))
    {
        Console.Error.WriteLine($"'{explicitInput}' does not exist.");
        return 1;
    }

    // Use an explicit path, else the solution or project in the current directory (like `dotnet build` — current
    // directory only, no walking up). The asset compiler resolves a bare .csproj to its containing solution.
    var input = explicitInput ?? manager.FindSolution(cwd) ?? manager.FindProject(cwd);
    if (string.IsNullOrEmpty(input))
    {
        Console.Error.WriteLine("No solution or project found in the current directory. Run it from the project folder, or pass a path.");
        return 1;
    }

    var projectDirectory = Path.GetDirectoryName(Path.GetFullPath(input))!;

    // Target version: explicit --version, else the newest installed (stable unless --prerelease). Upgrade is
    // only supported to Stride 4.4.0+ (older asset compilers lack the 'upgrade' verb).
    PackageVersion target;
    var explicitVersion = parseResult.GetValue(versionOption);
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
        var newest = manager.GetNewestUpgradeTarget(parseResult.GetValue(prereleaseOption));
        if (newest is null)
        {
            Console.Error.WriteLine("No Stride 4.4.0 or newer is installed to upgrade to. Install one with 'stride sdk install', or pass --prerelease / --version.");
            return 1;
        }
        target = newest.Version;
    }

    // Skip when the project already targets the same-or-newer version (no re-reconcile, no downgrade). Best-effort
    // since the current version can't always be determined; applies to both implicit and explicit targets.
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
        Console.Error.WriteLine($"The Asset Compiler for Stride {target} is not installed. Run 'stride sdk install {target}' first.");
        return 1;
    }

    // The asset compiler backs up the files it modifies before editing in place (default on); forward the opt-out.
    var args = new List<string> { "upgrade", input };
    if (parseResult.GetValue(noBackupOption))
        args.Add("--no-backup");

    Console.WriteLine($"Upgrading {Path.GetFileName(input)} to Stride {target}...");
    return Tools.Run(compiler, $"the Asset Compiler for Stride {target}", args, wait: true);
});

// asset: advanced escape hatch — forward a verb and arguments straight to the Asset Compiler. Hidden because
// 'stride upgrade' covers the common case and build/pack/generate-code normally run via 'dotnet build'/'pack'.
var assetCommand = new Command("asset", "Run the Stride Asset Compiler directly (advanced; arguments are forwarded).") { Hidden = true };
assetCommand.Options.Add(versionOption);
assetCommand.TreatUnmatchedTokensAsErrors = false;
assetCommand.SetAction(parseResult =>
{
    var version = ResolveOrReport(parseResult.GetValue(versionOption));
    return version is null
        ? 1
        : Tools.Run(manager.LocateAssetCompiler(version), $"the Asset Compiler for Stride {version}", parseResult.UnmatchedTokens, wait: true);
});

// studio: open Game Studio (launches and returns immediately).
var studioPath = new Argument<string?>("path")
{
    Arity = ArgumentArity.ZeroOrOne,
    Description = "Solution to open. Defaults to the solution in the current directory.",
};
var studioCommand = new Command("studio", "Open Game Studio.");
studioCommand.Arguments.Add(studioPath);
studioCommand.Options.Add(versionOption);
studioCommand.TreatUnmatchedTokensAsErrors = false;
studioCommand.SetAction(parseResult =>
{
    var version = ResolveOrReport(parseResult.GetValue(versionOption));
    if (version is null)
        return 1;

    // Open the solution in the current directory by default. Game Studio expects a .sln (opening a bare .csproj
    // crashes it), so only a solution is auto-selected — never a project.
    var args = new List<string>();
    var solution = parseResult.GetValue(studioPath) ?? manager.FindSolution(Environment.CurrentDirectory);
    if (solution is not null)
        args.Add(solution);
    args.AddRange(parseResult.UnmatchedTokens);

    return Tools.Run(manager.LocateGameStudio(version), $"Game Studio for Stride {version}", args, wait: false);
});

// self update: update the Stride CLI itself by delegating to dotnet, then exit so the running
// executable is unlocked while dotnet replaces it.
var selfCommand = new Command("self", "Manage the Stride CLI itself.");
var selfUpdateCommand = new Command("update", "Update the Stride CLI to its newest version.");
selfUpdateCommand.SetAction(_ =>
{
    Console.WriteLine("Updating the Stride CLI in the background via 'dotnet tool update -g Stride.Cli'...");
    var startInfo = new ProcessStartInfo("dotnet") { UseShellExecute = false };
    foreach (var argument in new[] { "tool", "update", "-g", "Stride.Cli" })
        startInfo.ArgumentList.Add(argument);
    return Process.Start(startInfo) is null ? 1 : 0;
});
selfCommand.Subcommands.Add(selfUpdateCommand);

// version: show the CLI version and the Stride version resolved for the current directory.
var versionCommand = new Command("version", "Show the Stride CLI version and the resolved Stride version.");
versionCommand.SetAction(_ =>
{
    var cliVersion = Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "unknown";
    Console.WriteLine($"Stride CLI {cliVersion}");

    var resolved = ResolveOrReport(null);
    if (resolved is not null)
        Console.WriteLine($"Resolved Stride version: {resolved}");
});

// sdk: manage installed Stride versions (engine, editor, tools).
var sdkCommand = new Command("sdk", "Manage installed Stride versions (list, install, uninstall, update).");
sdkCommand.Subcommands.Add(listCommand);
sdkCommand.Subcommands.Add(installCommand);
sdkCommand.Subcommands.Add(uninstallCommand);
sdkCommand.Subcommands.Add(updateCommand);

// Wire up the command tree and run. SDK management is grouped under 'sdk'; project actions stay flat.
var root = new RootCommand("Stride command-line tool.");
root.Subcommands.Add(sdkCommand);
root.Subcommands.Add(NewCommand.Create(manager));
root.Subcommands.Add(upgradeCommand);
root.Subcommands.Add(studioCommand);
root.Subcommands.Add(assetCommand);
root.Subcommands.Add(selfCommand);
root.Subcommands.Add(versionCommand);
return await root.Parse(args).InvokeAsync();
