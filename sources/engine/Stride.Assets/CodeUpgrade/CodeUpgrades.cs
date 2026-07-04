// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

namespace Stride.Assets;

/// <summary>
/// A symbol-driven rewrite: resolves the OLD symbol(s) by their literal type+member name (never
/// <c>nameof</c> — the name is frozen at upgrade-authoring time), then rewrites every reference the
/// semantic model points at <see cref="RewriteReference"/>. Resolving against the old-version
/// compilation and using <c>FindReferences</c> catches every reference form (qualified, instance
/// sugar, generics, base lists, attributes, cref) and never fires on an unrelated same-named member.
/// </summary>
/// <param name="ResolveSymbols">Resolves the matched symbol(s) from the old-version compilation.</param>
/// <param name="RewriteReference">
/// Applies the edit at one reference. Receives the editor, the syntax node at the reference location
/// (typically the member-name identifier), and the matched symbol.
/// </param>
public sealed record SymbolRewrite(
    Func<Compilation, IEnumerable<ISymbol>> ResolveSymbols,
    Action<DocumentEditor, SyntaxNode, ISymbol> RewriteReference);

/// <summary>
/// Factory helpers for declaring code migrations. Import with
/// <c>using static Stride.Assets.CodeUpgrades;</c> so registrations read as intent.
/// </summary>
public static class CodeUpgrades
{
    /// <summary>
    /// Batches symbol rewrites into one <see cref="CodeUpgrade"/>: all symbols are resolved and their
    /// references found against the ORIGINAL solution, edits collected, then applied once per document.
    /// Matchers in one batch are order-independent (each matches the original symbols) and must not
    /// depend on each other's edits — put a genuine dependency in a separate <see cref="CodeUpgrade"/>.
    /// </summary>
    public static CodeUpgrade Rewrite(params SymbolRewrite[] rewrites)
    {
        ArgumentNullException.ThrowIfNull(rewrites);
        return (solution, targets, cancellationToken) => SymbolRewriteEngine.ApplyAsync(solution, targets, rewrites, cancellationToken);
    }

    /// <summary>
    /// Raw escape hatch: wrap an arbitrary <see cref="CodeUpgrade"/> (full Roslyn) for cases the member
    /// helpers don't cover — structural multi-node patterns, conditional/cross-cutting rewrites, etc.
    /// </summary>
    public static CodeUpgrade Custom(CodeUpgrade upgrade)
    {
        ArgumentNullException.ThrowIfNull(upgrade);
        return upgrade;
    }

    /// <summary>
    /// Migrates a property that became a (parameterless) method: <c>x.Foo</c> → <c>x.Foo()</c>.
    /// Matches the property <paramref name="propertyName"/> on <paramref name="declaringType"/> (its
    /// full metadata name, e.g. <c>"Stride.Graphics.PixelFormatExtensions"</c>) and wraps each access in
    /// an invocation, preserving the receiver and trivia. Also handles extension members and conditional
    /// access (<c>x?.Foo</c> → <c>x?.Foo()</c>).
    /// </summary>
    public static SymbolRewrite PropertyToMethod(string declaringType, string propertyName)
    {
        ArgumentNullException.ThrowIfNull(declaringType);
        ArgumentNullException.ThrowIfNull(propertyName);
        return new SymbolRewrite(
            compilation => ResolveMembers(compilation, declaringType, propertyName),
            static (editor, referenceNode, symbol) =>
            {
                // The reference location points at the member-name identifier; promote it to the whole
                // member access/binding so the receiver is carried into the invocation.
                var target = referenceNode;
                if (referenceNode.Parent is MemberAccessExpressionSyntax memberAccess && memberAccess.Name == referenceNode)
                    target = memberAccess;
                else if (referenceNode.Parent is MemberBindingExpressionSyntax memberBinding && memberBinding.Name == referenceNode)
                    target = memberBinding;

                if (target is not ExpressionSyntax expression)
                    return;

                // Defensive: never re-wrap an already-invoked node (idempotent re-runs / overlapping rules).
                if (target.Parent is InvocationExpressionSyntax invocation && invocation.Expression == target)
                    return;

                var rewritten = SyntaxFactory.InvocationExpression(expression.WithoutTrivia())
                    .WithLeadingTrivia(expression.GetLeadingTrivia())
                    .WithTrailingTrivia(expression.GetTrailingTrivia());
                editor.ReplaceNode(target, rewritten);
            });
    }

