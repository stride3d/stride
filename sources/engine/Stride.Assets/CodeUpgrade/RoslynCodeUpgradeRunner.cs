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
using Microsoft.CodeAnalysis.MSBuild;
using Stride.Core.Assets;
using Stride.Core.Diagnostics;
using Stride.Core.IO;

namespace Stride.Assets;

/// <summary>
/// Roslyn implementation of <see cref="ICodeUpgradeRunner"/>: opens the solution (already restored at
/// the from-version closure), folds the declared <see cref="CodeUpgrade"/>s over it, and writes the
/// changed source files back. Registered into <see cref="CodeUpgradeRunner.Instance"/> at module init.
/// </summary>
internal sealed class RoslynCodeUpgradeRunner : ICodeUpgradeRunner
{
    public void Run(UFile? solutionPath, IReadOnlyList<PendingCodeUpgrade> pending, ILogger log)
    {
        // Resolve each pending upgrader's applicable code rules up front. If nothing applies, never open
        // the (expensive) workspace — this keeps project-only upgrades at exactly today's cost.
        var plans = new List<(PendingCodeUpgrade Pending, List<CodeUpgrade> Upgrades)>();
        foreach (var item in pending)
        {
            if (item.Upgrader is not ICodeUpgradeProvider provider)
                continue;

            var registry = new UpgradeRegistry();
            provider.DeclareUpgrades(registry);

            var applicable = new List<CodeUpgrade>();
            foreach (var registration in registry.CodeUpgrades)
            {
                // Gate: the change landed at GateVersion, so migrate any project coming from below it.
                if (item.FromVersion.Version >= registration.GateVersion.Version)
                    continue;

                // resolveSince is the exact version where the matched OLD form first exists. When it's above
                // the from-version the rule can't bind against the from-version closure; resolving it would
                // need a version-overridden intermediate restore of the package family at that version
                // (sequenced low→high, merging overlapping resolution windows, replaying any family package
                // rename up to it), then a fresh workspace opened there. Not implemented — fail loud rather
                // than silently resolve at the from-version, which would no-op the rule.
                if (registration.ResolveSince is { } resolveSince && resolveSince.Version > item.FromVersion.Version)
                    throw new NotImplementedException(
                        $"Code upgrade gated at {registration.GateVersion} declares resolveSince {resolveSince}, above the " +
                        $"project's from-version {item.FromVersion}. Intermediate version-overridden restores (chained " +
                        $"boundaries) are not implemented yet; only single-step resolution at the from-version is supported.");

                applicable.AddRange(registration.Upgrades);
            }
            if (applicable.Count > 0)
                plans.Add((item, applicable));
        }
        if (plans.Count == 0)
        {
            log.Info($"Code upgrade: {pending.Count} pending project(s), no applicable source migrations.");
            return;
        }

        log.Info($"Code upgrade: applying source migrations to {plans.Count} project(s)...");

        try
        {
            // Escape any ambient synchronization context; MSBuildWorkspace is async-heavy.
            Task.Run(() => RunAsync(solutionPath, plans, log)).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            log.Warning("Source-code migration failed; the package upgrade will continue (code left unchanged).", ex);
        }
    }

    private static async Task RunAsync(UFile? solutionPath, List<(PendingCodeUpgrade Pending, List<CodeUpgrade> Upgrades)> plans, ILogger log)
    {
        var cancellationToken = CancellationToken.None;

        using var workspace = MSBuildWorkspace.Create();
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

        var originalSolution = workspace.CurrentSolution;

        foreach (var diagnostic in workspace.Diagnostics)
        {
            // Surface workspace failures (e.g. a project that wouldn't load) so a no-op migration is
            // diagnosable; routine messages stay verbose.
            if (diagnostic.Kind == WorkspaceDiagnosticKind.Failure)
                log.Warning($"Code upgrade workspace: {diagnostic.Message}");
            else
                log.Verbose($"Code upgrade workspace: {diagnostic.Message}");
        }

        var solution = originalSolution;
        foreach (var (pendingItem, upgrades) in plans)
        {
            var projectOsPath = pendingItem.ProjectFullPath.ToOSPath();
            var project = solution.Projects.FirstOrDefault(x => PathsEqual(x.FilePath, projectOsPath));
            if (project is null)
            {
                log.Warning($"Code upgrade: project [{pendingItem.ProjectFullPath.GetFileName()}] not found in the workspace; skipping its source migration.");
                continue;
            }

            IReadOnlyList<ProjectId> targets = [project.Id];
            foreach (var upgrade in upgrades)
                solution = await upgrade(solution, targets, cancellationToken);
        }

        // Write back only the documents that actually changed, preserving each file's original encoding.
        var changedCount = 0;
        foreach (var projectChanges in solution.GetChanges(originalSolution).GetProjectChanges())
        {
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
            log.Info($"Code upgrade: migrated {changedCount} source file(s). Review the changes before building (back up / commit first if you haven't).");
    }

    private static bool PathsEqual(string? a, string? b)
    {
        if (a is null || b is null)
            return false;
        return string.Equals(Path.GetFullPath(a), Path.GetFullPath(b), StringComparison.OrdinalIgnoreCase);
    }
}
