// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.Commands;
using Microsoft.VisualStudio.Extensibility.Shell;
using Microsoft.VisualStudio.ProjectSystem.Query;
using Microsoft.VisualStudio.RpcContracts.Notifications;

namespace Stride.VisualStudio.Commands;

/// <summary>
/// Regenerates the C# produced from .sdsl/.sdfx shaders for the right-clicked solution, project, or shader file,
/// delegating to the bundled <c>stride</c> CLI. This is the modern replacement for the old in-process Custom Tool:
/// Stride 4.0-4.3 generated the code via Stride.VisualStudio.Commands, whereas 4.4+ generates it at build.
/// </summary>
[VisualStudioContribution]
internal sealed class RegenerateShadersCommand : Command
{
    // guidSHLMainMenu: the Solution Explorer context menus. Each node type has an "…EXPLORE" group (the one with
    // Copy Full Path / Open in Terminal); placing here mirrors the working Open in Game Studio / Update commands.
    private static readonly Guid VsMainMenu = new("d309f791-903f-11d0-9efc-00a0c911004f");
    private const int SolutionExploreGroup = 0x0265; // IDG_VS_CTXT_SOLUTION_EXPLORE
    private const int ProjectExploreGroup = 0x0266;  // IDG_VS_CTXT_PROJECT_EXPLORE
    private const int ItemExploreGroup = 0x02E6;     // IDG_VS_CTXT_ITEM_EXPLORE (.sdsl/.sdfx and other files)

    public override CommandConfiguration CommandConfiguration => new("%Stride.VisualStudio.RegenerateShaders.DisplayName%")
    {
        Icon = new(ImageMoniker.Custom("GameStudio"), IconSettings.IconAndText),
        Placements =
        [
            CommandPlacement.KnownPlacements.ExtensionsMenu,
            CommandPlacement.VsctParent(VsMainMenu, id: SolutionExploreGroup, priority: 0x0220),
            CommandPlacement.VsctParent(VsMainMenu, id: ProjectExploreGroup, priority: 0x0220),
            CommandPlacement.VsctParent(VsMainMenu, id: ItemExploreGroup, priority: 0x0220),
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

        // Regenerate what was right-clicked (solution / project / .sdsl / .sdfx); fall back to the whole solution.
        var target = await GetSelectedFileAsync(context, cancellationToken) ?? solutionPath!;

        // Resolve the version from the solution (reliable even when a shader deep in the tree is selected) and pass
        // it explicitly, so the CLI doesn't have to infer it from the target's folder. The CLI itself reports the
        // pre-4.0 / 4.4+ cases; we just stream its output.
        var version = await StrideVersionResolver.ResolveVersionAsync(solutionPath, cancellationToken);
        if (version is null)
        {
            await shell.ShowPromptAsync("This solution doesn't reference Stride.", PromptOptions.OK, cancellationToken);
            return;
        }

        var workingDirectory = Path.GetDirectoryName(solutionPath)!;
        var data = new ShaderRegenData($"Regenerating shader code for {Path.GetFileName(target)} with Stride {version.ToNormalizedString()}…");

        // Start regeneration immediately and stream its output into the dialog while it's shown; the dialog stays
        // open (Close button) so the user can read the log and the final result.
        using var control = new ShaderRegenControl(data);
        var runTask = RunAsync(data, workingDirectory, target, version.ToNormalizedString(), cancellationToken);
        await shell.ShowDialogAsync(control, "Regenerate Stride Shaders", DialogOption.Close, cancellationToken);
        await runTask;
    }

    private static async Task RunAsync(ShaderRegenData data, string workingDirectory, string target, string version, CancellationToken cancellationToken)
    {
        StrideCli.Result result;
        try
        {
            result = await StrideCli.RunAsync(
                workingDirectory,
                ["generate-legacy-shader-code", target, "--version", version],
                cancellationToken,
                line => { data.Append(line); return Task.CompletedTask; });
        }
        catch (Exception exception)
        {
            data.Append(exception.Message);
            data.Complete("Shader code regeneration failed.");
            return;
        }

        if (result.RuntimeMissing)
        {
            data.Complete("This needs the .NET 10 runtime, which wasn't found. Install it from https://dotnet.microsoft.com/download and try again.");
            return;
        }
        if (!result.Launched)
        {
            data.Complete("Couldn't find the bundled Stride CLI in the extension. Try reinstalling the extension.");
            return;
        }

        data.Complete(result.Succeeded ? "Done." : "Shader code regeneration failed — see the log above.");
    }

    // The right-clicked item's local path when it's a solution/project/shader file, else null (e.g. an unrelated
    // file or a virtual node) so the caller falls back to the whole solution.
    private static async Task<string?> GetSelectedFileAsync(IClientContext context, CancellationToken cancellationToken)
    {
        Uri selection;
        try
        {
            selection = await context.GetSelectedPathAsync(cancellationToken);
        }
        catch
        {
            return null;
        }

        if (selection is null || !selection.IsFile)
            return null;

        var path = selection.LocalPath;
        var extension = Path.GetExtension(path);
        var supported = extension.Equals(".sln", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".slnx", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".csproj", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".sdsl", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".sdfx", StringComparison.OrdinalIgnoreCase);
        return supported && File.Exists(path) ? path : null;
    }
}
