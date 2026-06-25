// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.CommandLine;
using Stride.Launcher.Core;

var manager = new StrideVersionManager();

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

// Wire up the command tree and run.
var root = new RootCommand("Stride command-line tool.");
root.Subcommands.Add(listCommand);
root.Subcommands.Add(installCommand);
root.Subcommands.Add(uninstallCommand);
return await root.Parse(args).InvokeAsync();
