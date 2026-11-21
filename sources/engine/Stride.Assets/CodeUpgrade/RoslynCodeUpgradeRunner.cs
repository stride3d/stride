// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.MSBuild;
using Stride.Core.Assets;
using Stride.Core.Diagnostics;
using Stride.Core.IO;

namespace Stride.Assets;

/// <summary>
/// Roslyn implementation of <see cref="ICodeUpgradeRunner"/>: opens the solution (already restored at
/// the from-version closure), folds the declared <see cref="CodeStep"/>s over it, and writes the
/// changed source files back. Registered into <see cref="CodeUpgradeRunner.Instance"/> at module init.
/// </summary>
internal sealed class RoslynCodeUpgradeRunner : ICodeUpgradeRunner
{
    public bool HasSourceMigrations(IReadOnlyList<PendingCodeUpgrade> pending) => GetPlans(pending).Count > 0;

    public bool Run(UFile? solutionPath, IReadOnlyList<PendingCodeUpgrade> pending, UpgradeBackup? backup, ILogger log)
    {
        // If nothing applies, never open the (expensive) workspace — this keeps project-only upgrades at exactly today's cost.
        var plans = GetPlans(pending);
        if (plans.Count == 0)
        {
            log.Info($"Code upgrade: {pending.Count} pending project(s), no applicable source migrations.");
            return true;
        }

        log.Info($"Code upgrade: applying source migrations to {plans.Count} project(s)...");

        try
        {
            // Escape any ambient synchronization context; MSBuildWorkspace is async-heavy.
            return Task.Run(() => RunAsync(solutionPath, plans, backup, log)).GetAwaiter().GetResult();
        }
        catch (NotImplementedException)
        {
            // Unsupported upgrade output (e.g. project-level changes) is an upgrader-authoring error —
            // surface it loudly instead of degrading to a warning.
            throw;
        }
        catch (Exception ex)
        {
            log.Error("Source-code migration could not run (code left unchanged).", ex);
            return false;
        }
    }

    // Each pending upgrader's code steps that apply to the version its project is upgraded from
    private static List<(PendingCodeUpgrade Pending, List<CodeStep> Steps)> GetPlans(IReadOnlyList<PendingCodeUpgrade> pending)
    {
        var plans = new List<(PendingCodeUpgrade Pending, List<CodeStep> Steps)>();
        foreach (var item in pending)
        {
            if (item.Upgrader is not ICodeUpgradeProvider provider)
                continue;

            var registry = new UpgradeRegistry();
            provider.DeclareUpgrades(registry);

            // Gate: the change landed at GateVersion, so migrate any project coming from below it.
            var applicable = registry.CodeUpgrades
                .Where(registration => item.FromVersion.Version < registration.GateVersion.Version)
                .SelectMany(registration => registration.Steps)
                .ToList();
            if (applicable.Count > 0)
                plans.Add((item, applicable));
        }
        return plans;
    }

