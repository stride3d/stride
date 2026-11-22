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
using Microsoft.CodeAnalysis.Text;
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

    // Each pending upgrader's code steps that apply to the version its project is upgraded from, with their IDs
    private static List<(PendingCodeUpgrade Pending, List<(CodeStep Step, string Id)> Steps)> GetPlans(IReadOnlyList<PendingCodeUpgrade> pending)
    {
        var plans = new List<(PendingCodeUpgrade Pending, List<(CodeStep Step, string Id)> Steps)>();
        foreach (var item in pending)
        {
            if (item.Upgrader is not ICodeUpgradeProvider provider)
                continue;

            var registry = new UpgradeRegistry();
            provider.DeclareUpgrades(registry);

            // Gate: the change landed at GateVersion, so migrate any project coming from below it.
            var applicable = registry.GetSteps(item.Upgrader.Attribute.Name)
                .Where(step => step.Declaration is CodeStep && item.FromVersion.Version < step.GateVersion.Version)
                .Select(step => ((CodeStep)step.Declaration, step.Id))
                .ToList();
            if (applicable.Count > 0)
                plans.Add((item, applicable));
        }
        return plans;
    }

    /// <summary>
    /// Folds <paramref name="steps"/> over the project <paramref name="projectId"/>, each on the solution the previous one
    /// left, and reports in <paramref name="report"/> what each did. A step that throws is skipped alone: the others still
    /// apply. A document a step could not change stays as it was before that step, for the rest of the upgrade.
    /// </summary>
    /// <remarks>
    /// To report the changes a step did not make, each step also runs alone on the original solution, once an earlier step
    /// changed it: a change it makes there is made when its own edits in the fold touch that place.
    /// </remarks>
    internal static async Task<Solution> ApplyStepsAsync(Solution solution, ProjectId projectId, IReadOnlyList<(CodeStep Step, string Id)> steps,
        UpgraderReport? report, string projectName, ILogger log, CancellationToken cancellationToken)
    {
        IReadOnlyList<ProjectId> targets = [projectId];
        var original = solution;
        var frozen = new HashSet<DocumentId>();
        var foldChanged = false;
        // Each change not made, with its document and its place in the original code, until the fold ends
        var missedPlaces = new List<(UpgradeStepResult Result, int Index, DocumentId Document, int Start)>();
        foreach (var (step, id) in steps)
        {
            var result = report?.Find(id);
            var failedDocuments = new List<(DocumentId Document, Exception Exception)>();
            Solution upgraded;
            try
            {
                using (CodeUpgradeUnits.Collect((document, exception) => failedDocuments.Add((document, exception))))
                    upgraded = await step.Upgrade(solution, targets, cancellationToken);
            }
            catch (Exception e) when (e is not NotImplementedException)
            {
                result?.SetFailed(e);
                log.Warning($"{id}: Code upgrade: the step \"{step.Title}\" of [{projectName}] failed and was skipped; its code is left unchanged.", e);
                continue;
            }

            foreach (var (documentId, exception) in failedDocuments)
            {
                if (!frozen.Add(documentId))
                    continue;
                var path = solution.GetDocument(documentId)?.FilePath ?? documentId.ToString();
                result?.FailedUnits.Add(new UpgradeUnitFailure(path, Exception: exception));
                log.Warning($"{id}: Code upgrade: the step \"{step.Title}\" could not change [{Path.GetFileName(path)}] of [{projectName}]; it is left unchanged for the rest of the upgrade.", exception);
            }
            upgraded = await RestoreDocumentsAsync(upgraded, solution, frozen, cancellationToken);

            var changes = await GetTextChangesAsync(upgraded, solution, cancellationToken);
            if (result is not null)
            {
                result.Edits = changes.Sum(x => x.Value.Count);
                result.ChangedFiles.AddRange(changes.Keys.Select(x => upgraded.GetDocument(x)?.FilePath).OfType<string>());
                if (foldChanged)
                    await AddMissedChangesAsync(result, step, id, original, solution, changes, frozen, targets, missedPlaces, projectName, log, cancellationToken);
                result.SetRan();
            }
            foldChanged |= changes.Count > 0;
            solution = upgraded;
        }

        // The place of each change not made, in the code the upgrade writes: the steps before and after it moved it
        var finalChanges = await GetTextChangesAsync(solution, original, cancellationToken);
        foreach (var (result, index, documentId, start) in missedPlaces)
        {
            if (solution.GetDocument(documentId) is not { } document)
                continue;
            var place = MapSpan(new TextSpan(start, 0), finalChanges.GetValueOrDefault(documentId) ?? []);
            var position = (await document.GetTextAsync(cancellationToken)).Lines.GetLinePosition(place.Start);
            result.MissedChanges[index] = result.MissedChanges[index] with { Line = position.Line + 1, Column = position.Character + 1 };
        }
        return solution;
    }

    // Runs the step alone on the original solution, and reports each change it makes there whose place, after the edits of the
    // earlier steps, none of its own edits in the fold touches; missedPlaces gets their places in the original code
    private static async Task AddMissedChangesAsync(UpgradeStepResult result, CodeStep step, string id, Solution original, Solution input,
        Dictionary<DocumentId, List<TextChange>> foldChanges, HashSet<DocumentId> frozen, IReadOnlyList<ProjectId> targets,
        List<(UpgradeStepResult Result, int Index, DocumentId Document, int Start)> missedPlaces, string projectName, ILogger log, CancellationToken cancellationToken)
    {
        Dictionary<DocumentId, List<TextChange>> intended;
        try
        {
            using (CodeUpgradeUnits.Collect((_, _) => { }))
                intended = await GetTextChangesAsync(await step.Upgrade(original, targets, cancellationToken), original, cancellationToken);
        }
        catch (Exception e) when (e is not NotImplementedException)
        {
            log.Verbose($"{id}: Code upgrade: the step \"{step.Title}\" could not run alone on the code of [{projectName}], to find the changes it did not make: {e.Message}");
            return;
        }

        var earlierChanges = await GetTextChangesAsync(input, original, cancellationToken);
        foreach (var (documentId, changes) in intended)
        {
            if (frozen.Contains(documentId) || original.GetDocument(documentId) is not { FilePath: { } path } document)
                continue;

            var text = await document.GetTextAsync(cancellationToken);
            var made = foldChanges.GetValueOrDefault(documentId) ?? [];
            var earlier = earlierChanges.GetValueOrDefault(documentId) ?? [];
            foreach (var change in changes)
            {
                // The code an earlier step deleted is no longer there to change
                if (earlier.Any(x => string.IsNullOrWhiteSpace(x.NewText) && x.Span.Contains(change.Span)))
                    continue;
                var place = MapSpan(change.Span, earlier);
                if (made.Any(x => x.Span.IntersectsWith(place)))
                    continue;
                var start = ToWords(text, change).Span.Start;
                var position = text.Lines.GetLinePosition(start);
                missedPlaces.Add((result, result.MissedChanges.Count, documentId, start));
                result.MissedChanges.Add(new UpgradeMissedChange(path, position.Line + 1, position.Character + 1, DescribeChange(text, change)));
            }
        }
    }

    /// <summary>
    /// The place of <paramref name="span"/> of a text, after <paramref name="changes"/> of that text (in order, not overlapping):
    /// a span that a change rewrote covers what the change wrote.
    /// </summary>
    internal static TextSpan MapSpan(TextSpan span, IReadOnlyList<TextChange> changes)
    {
        return TextSpan.FromBounds(Map(span.Start, start: true), Math.Max(Map(span.Start, start: true), Map(span.End, start: false)));

        int Map(int position, bool start)
        {
            var shift = 0;
            foreach (var change in changes)
            {
                var newLength = change.NewText?.Length ?? 0;
                if (change.Span.End <= position && !(start && change.Span.IsEmpty && change.Span.Start == position))
                {
                    shift += newLength - change.Span.Length;
                    continue;
                }
                if (change.Span.Start < position)
                    return change.Span.Start + shift + (start ? 0 : newLength);
                break;
            }
            return position + shift;
        }
    }

    // The documents of solution that are in documentIds, put back as they are in previous
    private static async Task<Solution> RestoreDocumentsAsync(Solution solution, Solution previous, HashSet<DocumentId> documentIds, CancellationToken cancellationToken)
    {
        foreach (var documentId in documentIds)
        {
            if (previous.GetDocument(documentId) is { } document && solution.GetDocument(documentId) is { } changed && changed != document)
                solution = solution.WithDocumentText(documentId, await document.GetTextAsync(cancellationToken));
        }
        return solution;
    }

    // The text changes of each document that solution changed from previous
    private static async Task<Dictionary<DocumentId, List<TextChange>>> GetTextChangesAsync(Solution solution, Solution previous, CancellationToken cancellationToken)
    {
        var result = new Dictionary<DocumentId, List<TextChange>>();
        foreach (var documentId in solution.GetChanges(previous).GetProjectChanges().SelectMany(x => x.GetChangedDocuments()))
        {
            if (solution.GetDocument(documentId) is not { } document || previous.GetDocument(documentId) is not { } previousDocument)
                continue;
            var changes = (await document.GetTextChangesAsync(previousDocument, cancellationToken)).ToList();
            if (changes.Count > 0)
                result[documentId] = changes;
        }
        return result;
    }

    // The length a change shows of the code, on one line
    private const int ChangeDescriptionLength = 80;

    /// <summary>
    /// A change of <paramref name="text"/> in one line: <c>fmt.IsSRgb() → fmt.IsSRgb</c>, <c>add [EffectKeys]</c> or
    /// <c>remove buffer.Reload = null;</c>.
    /// </summary>
    internal static string DescribeChange(SourceText text, TextChange change)
    {
        change = ToWords(text, change);
        var before = OneLine(text.ToString(change.Span));
        var after = OneLine(change.NewText ?? "");
        return (before.Length, after.Length) switch
        {
            (0, _) => $"add {after}",
            (_, 0) => $"remove {before}",
            _ => $"{before} → {after}",
        };

        static string OneLine(string code)
        {
            var line = string.Join(' ', code.Split((char[])[' ', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries));
            return line.Length <= ChangeDescriptionLength ? line : line[..(ChangeDescriptionLength - 1)] + "…";
        }
    }

    // The change of whole words: the diff keeps what an identifier shares with the new one (Old → Other is ld → ther)
    private static TextChange ToWords(SourceText text, TextChange change)
    {
        var start = change.Span.Start;
        while (start > 0 && IsWordPart(text[start - 1]))
            start--;
        var end = change.Span.End;
        while (end < text.Length && IsWordPart(text[end]))
            end++;
        return new TextChange(TextSpan.FromBounds(start, end), text.ToString(TextSpan.FromBounds(start, change.Span.Start)) + change.NewText + text.ToString(TextSpan.FromBounds(change.Span.End, end)));

        static bool IsWordPart(char c) => char.IsLetterOrDigit(c) || c == '_';
    }

    // Returns false, having written nothing, when the workspace lacks a project
    private static async Task<bool> RunAsync(UFile? solutionPath, List<(PendingCodeUpgrade Pending, List<(CodeStep Step, string Id)> Steps)> plans, UpgradeBackup? backup, ILogger log)
    {
        var cancellationToken = CancellationToken.None;

        // The restore's own messages (NuGet audit, ...) are logged by the restore; replayed here they read as failures
        using var workspace = MSBuildWorkspace.Create(new Dictionary<string, string> { ["EmitAssetsLogMessages"] = "false" });
        if (solutionPath is not null)
        {
            // Solution-coordinated upgrade: open the whole solution (one consistent old closure), then the projects added to
            // the session since the solution file was saved
            await workspace.OpenSolutionAsync(solutionPath.ToOSPath(), cancellationToken: cancellationToken);
            foreach (var (pendingItem, _) in plans)
            {
                var projectOsPath = pendingItem.ProjectFullPath.ToOSPath();
                if (!workspace.CurrentSolution.Projects.Any(x => PathsEqual(x.FilePath, projectOsPath)))
                    await workspace.OpenProjectAsync(projectOsPath, cancellationToken: cancellationToken);
            }
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
            solution = await ApplyStepsAsync(solution, projectId, steps, pendingItem.Report, pendingItem.ProjectFullPath.GetFileName(), log, cancellationToken);

        var changedCount = await WriteChangedDocumentsAsync(solution, originalSolution, [.. plans.Select(x => x.Pending.Report)], backup, log, cancellationToken);
        if (changedCount > 0)
            log.Info($"Code upgrade: migrated {changedCount} source file(s).");
        return true;
    }

    /// <summary>
    /// Writes back the documents of <paramref name="solution"/> that changed from <paramref name="originalSolution"/>, each in
    /// the encoding of its file. A file that can't be written stays as it was: the steps of <paramref name="reports"/> that
    /// changed it report it left unchanged.
    /// </summary>
    /// <returns>The number of files written.</returns>
    internal static async Task<int> WriteChangedDocumentsAsync(Solution solution, Solution originalSolution, IReadOnlyList<UpgraderReport?> reports, UpgradeBackup? backup, ILogger log, CancellationToken cancellationToken)
    {
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
                    foreach (var step in reports.OfType<UpgraderReport>().SelectMany(x => x.Steps).Where(x => x.ChangedFiles.Contains(newDocument.FilePath, StringComparer.OrdinalIgnoreCase)))
                    {
                        step.ChangedFiles.RemoveAll(x => string.Equals(x, newDocument.FilePath, StringComparison.OrdinalIgnoreCase));
                        step.FailedUnits.Add(new UpgradeUnitFailure(newDocument.FilePath, Exception: e));
                        step.SetRan();
                    }
                    continue;
                }

                log.Info($"Code upgrade: migrated [{Path.GetFileName(newDocument.FilePath)}]");
                changedCount++;
            }
        }
        return changedCount;
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
