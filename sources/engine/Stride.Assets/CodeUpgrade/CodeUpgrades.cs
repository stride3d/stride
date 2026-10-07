// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
/// Public types that moved to another namespace, keeping their names.
/// </summary>
/// <param name="TypeNames">Metadata names (<c>Foo`1</c> for a generic type).</param>
public sealed record TypeMove(string OldNamespace, string NewNamespace, params string[] TypeNames);

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
    /// Migrates a static member that moved to another (non-nested) type, optionally renamed:
    /// <c>Utilities.FreeMemory(p)</c> → <c>MemoryUtilities.Free(p)</c>. Alias and <c>using static</c>
    /// references become fully qualified, since the new type can't be assumed in scope.
    /// </summary>
    public static SymbolRewrite StaticMemberMove(string declaringType, string memberName, string newDeclaringType, string newMemberName = null)
    {
        ArgumentNullException.ThrowIfNull(declaringType);
        ArgumentNullException.ThrowIfNull(memberName);
        ArgumentNullException.ThrowIfNull(newDeclaringType);
        newMemberName ??= memberName;

        var oldTypeName = declaringType[(declaringType.LastIndexOf('.') + 1)..];
        var newTypeName = newDeclaringType[(newDeclaringType.LastIndexOf('.') + 1)..];
        var sameNamespace = declaringType[..^oldTypeName.Length] == newDeclaringType[..^newTypeName.Length];

        return new SymbolRewrite(
            compilation => ResolveMembers(compilation, declaringType, memberName),
            (editor, referenceNode, symbol) =>
            {
                // Anything that isn't the member-name identifier (e.g. a reference inside a doc comment,
                // which the engine doesn't descend into) is left alone.
                if (referenceNode is not SimpleNameSyntax name || name.Identifier.ValueText != memberName)
                    return;

                // WithIdentifier keeps the type argument list of a generic name (Swap<T>).
                var newName = name.WithIdentifier(SyntaxFactory.Identifier(name.Identifier.LeadingTrivia, newMemberName, name.Identifier.TrailingTrivia));

                if (name.Parent is MemberAccessExpressionSyntax memberAccess && memberAccess.Name == name)
                {
                    var receiver = memberAccess.Expression;
                    var newReceiver = receiver;
                    if (editor.SemanticModel.GetSymbolInfo(receiver).Symbol is ITypeSymbol)
                    {
                        var receiverTypeName = receiver switch
                        {
                            MemberAccessExpressionSyntax qualified => qualified.Name,
                            AliasQualifiedNameSyntax aliasQualified => aliasQualified.Name,
                            _ => receiver as SimpleNameSyntax,
                        };
                        newReceiver = sameNamespace && receiverTypeName is not null && receiverTypeName.Identifier.ValueText == oldTypeName
                            ? receiver.ReplaceNode(receiverTypeName, SyntaxFactory.IdentifierName(newTypeName).WithTriviaFrom(receiverTypeName))
                            : SyntaxFactory.ParseExpression(newDeclaringType).WithTriviaFrom(receiver);
                    }
                    editor.ReplaceNode(memberAccess, memberAccess.WithExpression(newReceiver).WithName(newName));
                }
                else if (name.Parent is not MemberBindingExpressionSyntax)
                {
                    // Bare reference through `using static OldType;`, which doesn't bring the new type
                    // (or even its namespace) in scope.
                    var qualified = SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.ParseExpression(newDeclaringType),
                        newName.WithoutTrivia());
                    editor.ReplaceNode(name, qualified.WithTriviaFrom(name));
                }
                else
                {
                    editor.ReplaceNode(name, newName);
                }
            });
    }

    /// <summary>
    /// Migrates a removed method that returned its receiver, so that the call can simply go:
    /// <c>Buffer.New(...).RecreateWith(data)</c> → <c>Buffer.New(...)</c>. Matches the method
    /// <paramref name="methodName"/> on <paramref name="declaringType"/> (its full metadata name) and
    /// replaces each invocation by its receiver, preserving trivia. Also handles conditional access
    /// (<c>x?.Foo(a)</c> → <c>x</c>). Other reference forms (method groups, cref) are left untouched.
    /// </summary>
    public static SymbolRewrite FluentCallRemove(string declaringType, string methodName)
    {
        ArgumentNullException.ThrowIfNull(declaringType);
        ArgumentNullException.ThrowIfNull(methodName);
        return new SymbolRewrite(
            compilation => ResolveMembers(compilation, declaringType, methodName),
            static (editor, referenceNode, symbol) =>
            {
                if (referenceNode.Parent is MemberAccessExpressionSyntax memberAccess && memberAccess.Name == referenceNode
                    && memberAccess.Parent is InvocationExpressionSyntax invocation && invocation.Expression == memberAccess)
                {
                    ReplaceByReceiver(editor, invocation, memberAccess.Expression);
                }
                else if (referenceNode.Parent is MemberBindingExpressionSyntax memberBinding && memberBinding.Name == referenceNode
                    && memberBinding.Parent is InvocationExpressionSyntax conditionalInvocation
                    && conditionalInvocation.Parent is ConditionalAccessExpressionSyntax conditionalAccess && conditionalAccess.WhenNotNull == conditionalInvocation)
                {
                    ReplaceByReceiver(editor, conditionalAccess, conditionalAccess.Expression);
                }
            });

        // Where the value of the call is discarded, the receiver stays only if it is a statement itself (x.Foo(a); goes,
        // New().Foo(a); becomes New();)
        static void ReplaceByReceiver(DocumentEditor editor, ExpressionSyntax call, ExpressionSyntax receiver)
        {
            if (!IsValueDiscarded(editor.SemanticModel, call)
                || receiver is InvocationExpressionSyntax or ObjectCreationExpressionSyntax or ImplicitObjectCreationExpressionSyntax or AwaitExpressionSyntax)
            {
                editor.ReplaceNode(call, receiver.WithTriviaFrom(call));
                return;
            }

            switch (call.Parent)
            {
                case ExpressionStatementSyntax statement:
                    RemoveStatement(editor, statement);
                    break;
                case ForStatementSyntax:
                    editor.RemoveNode(call);
                    break;
                case AnonymousFunctionExpressionSyntax function:
                    editor.ReplaceNode(function, function.WithExpressionBody(null).WithBlock(SyntaxFactory.Block()));
                    break;
                case ArrowExpressionClauseSyntax { Parent: BaseMethodDeclarationSyntax member }:
                    editor.ReplaceNode(member, member.WithExpressionBody(null).WithSemicolonToken(default).WithBody(SyntaxFactory.Block()));
                    break;
                case ArrowExpressionClauseSyntax { Parent: LocalFunctionStatementSyntax member }:
                    editor.ReplaceNode(member, member.WithExpressionBody(null).WithSemicolonToken(default).WithBody(SyntaxFactory.Block()));
                    break;
                case ArrowExpressionClauseSyntax { Parent: AccessorDeclarationSyntax member }:
                    editor.ReplaceNode(member, member.WithExpressionBody(null).WithSemicolonToken(default).WithBody(SyntaxFactory.Block()));
                    break;
            }
        }

        // A statement, a for-loop initializer or increment, and the expression body of a member or lambda returning nothing
        static bool IsValueDiscarded(SemanticModel semanticModel, ExpressionSyntax call)
        {
            return call.Parent switch
            {
                ExpressionStatementSyntax => true,
                ForStatementSyntax forStatement => forStatement.Initializers.Contains(call) || forStatement.Incrementors.Contains(call),
                ArrowExpressionClauseSyntax arrow => semanticModel.GetDeclaredSymbol(arrow.Parent) is IMethodSymbol method && ReturnsNothing(method),
                AnonymousFunctionExpressionSyntax function => semanticModel.GetSymbolInfo(function).Symbol is IMethodSymbol method && ReturnsNothing(method),
                _ => false,
            };

            // An async method returning a plain Task or ValueTask discards its expression body too
            static bool ReturnsNothing(IMethodSymbol method)
                => method.ReturnsVoid || method.IsAsync && method.ReturnType is INamedTypeSymbol { IsGenericType: false };
        }
    }

    /// <summary>
    /// Migrates a removed member that user code only assigned or subscribed to: the statements
    /// <c>x.Reload = ...;</c>, <c>x.DeviceReset += ...;</c> and <c>x.DeviceReset -= ...;</c> are removed.
    /// Matches the member <paramref name="memberName"/> on <paramref name="declaringType"/> (its full
    /// metadata name). A read of the member is left untouched: the code around it needs manual porting,
    /// and the compile error shows where.
    /// </summary>
    public static SymbolRewrite AssignmentRemove(string declaringType, string memberName)
    {
        ArgumentNullException.ThrowIfNull(declaringType);
        ArgumentNullException.ThrowIfNull(memberName);
        return new SymbolRewrite(
            compilation => ResolveMembers(compilation, declaringType, memberName),
            static (editor, referenceNode, symbol) =>
            {
                var target = referenceNode;
                if (referenceNode.Parent is MemberAccessExpressionSyntax memberAccess && memberAccess.Name == referenceNode)
                    target = memberAccess;

                if (target.Parent is AssignmentExpressionSyntax assignment && assignment.Left == target
                    && assignment.Parent is ExpressionStatementSyntax statement)
                {
                    RemoveStatement(editor, statement);
                }
            });
    }

    /// <summary>
    /// Removes a statement. One that is not in a list of statements (the body of an <c>if</c>, <c>else</c>, loop or
    /// <c>using</c> without braces, or a labeled statement) cannot go, so it becomes an empty block.
    /// </summary>
    private static void RemoveStatement(SyntaxEditor editor, StatementSyntax statement)
    {
        switch (statement.Parent)
        {
            case BlockSyntax or SwitchSectionSyntax:
                editor.RemoveNode(statement, SyntaxRemoveOptions.KeepNoTrivia);
                break;
            case GlobalStatementSyntax globalStatement:
                editor.RemoveNode(globalStatement, SyntaxRemoveOptions.KeepNoTrivia);
                break;
            default:
                editor.ReplaceNode(statement, SyntaxFactory.Block().WithTriviaFrom(statement));
                break;
        }
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
    /// Migrates code using types that moved to another namespace: qualified names and using directives take the new
    /// namespace, and old directives nothing else uses are removed. Usage is read against the old-version closure.
    /// </summary>
    public static CodeUpgrade MoveTypes(params TypeMove[] moves)
    {
        ArgumentNullException.ThrowIfNull(moves);
        var rewrites = moves.SelectMany(move => move.TypeNames.Select(typeName => QualifiedTypeMove(move, typeName))).ToArray();
        var newNamespaces = moves.SelectMany(move => move.TypeNames.Select(typeName => (Type: (move.OldNamespace, TypeName: typeName), move.NewNamespace)))
            .ToDictionary(x => x.Type, x => x.NewNamespace);
        var oldNamespaces = moves.Select(move => move.OldNamespace).ToHashSet(StringComparer.Ordinal);
        // A document needs no change unless it contains the name of a moved type or of an old namespace
        var keywords = moves.SelectMany(move => move.TypeNames.Select(typeName => typeName.Split('`')[0])).Concat(oldNamespaces).Distinct().ToArray();

        return async (solution, targets, cancellationToken) =>
        {
            var originalSolution = solution;
            solution = await SymbolRewriteEngine.ApplyAsync(solution, targets, rewrites, cancellationToken);

            foreach (var projectId in targets)
            {
                var project = originalSolution.GetProject(projectId);
                var compilation = project is not null ? await project.GetCompilationAsync(cancellationToken) : null;
                if (compilation is null || !newNamespaces.Keys.Any(type => compilation.GetTypeByMetadataName($"{type.OldNamespace}.{type.TypeName}") is not null))
                    continue;
                var emptiedNamespaces = oldNamespaces.Where(x => IsEmptied(compilation, x, newNamespaces)).ToHashSet(StringComparer.Ordinal);

                foreach (var originalDocument in project.Documents)
                {
                    if (!SymbolRewriteEngine.IsUpgradableSource(originalDocument))
                        continue;
                    var content = (await originalDocument.GetTextAsync(cancellationToken)).ToString();
                    if (!keywords.Any(keyword => content.Contains(keyword, StringComparison.Ordinal)))
                        continue;

                    var (imports, usedForMovedTypes, stillUsed) = await FindNamespaceUsageAsync(originalDocument, newNamespaces, oldNamespaces, cancellationToken);
                    if (await solution.GetDocument(originalDocument.Id).GetSyntaxRootAsync(cancellationToken) is not CompilationUnitSyntax root)
                        continue;
                    var removable = oldNamespaces
                        .Where(x => !stillUsed.Contains(x) && (usedForMovedTypes.Contains(x) || emptiedNamespaces.Contains(x)))
                        .ToHashSet(StringComparer.Ordinal);
                    var newRoot = UpdateUsings(root, moves, imports, removable, content.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n");
                    if (newRoot != root)
                        solution = solution.WithDocumentSyntaxRoot(originalDocument.Id, newRoot);
                }
            }
            return solution;
        };
    }

    // A reference qualified by the old namespace takes the new one; the other references are left to the using directives
    private static SymbolRewrite QualifiedTypeMove(TypeMove move, string typeName)
    {
        return new SymbolRewrite(
            compilation => compilation.GetTypeByMetadataName($"{move.OldNamespace}.{typeName}") is { } type ? [type] : Array.Empty<ISymbol>(),
            (editor, referenceNode, symbol) =>
            {
                if (referenceNode is not SimpleNameSyntax name)
                    return;
                var (qualified, qualifier) = name.Parent switch
                {
                    QualifiedNameSyntax qualifiedName when qualifiedName.Right == name => ((ExpressionSyntax)qualifiedName, (ExpressionSyntax)qualifiedName.Left),
                    MemberAccessExpressionSyntax memberAccess when memberAccess.Name == name => (memberAccess, memberAccess.Expression),
                    _ => (null, null),
                };
                if (qualified is null || editor.SemanticModel.GetSymbolInfo(qualifier).Symbol is not INamespaceSymbol)
                    return;

                var global = qualifier.DescendantNodesAndSelf().OfType<AliasQualifiedNameSyntax>().Any(x => x.Alias.Identifier.IsKind(SyntaxKind.GlobalKeyword)) ? "global::" : "";
                var newName = SyntaxFactory.ParseName($"{global}{move.NewNamespace}.{name.WithoutTrivia()}");
                editor.ReplaceNode(qualified, newName.WithTriviaFrom(qualified));
            });
    }

    // True when every type of the namespace the code can use moved
    private static bool IsEmptied(Compilation compilation, string namespaceName, Dictionary<(string OldNamespace, string TypeName), string> newNamespaces)
    {
        var namespaceSymbol = compilation.GlobalNamespace;
        foreach (var part in namespaceName.Split('.'))
        {
            namespaceSymbol = namespaceSymbol.GetNamespaceMembers().FirstOrDefault(x => x.Name == part);
            if (namespaceSymbol is null)
                return false;
        }
        return !namespaceSymbol.GetNamespaceMembers().Any()
            && namespaceSymbol.GetTypeMembers().All(type => newNamespaces.ContainsKey((namespaceName, type.MetadataName))
                || type.DeclaredAccessibility != Accessibility.Public && !type.Locations.Any(x => x.IsInSource));
    }

    // New namespaces to import, old namespaces used for moved types, and old namespaces still used for other types
    private static async Task<(SortedSet<string> Imports, HashSet<string> UsedForMovedTypes, HashSet<string> StillUsed)> FindNamespaceUsageAsync(Document document,
        Dictionary<(string OldNamespace, string TypeName), string> newNamespaces, HashSet<string> oldNamespaces, CancellationToken cancellationToken)
    {
        var imports = new SortedSet<string>(StringComparer.Ordinal);
        var usedForMovedTypes = new HashSet<string>(StringComparer.Ordinal);
        var stillUsed = new HashSet<string>(StringComparer.Ordinal);
        var root = await document.GetSyntaxRootAsync(cancellationToken);
        var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
        if (root is null || semanticModel is null)
            return (imports, usedForMovedTypes, stillUsed);

        foreach (var name in root.DescendantNodes(descendIntoTrivia: true).OfType<SimpleNameSyntax>())
        {
            if (name.Ancestors().Any(x => x is UsingDirectiveSyntax))
                continue;

            var symbolInfo = semanticModel.GetSymbolInfo(name, cancellationToken);
            var symbol = symbolInfo.Symbol ?? symbolInfo.CandidateSymbols.FirstOrDefault();
            if (symbol is IMethodSymbol { MethodKind: MethodKind.Constructor } constructor)
                symbol = constructor.ContainingType;
            else if (symbol is IMethodSymbol { ReducedFrom: { } extensionMethod })
                symbol = extensionMethod;

            // The type that brings the name in scope: the type itself, or the type declaring the member
            var type = symbol as INamedTypeSymbol ?? symbol?.ContainingType;
            while (type?.ContainingType is { } containingType)
                type = containingType;
            if (type?.ContainingNamespace is not { IsGlobalNamespace: false } typeNamespace)
                continue;
            var namespaceName = typeNamespace.ToDisplayString();
            if (!oldNamespaces.Contains(namespaceName))
                continue;

            if (!newNamespaces.TryGetValue((namespaceName, type.OriginalDefinition.MetadataName), out var newNamespace))
                stillUsed.Add(namespaceName);
            else if (symbol is INamedTypeSymbol && !IsQualified(name) && semanticModel.GetAliasInfo(name, cancellationToken) is null
                || symbol is IMethodSymbol { IsExtensionMethod: true })
            {
                imports.Add(newNamespace);
                usedForMovedTypes.Add(namespaceName);
            }
        }
        return (imports, usedForMovedTypes, stillUsed);

        static bool IsQualified(SimpleNameSyntax name) => name.Parent switch
        {
            QualifiedNameSyntax qualifiedName => qualifiedName.Right == name,
            AliasQualifiedNameSyntax aliasQualifiedName => aliasQualifiedName.Name == name,
            MemberAccessExpressionSyntax memberAccess => memberAccess.Name == name,
            MemberBindingExpressionSyntax => true,
            _ => false,
        };
    }

    // Replaces each removable old directive by the needed new ones; the other needed directives go after the existing ones
    private static CompilationUnitSyntax UpdateUsings(CompilationUnitSyntax root, TypeMove[] moves, SortedSet<string> imports, HashSet<string> removable, string endOfLine)
    {
        var directives = root.DescendantNodes().OfType<UsingDirectiveSyntax>().Where(x => x.Alias is null && x.StaticKeyword.IsKind(SyntaxKind.None)).ToList();
        var pending = new SortedSet<string>(imports.Except(directives.Select(GetNamespace)), StringComparer.Ordinal);

        var replacements = new Dictionary<UsingDirectiveSyntax, List<UsingDirectiveSyntax>>();
        foreach (var directive in directives)
        {
            var oldNamespace = GetNamespace(directive);
            if (!directive.GlobalKeyword.IsKind(SyntaxKind.None) || !removable.Contains(oldNamespace))
                continue;

            var newDirectives = new List<UsingDirectiveSyntax>();
            foreach (var newNamespace in moves.Where(x => x.OldNamespace == oldNamespace).Select(x => x.NewNamespace).Distinct().Order(StringComparer.Ordinal))
            {
                if (!pending.Remove(newNamespace))
                    continue;
                var newDirective = directive.WithName(SyntaxFactory.ParseName(newNamespace).WithTriviaFrom(directive.Name));
                // Only the first one keeps the comments above the old directive
                if (newDirectives.Count > 0)
                    newDirective = newDirective.WithLeadingTrivia(directive.GetLeadingTrivia().Where(x => x.IsKind(SyntaxKind.WhitespaceTrivia)));
                newDirectives.Add(newDirective);
            }
            replacements.Add(directive, newDirectives);
        }

        var result = root.TrackNodes(replacements.Keys);
        foreach (var (directive, newDirectives) in replacements)
        {
            var current = result.GetCurrentNode(directive);
            if (newDirectives.Count > 0)
            {
                result = result.ReplaceNode(current, newDirectives);
                continue;
            }

            // The comments above a removed directive (such as the file header) go to the next line
            var leadingTrivia = current.GetLeadingTrivia();
            if (leadingTrivia.Any(x => !x.IsKind(SyntaxKind.WhitespaceTrivia) && !x.IsKind(SyntaxKind.EndOfLineTrivia)))
            {
                var nextToken = current.GetLastToken().GetNextToken();
                var kept = leadingTrivia.Reverse().SkipWhile(x => x.IsKind(SyntaxKind.WhitespaceTrivia)).Reverse();
                result = result.ReplaceToken(nextToken, nextToken.WithLeadingTrivia(kept.Concat(nextToken.LeadingTrivia)));
                current = result.GetCurrentNode(directive);
            }
            result = result.RemoveNode(current, SyntaxRemoveOptions.KeepNoTrivia);
        }

        if (pending.Count > 0)
        {
            var newDirectives = pending.Select(x => SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(x).WithLeadingTrivia(SyntaxFactory.Space))
                .WithTrailingTrivia(SyntaxFactory.EndOfLine(endOfLine))).ToArray();
            if (result.Usings.Count == 0 && result.Externs.Count == 0)
            {
                // The comments at the top of the file stay above the directives
                var firstToken = result.GetFirstToken(includeZeroWidth: true);
                newDirectives[0] = newDirectives[0].WithLeadingTrivia(firstToken.LeadingTrivia);
                result = result.ReplaceToken(firstToken, firstToken.WithLeadingTrivia(SyntaxFactory.EndOfLine(endOfLine)));
            }
            result = result.AddUsings(newDirectives);
        }
        return result;

        static string GetNamespace(UsingDirectiveSyntax directive)
        {
            var name = directive.Name?.WithoutTrivia().ToString() ?? "";
            return name.StartsWith("global::", StringComparison.Ordinal) ? name["global::".Length..] : name;
        }
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