    // Returns false, having written nothing, when the workspace lacks a project
    private static async Task<bool> RunAsync(UFile? solutionPath, List<(PendingCodeUpgrade Pending, List<CodeStep> Steps)> plans, UpgradeBackup? backup, ILogger log)
    {
        var cancellationToken = CancellationToken.None;

        // The restore's own messages (NuGet audit, ...) are logged by the restore; replayed here they read as failures
        using var workspace = MSBuildWorkspace.Create(new Dictionary<string, string> { ["EmitAssetsLogMessages"] = "false" });
        if (solutionPath is not null)
        {
            // Solution-coordinated upgrade: open the whole solution (one consistent old closure).
            await workspace.OpenSolutionAsync(solutionPath.ToOSPath(), cancellationToken: cancellationToken);
        }
        else
        {
            // Standalone upgrade (no solution): open each pending project directly into the workspace.
            foreach (var (pendingItem, _) in plans)
                await workspace.OpenProjectAsync(pendingItem.ProjectFullPath.ToOSPath(), cancellationToken: cancellationToken);
        }

        var originalSolution = ParseDocumentationComments(workspace.CurrentSolution);

        foreach (var diagnostic in workspace.Diagnostics)
        {
            // Surface workspace failures (e.g. a project that wouldn't load) so a no-op migration is
            // diagnosable; routine messages stay verbose.
            if (diagnostic.Kind == WorkspaceDiagnosticKind.Failure)
                log.Warning($"Code upgrade workspace: {diagnostic.Message}");
            else
                log.Verbose($"Code upgrade workspace: {diagnostic.Message}");
        }

        var projectIds = new List<ProjectId>();
        foreach (var (pendingItem, _) in plans)
        {
            var projectOsPath = pendingItem.ProjectFullPath.ToOSPath();
            var project = originalSolution.Projects.FirstOrDefault(x => PathsEqual(x.FilePath, projectOsPath));
            if (project is null)
            {
                log.Error($"Code upgrade: project [{pendingItem.ProjectFullPath.GetFileName()}] not found in the workspace; no source code is migrated.");
                return false;
            }
            projectIds.Add(project.Id);
        }

        var solution = originalSolution;
        foreach (var ((pendingItem, steps), projectId) in plans.Zip(projectIds))
        {
            IReadOnlyList<ProjectId> targets = [projectId];
            foreach (var step in steps)
            {
                // A failing step is skipped alone: the others of the project still apply
                try
                {
                    solution = await step.Upgrade(solution, targets, cancellationToken);
                }
                catch (Exception e) when (e is not NotImplementedException)
                {
                    log.Warning($"Code upgrade: the step \"{step.Title}\" of [{pendingItem.ProjectFullPath.GetFileName()}] failed and was skipped; its code is left unchanged.", e);
                }
            }
        }

        // Write back only the documents that actually changed, preserving each file's original encoding.
        var changedCount = 0;
        foreach (var projectChanges in solution.GetChanges(originalSolution).GetProjectChanges())
        {
            // We persist changed .cs text only; reject any other project change rather than drop it silently.
            EnsureNoUnsupportedChanges(projectChanges);

            foreach (var documentId in projectChanges.GetChangedDocuments())
            {
                var newDocument = solution.GetDocument(documentId);
                var oldDocument = originalSolution.GetDocument(documentId);
                if (newDocument?.FilePath is null)
                    continue;

                var newText = await newDocument.GetTextAsync(cancellationToken);
                var oldText = oldDocument is not null ? await oldDocument.GetTextAsync(cancellationToken) : null;
                var encoding = oldText?.Encoding ?? new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

                try
                {
                    // Snapshot the original before overwriting it (copy-on-write; no-op when no backup is armed).
                    backup?.Snapshot(newDocument.FilePath);

                    using var writer = new StreamWriter(newDocument.FilePath, append: false, encoding);
                    newText.Write(writer, cancellationToken);
                }
                catch (Exception e)
                {
                    log.Warning($"Code upgrade: could not write migrated file [{newDocument.FilePath}]; left unchanged.", e);
                    continue;
                }

                log.Info($"Code upgrade: migrated [{Path.GetFileName(newDocument.FilePath)}]");
                changedCount++;
            }
        }

        if (changedCount > 0)
            log.Info($"Code upgrade: migrated {changedCount} source file(s).");
        return true;
    }

    /// <summary>
    /// Parses the documentation comments of the projects that generate no documentation file, which the workspace opens with
    /// these comments as plain text: the upgrades then see the references in them, and the compiler counts the usings they need.
    /// </summary>
    internal static Solution ParseDocumentationComments(Solution solution)
    {
        foreach (var projectId in solution.ProjectIds)
        {
            if (solution.GetProject(projectId)?.ParseOptions is CSharpParseOptions { DocumentationMode: DocumentationMode.None } parseOptions)
                solution = solution.WithProjectParseOptions(projectId, parseOptions.WithDocumentationMode(DocumentationMode.Parse));
        }
        return solution;
    }

    // Enumerates the full ProjectChanges surface except GetChangedDocuments (the only delta the runner persists);
    // anything else would need workspace.TryApplyChanges() to reach the .csproj, so fail loud instead of dropping it.
    private static void EnsureNoUnsupportedChanges(ProjectChanges projectChanges)
    {
        if (projectChanges.GetAddedDocuments().Any()
            || projectChanges.GetRemovedDocuments().Any()
            || projectChanges.GetChangedAdditionalDocuments().Any()
            || projectChanges.GetAddedAdditionalDocuments().Any()
            || projectChanges.GetRemovedAdditionalDocuments().Any()
            || projectChanges.GetChangedAnalyzerConfigDocuments().Any()
            || projectChanges.GetAddedAnalyzerConfigDocuments().Any()
            || projectChanges.GetRemovedAnalyzerConfigDocuments().Any()
            || projectChanges.GetAddedMetadataReferences().Any()
            || projectChanges.GetRemovedMetadataReferences().Any()
            || projectChanges.GetAddedProjectReferences().Any()
            || projectChanges.GetRemovedProjectReferences().Any()
            || projectChanges.GetAddedAnalyzerReferences().Any()
            || projectChanges.GetRemovedAnalyzerReferences().Any())
        {
            throw new NotImplementedException(
                $"Code upgrade for [{projectChanges.NewProject.Name}] produced changes other than regular document " +
                "text (added/removed docs, references, etc.); the runner persists changed .cs text only.");
        }
    }

    private static bool PathsEqual(string? a, string? b)
    {
        if (a is null || b is null)
            return false;
        return string.Equals(Path.GetFullPath(a), Path.GetFullPath(b), StringComparison.OrdinalIgnoreCase);
    }
}