    /// <summary>
    /// Migrates a (parameterless) method that became a property: <c>x.Foo()</c> → <c>x.Foo</c>.
    /// Matches the method <paramref name="methodName"/> on <paramref name="declaringType"/> (its full
    /// metadata name, e.g. <c>"Stride.Graphics.PixelFormatExtensions"</c>) and unwraps each no-argument
    /// invocation to a plain member access, preserving the receiver and trivia. Also handles extension
    /// members and conditional access (<c>x?.Foo()</c> → <c>x?.Foo</c>). Calls passing arguments are left
    /// untouched (they can't become a property access).
    /// </summary>
    public static SymbolRewrite MethodToProperty(string declaringType, string methodName)
    {
        ArgumentNullException.ThrowIfNull(declaringType);
        ArgumentNullException.ThrowIfNull(methodName);
        return new SymbolRewrite(
            compilation => ResolveMembers(compilation, declaringType, methodName),
            static (editor, referenceNode, symbol) =>
            {
                // The reference location points at the member-name identifier; promote it to the whole
                // member access/binding — the invocation's Expression.
                var target = referenceNode;
                if (referenceNode.Parent is MemberAccessExpressionSyntax memberAccess && memberAccess.Name == referenceNode)
                    target = memberAccess;
                else if (referenceNode.Parent is MemberBindingExpressionSyntax memberBinding && memberBinding.Name == referenceNode)
                    target = memberBinding;

                // Only a no-argument invocation of this member becomes a property. Anything else — already
                // a property access on an idempotent re-run, or a call with arguments — is left as-is.
                if (target.Parent is not InvocationExpressionSyntax invocation
                    || invocation.Expression != target
                    || invocation.ArgumentList.Arguments.Count != 0)
                    return;

                var rewritten = target.WithoutTrivia()
                    .WithLeadingTrivia(invocation.GetLeadingTrivia())
                    .WithTrailingTrivia(invocation.GetTrailingTrivia());
                editor.ReplaceNode(invocation, rewritten);
            });
    }

    /// <summary>
    /// Migrates a renamed parameter at call sites: <c>x.Foo(count: 1)</c> → <c>x.Foo(indexCount: 1)</c>.
    /// Matches the method(s) named <paramref name="methodName"/> on <paramref name="declaringType"/> (its
    /// full metadata name) that have a parameter named <paramref name="oldParameterName"/>, and renames
    /// the matching named argument at every call site — invocations, object creations (including
    /// <c>new(...)</c>), <c>this</c>/<c>base</c> initializers and attributes. Positional arguments need no
    /// migration and are left untouched. Use <c>".ctor"</c> as <paramref name="methodName"/> for
    /// constructors.
    /// </summary>
    public static SymbolRewrite ParameterRename(string declaringType, string methodName, string oldParameterName, string newParameterName)
    {
        ArgumentNullException.ThrowIfNull(declaringType);
        ArgumentNullException.ThrowIfNull(methodName);
        ArgumentNullException.ThrowIfNull(oldParameterName);
        ArgumentNullException.ThrowIfNull(newParameterName);
        return new SymbolRewrite(
            compilation => ResolveMembers(compilation, declaringType, methodName)
                .Where(member => member is IMethodSymbol method && method.Parameters.Any(p => p.Name == oldParameterName)),
            (editor, referenceNode, symbol) =>
            {
                foreach (var nameColon in GetArgumentNameColons(referenceNode))
                {
                    if (nameColon.Name.Identifier.ValueText != oldParameterName)
                        continue;
                    editor.ReplaceNode(nameColon.Name, SyntaxFactory.IdentifierName(newParameterName).WithTriviaFrom(nameColon.Name));
                }
            });
    }

