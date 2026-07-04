// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics;
using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.Commands;
using Microsoft.VisualStudio.Extensibility.Shell;
using Microsoft.VisualStudio.ProjectSystem.Query;
using NuGet.Versioning;

namespace Stride.VisualStudio.Commands;

/// <summary>
/// Opens the current solution in the version-matched Stride Game Studio.
/// </summary>
[VisualStudioContribution]
internal sealed class OpenInGameStudioCommand : Command
{
    // guidSHLMainMenu + IDG_VS_CTXT_SOLUTION_EXPLORE: the solution-node group holding Copy Full Path /
    // Open in File Explorer / Open in Terminal / Hide Unloaded Projects. Priority sorts within the group
    // (lower = higher up); this value places the command just after "Open in Terminal".
    private static readonly Guid VsMainMenu = new("d309f791-903f-11d0-9efc-00a0c911004f");
    private const int SolutionExploreGroup = 0x0265;

    public override CommandConfiguration CommandConfiguration => new("%Stride.VisualStudio.OpenInGameStudio.DisplayName%")
    {
        Icon = new(ImageMoniker.Custom("GameStudio"), IconSettings.IconAndText),
        Placements =
        [
            CommandPlacement.KnownPlacements.ExtensionsMenu,
            CommandPlacement.VsctParent(VsMainMenu, id: SolutionExploreGroup, priority: 0x0200),
        ],
    };

    public override async Task ExecuteCommandAsync(IClientContext context, CancellationToken cancellationToken)
    {
        var solutions = await this.Extensibility.Workspaces().QuerySolutionAsync(
            solution => solution.With(s => s.Path),
            cancellationToken);
        var solutionPath = solutions.FirstOrDefault()?.Path;
        if (string.IsNullOrEmpty(solutionPath))
        {
            await ShowAsync("Open a Stride solution first.", cancellationToken);
            return;
        }

        var version = await StrideVersionResolver.ResolveVersionAsync(solutionPath, cancellationToken);
        if (version is null)
        {
            await ShowAsync("This solution doesn't reference Stride, so Game Studio can't be opened for it.", cancellationToken);
            return;
        }

        var gameStudio = LocateGameStudio(version);
        if (gameStudio is null)
        {
            await ShowAsync($"Couldn't locate Stride Game Studio {version} in the NuGet package cache. Restore the solution first.", cancellationToken);
            return;
        }

        try
        {
            var startInfo = new ProcessStartInfo(gameStudio, $"\"{solutionPath}\"")
            {
                UseShellExecute = false,
                WorkingDirectory = Path.GetDirectoryName(gameStudio)!,
            };
            // The net8 host exports DOTNET_ROOT*/DOTNET_HOST_PATH; a net10 apphost (Game Studio) that inherits
            // them fails with "install .NET". Strip them so it resolves .NET from the machine install.
            foreach (var variable in new[] { "DOTNET_ROOT", "DOTNET_ROOT(x86)", "DOTNET_ROOT_X64", "DOTNET_ROOT_ARM64", "DOTNET_HOST_PATH" })
                startInfo.Environment.Remove(variable);

            Process.Start(startInfo);
        }
        catch (Exception ex)
        {
            await ShowAsync($"Couldn't start Game Studio: {ex.Message}", cancellationToken);
        }
    }

    private async Task ShowAsync(string message, CancellationToken cancellationToken)
        => await this.Extensibility.Shell().ShowPromptAsync(message, PromptOptions.OK, cancellationToken);

    // Best-effort: find the version-matched Game Studio executable in the NuGet global packages cache.
    private static string? LocateGameStudio(NuGetVersion version)
    {
        var packagesRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".nuget", "packages", "stride.gamestudio", version.ToNormalizedString());
        if (!Directory.Exists(packagesRoot))
            return null;

        return Directory.EnumerateFiles(packagesRoot, "Stride.GameStudio.exe", SearchOption.AllDirectories).FirstOrDefault();
    }
}
