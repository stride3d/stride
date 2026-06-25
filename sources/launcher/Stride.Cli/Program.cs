// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.CommandLine;
using Stride.Launcher.Core;

var listCommand = new Command("list", "List installed Stride versions.");
listCommand.SetAction(_ =>
{
    var versions = new StrideVersionManager().GetInstalled();
    if (versions.Count == 0)
    {
        Console.WriteLine("No Stride versions installed.");
        return;
    }

    foreach (var version in versions)
        Console.WriteLine($"{version.Version}  ({version.PackageId})");
});

var root = new RootCommand("Stride command-line tool.");
root.Subcommands.Add(listCommand);
return root.Parse(args).Invoke();
