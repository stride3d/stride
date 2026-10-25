// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.CommandLine;
using Stride;
using Stride.Cli.Core;
using Stride.Core;
using Stride.Core.Packages;

// sdk: manage installed Stride versions (the engine, editor, and tools).
internal static class SdkCommand
{
    public static Command Create(StrideVersionManager manager)
    {
        var prerelease = new Option<bool>("--prerelease") { Description = "Include prerelease (beta) versions when resolving the newest version or line." };
        var nightly = new Option<bool>("--nightly") { Description = $"Include the nightly builds (X.Y.Z-nightly.YYYYMMDD.N, project upgrades might not be fully tested). Install and update add their feed ({NugetStore.NightlyFeedUrl}) to your NuGet.config." };

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
                Console.WriteLine($"{version.Version}  ({string.Join(", ", version.Editors.Select(editor => editor.DisplayName()))})");
        });

        // available: print the Stride versions offered by the package sources (one per line, newest first).
        var availableCommand = new Command("available", "List Stride versions available from the package sources.");
        availableCommand.Options.Add(prerelease);
        availableCommand.Options.Add(nightly);
        availableCommand.SetAction(async (parseResult, cancellationToken) =>
        {
            // Listing only: the NuGet.config is left as is
            if (parseResult.GetValue(nightly))
                manager.AddNightlyFeed(save: false);
            var versions = await manager.GetAvailable(parseResult.GetValue(prerelease), parseResult.GetValue(nightly), cancellationToken);
            if (versions.Count == 0)
            {
                Console.Error.WriteLine("No Stride version is available from the package sources.");
                return 1;
            }

            foreach (var version in versions)
                Console.WriteLine(version);
            return 0;
        });

        // install: add a Stride version (newest, a major.minor line, or an exact version).
        var installVersion = new Argument<string?>("version")
        {
            Arity = ArgumentArity.ZeroOrOne,
            Description = "Version or major.minor line to install. If omitted, installs the version the current directory's project needs, else the newest stable version.",
        };
        var installCommand = new Command("install", "Install a Stride version (additive; never removes an existing one).");
        installCommand.Arguments.Add(installVersion);
        installCommand.Options.Add(prerelease);
        installCommand.Options.Add(nightly);
        installCommand.SetAction(async (parseResult, cancellationToken) =>
        {
            var spec = parseResult.GetValue(installVersion);
            // Before the project's restore below, which might need it
            if (parseResult.GetValue(nightly) && !AddNightlyFeed(manager))
                return 1;
            // No version given but inside a project: install the version that project needs (sets you up to work
            // on it), rather than the newest available. The restore that resolves it also pulls its engine packages.
            if (spec is null && manager.FindProjectVersion(Environment.CurrentDirectory) is { } projectVersion)
            {
                spec = projectVersion.ToString();
                Console.WriteLine($"Installing Stride {spec} required by the project in this directory.");
            }
            // A nightly asked for by its version needs the feed too
            if (spec is not null && PackageVersion.TryParse(spec, out var specVersion) && specVersion.IsNightly && !AddNightlyFeed(manager))
                return 1;

            var progress = new InstallProgressConsole();
            try
            {
                var installed = await manager.Install(spec, parseResult.GetValue(prerelease), parseResult.GetValue(nightly), progress, cancellationToken);
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
        updateCommand.Options.Add(prerelease);
        updateCommand.Options.Add(nightly);
        updateCommand.SetAction(async (parseResult, cancellationToken) =>
        {
            if (parseResult.GetValue(nightly) && !AddNightlyFeed(manager))
                return 1;
            var progress = new InstallProgressConsole();
            try
            {
                var updated = await manager.Update(parseResult.GetValue(updateLine), parseResult.GetValue(prerelease), parseResult.GetValue(nightly), progress, cancellationToken);
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

        var sdk = new Command("sdk", "Manage installed Stride versions (list, available, install, uninstall, update).");
        sdk.Subcommands.Add(listCommand);
        sdk.Subcommands.Add(availableCommand);
        sdk.Subcommands.Add(installCommand);
        sdk.Subcommands.Add(uninstallCommand);
        sdk.Subcommands.Add(updateCommand);
        return sdk;
    }

    // Adds the nightly feed to the user's NuGet.config when it isn't there yet, as the launcher's "Show nightly builds"
    // does, and says so. False, with the error printed, when the NuGet.config can't be written.
    internal static bool AddNightlyFeed(StrideVersionManager manager)
    {
        try
        {
            if (manager.AddNightlyFeed(save: true))
                Console.WriteLine($"Added the nightly feed ({NugetStore.NightlyFeedUrl}) to your NuGet.config.");
            return true;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"Could not add the nightly feed ({NugetStore.NightlyFeedUrl}) to your NuGet.config: {exception.Message}");
            return false;
        }
    }
}
