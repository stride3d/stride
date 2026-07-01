// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.Commands;
using Microsoft.VisualStudio.Extensibility.Documents;
using Microsoft.VisualStudio.Extensibility.Shell;
using Microsoft.VisualStudio.ProjectSystem.Query;
using Microsoft.VisualStudio.RpcContracts.Notifications;
using NuGet.Versioning;

namespace Stride.VisualStudio.Commands;

/// <summary>
/// Upgrades the solution to a newer Stride version chosen by the user, delegating to the bundled
/// <c>stride</c> CLI (which installs the target if needed and runs the version-matched asset compiler).
/// </summary>
[VisualStudioContribution]
internal sealed class UpdateStrideVersionCommand : Command
{
    // In-place upgrade is only supported from Stride 4.4.0 onwards (older asset compilers lack the verb).
    private static readonly Version UpgradeFloor = new(4, 4);

    // Same solution-node group as Open in Game Studio; a slightly higher priority sits just below it.
    private static readonly Guid VsMainMenu = new("d309f791-903f-11d0-9efc-00a0c911004f");
    private const int SolutionExploreGroup = 0x0265;

    public override CommandConfiguration CommandConfiguration => new("%Stride.VisualStudio.UpdateStrideVersion.DisplayName%")
    {
        Icon = new(ImageMoniker.Custom("GameStudio"), IconSettings.IconAndText),
        Placements =
        [
            CommandPlacement.KnownPlacements.ExtensionsMenu,
            CommandPlacement.VsctParent(VsMainMenu, id: SolutionExploreGroup, priority: 0x0210),
        ],
    };

    public override async Task ExecuteCommandAsync(IClientContext context, CancellationToken cancellationToken)
    {
        var shell = this.Extensibility.Shell();

        var solutions = await this.Extensibility.Workspaces().QuerySolutionAsync(
            solution => solution.With(s => s.Path),
            cancellationToken);
        var solutionPath = solutions.FirstOrDefault()?.Path;
        if (string.IsNullOrEmpty(solutionPath))
        {
            await shell.ShowPromptAsync("Open a Stride solution first.", PromptOptions.OK, cancellationToken);
            return;
        }

        var workingDirectory = Path.GetDirectoryName(solutionPath)!;

        var current = await StrideVersionResolver.ResolveVersionAsync(solutionPath, cancellationToken);
        if (current is null)
        {
            await shell.ShowPromptAsync("This solution doesn't reference Stride.", PromptOptions.OK, cancellationToken);
            return;
        }

        // The whole flow lives in one dialog: it opens with a loading indicator, fetches the available versions
        // concurrently and populates itself, then runs the upgrade (streaming its log into the dialog) when the
        // user clicks Update. The upgrade log is also mirrored to the Stride output window for later inspection.
        var channel = await GetOutputChannelAsync(cancellationToken);
        var data = new VersionPickerData($"Update Stride packages from {current} to:");
        data.SetUpdateAction((version, appendLine, token) => RunUpgradeAsync(version, solutionPath!, workingDirectory, channel, appendLine, token));

        using var control = new VersionPickerControl(data);
        var populateTask = PopulateVersionsAsync(data, workingDirectory, current, cancellationToken);
        await shell.ShowDialogAsync(control, "Update Stride", DialogOption.Close, cancellationToken);
        await populateTask;
    }

    // A single reused output channel: CreateOutputChannelAsync throws if the same display name is created twice.
    private static OutputChannel? outputChannel;

    private async Task<OutputChannel> GetOutputChannelAsync(CancellationToken cancellationToken)
        => outputChannel ??= await this.Extensibility.Views().Output.CreateOutputChannelAsync("Stride", cancellationToken);

    // Runs 'stride upgrade' for the chosen version, forwarding each log line to both the dialog and the Stride
    // output window. 'upgrade' installs the target if missing, then runs the version-matched asset compiler.
    private static async Task<bool> RunUpgradeAsync(
        string version, string solutionPath, string workingDirectory, OutputChannel channel, Func<string, Task> appendLine, CancellationToken cancellationToken)
    {
        async Task ForwardAsync(string line)
        {
            await channel.WriteLineAsync(line);
            await appendLine(line);
        }

        await channel.WriteLineAsync($"Updating {Path.GetFileName(solutionPath)} to Stride {version}…");
        var upgrade = await StrideCli.RunAsync(
            workingDirectory,
            ["upgrade", solutionPath, "--version", version],
            cancellationToken,
            ForwardAsync);

        if (upgrade.RuntimeMissing)
        {
            await appendLine("This needs the .NET 10 runtime, which wasn't found. Install it from https://dotnet.microsoft.com/download and try again.");
            return false;
        }
        if (!upgrade.Launched)
        {
            await appendLine("Couldn't find the bundled Stride CLI in the extension. Try reinstalling the extension.");
            return false;
        }

        return upgrade.Succeeded;
    }

    private static IEnumerable<NuGetVersion> ParseVersions(string output)
    {
        foreach (var line in output.Split('\n'))
            if (NuGetVersion.TryParse(line.Trim(), out var version))
                yield return version;
    }

    private static bool IsUpgradeTarget(NuGetVersion version)
        => new Version(version.Version.Major, version.Version.Minor) >= UpgradeFloor;

    // Groups the candidates into major.minor lines (newest line first, newest version first within each).
    private static IReadOnlyList<VersionLine> BuildLines(IEnumerable<NuGetVersion> candidates)
        => candidates
            .GroupBy(version => (version.Version.Major, version.Version.Minor))
            .OrderByDescending(line => line.Key)
            .Select(line => new VersionLine(
                $"{line.Key.Major}.{line.Key.Minor}",
                line.OrderByDescending(version => version).Select(version => version.ToNormalizedString()).ToList()))
            .ToList();

    private static string FirstNonEmpty(string first, string second)
        => string.IsNullOrWhiteSpace(first) ? second.Trim() : first.Trim();

    // Fetches the versions offered by the package sources and fills the picker, or leaves it with a message
    // when the fetch fails or offers nothing newer. Runs while the dialog is already showing its loading state.
    private static async Task PopulateVersionsAsync(VersionPickerData data, string workingDirectory, NuGetVersion current, CancellationToken cancellationToken)
    {
        var available = await StrideCli.RunAsync(workingDirectory, ["sdk", "available", "--prerelease"], cancellationToken);
        if (available.RuntimeMissing)
        {
            data.Fail("This needs the .NET 10 runtime, which wasn't found. Install it from https://dotnet.microsoft.com/download and try again.");
            return;
        }
        if (!available.Launched)
        {
            data.Fail("Couldn't find the bundled Stride CLI in the extension. Try reinstalling the extension.");
            return;
        }
        if (!available.Succeeded)
        {
            data.Fail($"Couldn't list available Stride versions:\n{FirstNonEmpty(available.StandardError, available.StandardOutput)}");
            return;
        }

        var candidates = ParseVersions(available.StandardOutput)
            .Where(version => version > current && IsUpgradeTarget(version))
            .OrderByDescending(version => version)
            .ToList();

        var allLines = BuildLines(candidates);
        var stableLines = BuildLines(candidates.Where(version => !version.IsPrerelease));
        if (allLines.Count == 0)
        {
            data.Fail($"No newer Stride version (4.4+) is available from your package sources (current is {current}).");
            return;
        }

        data.Populate(stableLines, allLines);
    }
}
