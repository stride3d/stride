// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.CommandLine;
using System.Diagnostics;
using System.Reflection;
using Stride.Core;
using Stride.Launcher.Core;

var manager = new StrideVersionManager();

// Resolve the version for the current directory (restoring the project if needed). Prints any error and
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
    Description = "Version or major.minor line to install. Installs the newest available if omitted.",
};
var installCommand = new Command("install", "Install a Stride version (additive; never removes an existing one).");
installCommand.Arguments.Add(installVersion);
installCommand.SetAction(async (parseResult, cancellationToken) =>
{
    var spec = parseResult.GetValue(installVersion);
    try
    {
        Console.WriteLine(spec is null ? "Installing the latest Stride version..." : $"Installing Stride {spec}...");
        var installed = await manager.Install(spec, cancellationToken);
        Console.WriteLine($"Installed Stride {installed.Version}.");
        return 0;
    }
    catch (Exception exception)
    {
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
    try
    {
        if (await manager.Uninstall(spec))
        {
            Console.WriteLine($"Uninstalled Stride {spec}.");
            return 0;
        }

        Console.Error.WriteLine($"Stride {spec} is not installed.");
        return 1;
    }
    catch (Exception exception)
    {
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
updateCommand.SetAction(async (parseResult, cancellationToken) =>
{
    try
    {
        var updated = await manager.Update(parseResult.GetValue(updateLine), cancellationToken);
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
        Console.Error.WriteLine(exception.Message);
        return 1;
    }
});

// --version overrides the version that would otherwise come from a project in the current directory.
var versionOption = new Option<string?>("--version") { Description = "Use a specific Stride version, bypassing a project in the current directory." };

// asset: forward the verb and its arguments to the Stride Asset Compiler (waits for it).
var assetCommand = new Command("asset", "Run the Stride Asset Compiler. Arguments are forwarded to it.");
assetCommand.Options.Add(versionOption);
assetCommand.TreatUnmatchedTokensAsErrors = false;
assetCommand.SetAction(parseResult =>
{
    var version = ResolveOrReport(parseResult.GetValue(versionOption));
    return version is null
        ? 1
        : RunTool(manager.LocateAssetCompiler(version), $"the Asset Compiler for Stride {version}", parseResult.UnmatchedTokens, wait: true);
});

// studio: open Game Studio (launches and returns immediately).
var studioCommand = new Command("studio", "Open Game Studio.");
studioCommand.Options.Add(versionOption);
studioCommand.TreatUnmatchedTokensAsErrors = false;
studioCommand.SetAction(parseResult =>
{
    var version = ResolveOrReport(parseResult.GetValue(versionOption));
    return version is null
        ? 1
        : RunTool(manager.LocateGameStudio(version), $"Game Studio for Stride {version}", parseResult.UnmatchedTokens, wait: false);
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

// Wire up the command tree and run.
var root = new RootCommand("Stride command-line tool.");
root.Subcommands.Add(listCommand);
root.Subcommands.Add(installCommand);
root.Subcommands.Add(uninstallCommand);
root.Subcommands.Add(updateCommand);
root.Subcommands.Add(assetCommand);
root.Subcommands.Add(studioCommand);
root.Subcommands.Add(selfCommand);
root.Subcommands.Add(versionCommand);
return await root.Parse(args).InvokeAsync();

// Run a located tool, forwarding arguments. When wait is true (Asset Compiler) the console is inherited
// and the tool's exit code returned; otherwise (Game Studio) it is launched detached.
static int RunTool(string? executable, string description, IReadOnlyList<string> forwardedArgs, bool wait)
{
    if (executable is null)
    {
        Console.Error.WriteLine($"Could not find {description}.");
        return 1;
    }

    var startInfo = new ProcessStartInfo(executable) { UseShellExecute = false };
    foreach (var argument in forwardedArgs)
        startInfo.ArgumentList.Add(argument);

    var process = Process.Start(startInfo);
    if (process is null)
    {
        Console.Error.WriteLine($"Failed to start {executable}.");
        return 1;
    }

    if (!wait)
        return 0;

    process.WaitForExit();
    return process.ExitCode;
}
