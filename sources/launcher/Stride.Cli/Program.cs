// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.CommandLine;
using System.CommandLine.Help;
using System.Diagnostics;
using System.Reflection;
using Stride.Cli.Core;

var manager = new StrideVersionManager();

// Version resolution restores the project first (so a stale project.assets.json can't report an out-of-date
// version); surface a spinner if a restore is slow.
var restoreIndicator = new RestoreIndicator();
manager.RestoreStarting += restoreIndicator.Start;
manager.RestoreFinished += restoreIndicator.Stop;

// self update: update the Stride CLI itself by delegating to dotnet, then exit so the running executable is
// unlocked while dotnet replaces it.
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

    var resolved = CliVersion.ResolveOrReport(manager, null);
    if (resolved is not null)
        Console.WriteLine($"Resolved Stride version: {resolved}");
});

// Wire up the command tree and run. SDK management is grouped under 'sdk'; project actions stay flat.
var root = new RootCommand("Stride command-line tool.");
root.Subcommands.Add(SdkCommand.Create(manager));
root.Subcommands.Add(NewCommand.Create(manager));
root.Subcommands.Add(UpgradeCommand.Create(manager));
root.Subcommands.Add(ToolCommands.CreateStudio(manager));
root.Subcommands.Add(ToolCommands.CreateAsset(manager));
var legacyCommands = new List<Command> { Stride.Cli.Legacy.LegacyCommands.CreateGenerateLegacyShaderCode(manager) };
foreach (var legacyCommand in legacyCommands)
    root.Subcommands.Add(legacyCommand);
root.Subcommands.Add(Stride.Cli.Legacy.LegacyCommands.CreateLister(legacyCommands));
root.Subcommands.Add(selfCommand);
root.Subcommands.Add(versionCommand);

// Let 'stride <hidden-command> --help' render help instead of nothing (the default writer skips Hidden commands).
foreach (var option in root.Options)
    if (option is HelpOption helpOption)
        helpOption.Action = new RevealHiddenHelpAction();

return await root.Parse(args).InvokeAsync();