    /// <summary>
    /// Finds the argument list the referenced member is called with, and yields the
    /// <see cref="NameColonSyntax"/> of each named argument in it. Reference forms without an argument
    /// list (method groups, <c>nameof</c>, cref) yield nothing.
    /// </summary>
    private static IEnumerable<NameColonSyntax> GetArgumentNameColons(SyntaxNode referenceNode)
    {
        // Climb the name wrappers (qualification, member access) so target ends up the direct child of
        // the argument-bearing construct.
        var target = referenceNode;
        while (true)
        {
            if (target.Parent is QualifiedNameSyntax qualifiedName && qualifiedName.Right == target)
                target = qualifiedName;
            else if (target.Parent is AliasQualifiedNameSyntax aliasQualifiedName && aliasQualifiedName.Name == target)
                target = aliasQualifiedName;
            else if (target.Parent is MemberAccessExpressionSyntax memberAccess && memberAccess.Name == target)
                target = memberAccess;
            else if (target.Parent is MemberBindingExpressionSyntax memberBinding && memberBinding.Name == target)
                target = memberBinding;
            else
                break;
        }

        var argumentList = target switch
        {
            // Implicit object creation (`new(...)`) and this(...)/base(...) initializers: the reference
            // location is the construct itself.
            BaseObjectCreationExpressionSyntax creation => creation.ArgumentList,
            ConstructorInitializerSyntax initializer => initializer.ArgumentList,
            _ when target.Parent is InvocationExpressionSyntax invocation && invocation.Expression == target => invocation.ArgumentList,
            _ when target.Parent is ObjectCreationExpressionSyntax objectCreation && objectCreation.Type == target => objectCreation.ArgumentList,
            _ when target.Parent is PrimaryConstructorBaseTypeSyntax primaryBase && primaryBase.Type == target => primaryBase.ArgumentList,
            _ => null,
        };
        if (argumentList is not null)
        {
            foreach (var argument in argumentList.Arguments)
            {
                if (argument.NameColon is not null)
                    yield return argument.NameColon;
            }
        }
        else if (target.Parent is AttributeSyntax attribute && attribute.Name == target && attribute.ArgumentList is not null)
        {
            foreach (var argument in attribute.ArgumentList.Arguments)
            {
                if (argument.NameColon is not null)
                    yield return argument.NameColon;
            }
        }
    }

    /// <summary>
    /// Removes <c>using</c> directives for namespaces that left the Stride dependency closure
    /// (each entry matches itself and its sub-namespaces). Only directives the compiler reports as
    /// unnecessary (CS8019, checked against the old-version closure) are removed: an unused directive is
    /// dead weight that turns into a compile error once the namespace is gone, while a used one means the
    /// code needs manual porting anyway and is left for the user to see.
    /// </summary>
    public static CodeUpgrade RemoveUnusedUsings(params string[] namespaces)
    {
        ArgumentNullException.ThrowIfNull(namespaces);
        return async (solution, targets, cancellationToken) =>
        {
            foreach (var projectId in targets)
            {
                var project = solution.GetProject(projectId);
                if (project is null)
                    continue;

                foreach (var documentId in project.DocumentIds)
                {
                    var document = solution.GetDocument(documentId);
                    if (document is null || !SymbolRewriteEngine.IsUpgradableSource(document))
                        continue;

                    var root = await document.GetSyntaxRootAsync(cancellationToken);
                    var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
                    if (root is null || semanticModel is null)
                        continue;

                    // CS8019 (unnecessary using directive) is only reported through the semantic model.
                    var removable = new List<UsingDirectiveSyntax>();
                    foreach (var diagnostic in semanticModel.GetDiagnostics(cancellationToken: cancellationToken))
                    {
                        if (diagnostic.Id != "CS8019")
                            continue;
                        if (root.FindNode(diagnostic.Location.SourceSpan) is not UsingDirectiveSyntax usingDirective)
                            continue;
                        var name = usingDirective.Name?.ToString();
                        if (name is not null && namespaces.Any(ns => name == ns || name.StartsWith(ns + ".", StringComparison.Ordinal)))
                            removable.Add(usingDirective);
                    }
                    if (removable.Count == 0)
                        continue;

                    var newRoot = root.RemoveNodes(removable, SyntaxRemoveOptions.KeepNoTrivia);
                    solution = solution.WithDocumentSyntaxRoot(documentId, newRoot);
                }
            }
            return solution;
        };
    }

    /// <summary>
    /// Resolves members named <paramref name="memberName"/> on the type with metadata name
    /// <paramref name="declaringType"/>, including C# extension members (which live in nested extension
    /// grouping types on the static class). Returns nothing if the type isn't in the compilation — a
    /// missing reference yields no match (a safe false-negative), never a false positive.
    /// </summary>
    internal static IEnumerable<ISymbol> ResolveMembers(Compilation compilation, string declaringType, string memberName)
    {
        var type = compilation.GetTypeByMetadataName(declaringType);
        if (type is null)
            yield break;

        foreach (var member in type.GetMembers(memberName))
            yield return member;

        // C# 14 extension members are surfaced inside nested extension grouping types on the static
        // class; walk them too. (Iterating all nested types keeps this buildable on older Roslyn that
        // lacks the extension-member symbol API; non-extension nested types simply won't have a match.)
        foreach (var nested in type.GetTypeMembers())
        {
            foreach (var member in nested.GetMembers(memberName))
                yield return member;
        }
    }
}
