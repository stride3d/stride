// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Text;

namespace Stride.Assets;

/// <summary>
/// Applies a batch of <see cref="SymbolRewrite"/>s: resolves their symbols against the target
/// projects' compilations, finds every reference across the (original) target documents, then applies
/// the collected edits once per document via a <see cref="DocumentEditor"/>.
/// </summary>
internal static class SymbolRewriteEngine
{
    private readonly record struct PendingEdit(TextSpan Span, SymbolRewrite Rewrite, ISymbol Symbol);

    public static async Task<Solution> ApplyAsync(Solution solution, IReadOnlyList<ProjectId> targets, IReadOnlyList<SymbolRewrite> rewrites, CancellationToken cancellationToken)
    {
        // The documents we're allowed to rewrite (real user source — never generated/obj output).
        var targetDocuments = ImmutableHashSet.CreateBuilder<Document>();
        var targetDocumentIds = new HashSet<DocumentId>();
        foreach (var projectId in targets)
        {
            var project = solution.GetProject(projectId);
            if (project is null)
                continue;
            foreach (var document in project.Documents)
            {
                if (!IsUpgradableSource(document))
                    continue;
                targetDocuments.Add(document);
                targetDocumentIds.Add(document.Id);
            }
        }
        if (targetDocuments.Count == 0)
            return solution;

        var documentScope = targetDocuments.ToImmutable();

        // Collect edits against the ORIGINAL trees, grouped per document.
        var editsByDocument = new Dictionary<DocumentId, List<PendingEdit>>();
        foreach (var rewrite in rewrites)
        {
            foreach (var symbol in await ResolveSymbolsAsync(solution, targets, rewrite, cancellationToken))
            {
                var references = await SymbolFinder.FindReferencesAsync(symbol, solution, documentScope, cancellationToken);
                foreach (var referencedSymbol in references)
                {
                    foreach (var location in referencedSymbol.Locations)
                    {
                        // Implicit locations are not skipped: some carry rewritable syntax (a target-typed
                        // `new(...)` referencing a constructor is reported implicit). Ones that truly have
                        // none (e.g. foreach's GetEnumerator) match no rewriter pattern and no-op.
                        var documentId = location.Document.Id;
                        if (!targetDocumentIds.Contains(documentId))
                            continue;
                        if (!editsByDocument.TryGetValue(documentId, out var list))
                            editsByDocument[documentId] = list = [];
                        list.Add(new PendingEdit(location.Location.SourceSpan, rewrite, symbol));
                    }
                }
            }
        }
        if (editsByDocument.Count == 0)
            return solution;

        // Apply per document. Each document is processed once (keyed by id), so its root is still the
        // original tree when we read it — the collected spans stay valid.
        foreach (var (documentId, edits) in editsByDocument)
        {
            var document = solution.GetDocument(documentId);
            if (document is null)
                continue;

            var root = await document.GetSyntaxRootAsync(cancellationToken);
            if (root is null)
                continue;

            var editor = await DocumentEditor.CreateAsync(document, cancellationToken);
            var appliedSpans = new HashSet<TextSpan>();
            foreach (var edit in edits)
            {
                // A node can be matched by multiple symbols (e.g. extension skeleton + implementation);
                // apply each span once.
                if (!appliedSpans.Add(edit.Span))
                    continue;
                var node = root.FindNode(edit.Span, getInnermostNodeForTie: true);
                if (node is null)
                    continue;
                edit.Rewrite.RewriteReference(editor, node, edit.Symbol);
            }

            solution = editor.GetChangedDocument().Project.Solution;
        }

        return solution;
    }

    private static async Task<List<ISymbol>> ResolveSymbolsAsync(Solution solution, IReadOnlyList<ProjectId> targets, SymbolRewrite rewrite, CancellationToken cancellationToken)
    {
        var result = new List<ISymbol>();
        var seen = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
        foreach (var projectId in targets)
        {
            var project = solution.GetProject(projectId);
            if (project is null)
                continue;
            var compilation = await project.GetCompilationAsync(cancellationToken);
            if (compilation is null)
                continue;
            foreach (var symbol in rewrite.ResolveSymbols(compilation))
            {
                if (symbol is not null && seen.Add(symbol))
                    result.Add(symbol);
            }
        }
        return result;
    }

    /// <summary>
    /// A real, rewritable source file: has a path, supports syntax, and is not under the project's
    /// output directories. <c>.sdsl.cs</c>/<c>.sdfx.cs</c> are still compile items at this point (this
    /// pre-pass runs before the structural phase retires them to <c>.bak</c>), so skip them explicitly;
    /// other generated code is rewritten, keeping it compiling until regeneration overwrites it.
    /// </summary>
    internal static bool IsUpgradableSource(Document document)
    {
        if (document.FilePath is null || !document.SupportsSyntaxTree)
            return false;

        if (document.FilePath.EndsWith(".sdsl.cs", StringComparison.OrdinalIgnoreCase) || document.FilePath.EndsWith(".sdfx.cs", StringComparison.OrdinalIgnoreCase))
            return false;

        return !IsUnderOutputDirectory(document);
    }

    /// <summary>
    /// True when the document lives under the project's intermediate or final output directory (taken
    /// from the project's own compilation output paths, wherever the build put them).
    /// </summary>
    private static bool IsUnderOutputDirectory(Document document)
    {
        var documentPath = Path.GetFullPath(document.FilePath!);
        foreach (var outputFilePath in (string?[])[document.Project.CompilationOutputInfo.AssemblyPath, document.Project.OutputFilePath, document.Project.OutputRefFilePath])
        {
            if (Path.GetDirectoryName(outputFilePath) is not { } outputDirectory)
                continue;
            if (documentPath.StartsWith(Path.GetFullPath(outputDirectory) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

}
