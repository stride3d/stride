// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;

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
    /// Migrates an instance member that became an extension member in <paramref name="extensionNamespace"/>: bare uses get
    /// <c>this.</c> and the file gets the <c>using</c>. With <paramref name="throughMember"/>, uses go through that member (<c>x.Game.Audio</c>).
    /// </summary>
    public static SymbolRewrite MemberToExtension(string declaringType, string memberName, string extensionNamespace, string throughMember = null)
    {
        ArgumentNullException.ThrowIfNull(declaringType);
        ArgumentNullException.ThrowIfNull(memberName);
        ArgumentNullException.ThrowIfNull(extensionNamespace);
        return new SymbolRewrite(
            compilation => ResolveMembers(compilation, declaringType, memberName),
            (editor, referenceNode, symbol) =>
            {
                // The member's own name only (not an alias or another node the reference search returns)
                if (referenceNode is not SimpleNameSyntax name || name.Identifier.ValueText != memberName)
                    return;

                var member = name.WithoutTrivia();
                switch (name.Parent)
                {
                    // x.Audio, this.Audio, base.Audio: base. becomes this. (base can't reach an extension member)
                    case MemberAccessExpressionSyntax access when access.Name == name:
                        if (throughMember is not null)
                            editor.ReplaceNode(access, Access(Access(access.Expression, SyntaxFactory.IdentifierName(throughMember)), member).WithTriviaFrom(access));
                        else if (access.Expression is BaseExpressionSyntax)
                            editor.ReplaceNode(access, Access(SyntaxFactory.ThisExpression(), member).WithTriviaFrom(access));
                        break;

                    // x?.Audio
                    case MemberBindingExpressionSyntax binding:
                        if (throughMember is not null)
                            editor.ReplaceNode(binding, Access(SyntaxFactory.MemberBindingExpression(SyntaxFactory.IdentifierName(throughMember)), member).WithTriviaFrom(binding));
                        break;

                    // A name in a type context (cref, qualified name): left alone
                    case QualifiedNameSyntax qualified when qualified.Right == name:
                    case NameMemberCrefSyntax:
                        break;

                    // Not an expression: a property pattern's name ({ Audio: ... }), a named argument or initializer
                    case NameColonSyntax:
                    case NameEqualsSyntax:
                        break;

                    // A bare use in a derived type needs an explicit receiver
                    default:
                        ExpressionSyntax receiver = throughMember is null ? SyntaxFactory.ThisExpression() : SyntaxFactory.IdentifierName(throughMember);
                        editor.ReplaceNode(name, Access(receiver, member).WithTriviaFrom(name));
                        break;
                }

                // Import the namespace once per file (the callback sees the root as edited so far)
                editor.ReplaceNode(editor.OriginalRoot, (root, _) => root is CompilationUnitSyntax unit ? AddUsing(unit, extensionNamespace) : root);
            });
    }

    private static MemberAccessExpressionSyntax Access(ExpressionSyntax receiver, SimpleNameSyntax member)
        => SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, receiver, member);

    private static CompilationUnitSyntax AddUsing(CompilationUnitSyntax unit, string @namespace)
    {
        if (unit.Usings.Any(x => x.Name?.ToString() == @namespace))
            return unit;
        var directive = SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(@namespace)).NormalizeWhitespace().WithTrailingTrivia(SyntaxFactory.EndOfLine(Environment.NewLine));
        return unit.AddUsings(directive);
    }

    /// <summary>
    /// Migrates an interface that gained a required member: each class that implements <paramref name="interfaceType"/> itself,
    /// rather than through a base class that already does, gets <paramref name="memberDeclaration"/> after its last property,
    /// and the file gets the <c>using</c> for <paramref name="memberNamespace"/>. A class that already has a member with that name is left alone.
    /// </summary>
    /// <param name="interfaceType">The full metadata name of the interface, e.g. <c>"Stride.Input.IInputDevice"</c>.</param>
    /// <param name="memberDeclaration">The property or event to add, as C# source.</param>
    /// <param name="memberNamespace">The namespace the member's types live in.</param>
    public static CodeUpgrade InterfaceMemberAdd(string interfaceType, string memberDeclaration, string memberNamespace)
    {
        ArgumentNullException.ThrowIfNull(interfaceType);
        ArgumentNullException.ThrowIfNull(memberDeclaration);
        ArgumentNullException.ThrowIfNull(memberNamespace);
        var (member, memberName) = SyntaxFactory.ParseMemberDeclaration(memberDeclaration) switch
        {
            PropertyDeclarationSyntax property => ((MemberDeclarationSyntax)property, property.Identifier.ValueText),
            EventDeclarationSyntax @event => (@event, @event.Identifier.ValueText),
            _ => throw new ArgumentException("The member must be a property or an event with accessors.", nameof(memberDeclaration)),
        };

        return async (solution, targets, cancellationToken) =>
        {
            foreach (var projectId in targets)
            {
                var project = solution.GetProject(projectId);
                if (project is null)
                    continue;

                foreach (var documentId in project.DocumentIds)
                {
                    // Read from the solution as edited so far: a partial class completed in one file is complete in the next
                    var document = solution.GetDocument(documentId);
                    if (document is null || !SymbolRewriteEngine.IsUpgradableSource(document))
                        continue;

                    var root = await document.GetSyntaxRootAsync(cancellationToken);
                    var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
                    var @interface = semanticModel?.Compilation.GetTypeByMetadataName(interfaceType);
                    if (root is not CompilationUnitSyntax unit || @interface is null)
                        continue;

                    var classes = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
                    var declarations = new List<TypeDeclarationSyntax>();
                    var needsUsing = false;
                    foreach (var declaration in unit.DescendantNodes().OfType<TypeDeclarationSyntax>())
                    {
                        if (semanticModel.GetDeclaredSymbol(declaration, cancellationToken) is not { TypeKind: TypeKind.Class } type
                            || !type.AllInterfaces.Contains(@interface, SymbolEqualityComparer.Default)
                            || (type.BaseType?.AllInterfaces.Contains(@interface, SymbolEqualityComparer.Default) ?? false)
                            || !type.GetMembers(memberName).IsEmpty
                            || !classes.Add(type))
                            continue;

                        declarations.Add(declaration);
                        var containingNamespace = type.ContainingNamespace.ToDisplayString();
                        needsUsing |= containingNamespace != memberNamespace && !containingNamespace.StartsWith(memberNamespace + ".", StringComparison.Ordinal);
                    }
                    if (declarations.Count == 0)
                        continue;

                    var newRoot = unit.ReplaceNodes(declarations, (_, declaration) => InsertMember(declaration, member));
                    if (needsUsing)
                        newRoot = AddUsing(newRoot, memberNamespace);

                    var formatted = await Formatter.FormatAsync(document.WithSyntaxRoot(newRoot), Formatter.Annotation, cancellationToken: cancellationToken);
                    solution = formatted.Project.Solution;
                }
            }
            return solution;
        };
    }

    /// <summary>
    /// Inserts a member after the last property, or else before the first member, with that member's indentation and line ending.
    /// A member added to a class without members, or written on one line, is left to the formatter.
    /// </summary>
    private static TypeDeclarationSyntax InsertMember(TypeDeclarationSyntax declaration, MemberDeclarationSyntax member)
    {
        var members = declaration.Members;
        var lastProperty = members.LastOrDefault(x => x is BasePropertyDeclarationSyntax);
        var anchor = lastProperty ?? members.FirstOrDefault();
        var endOfLine = anchor?.GetTrailingTrivia().Where(x => x.IsKind(SyntaxKind.EndOfLineTrivia)).TakeLast(1).ToList();
        if (endOfLine is not [_])
            return declaration.AddMembers(member.WithAdditionalAnnotations(Formatter.Annotation));

        var indentation = anchor.GetLeadingTrivia().Where(x => x.IsKind(SyntaxKind.WhitespaceTrivia)).TakeLast(1);
        member = member.WithLeadingTrivia(indentation).WithTrailingTrivia(endOfLine);
        return declaration.WithMembers(members.Insert(lastProperty is null ? 0 : members.IndexOf(lastProperty) + 1, member));
    }

    /// <summary>
    /// Migrates overrides of a property whose overridable part moved to another property:
    /// <c>public override GamePadState State => state;</c> → <c>protected override GamePadState RawState => state;</c>.
    /// Each override of <paramref name="propertyName"/> on <paramref name="declaringType"/> (its full metadata name) is renamed
    /// <paramref name="newPropertyName"/> with the accessibility <paramref name="newAccessibility"/>, and <c>base.</c> uses of the
    /// property inside it go to the new property too, since the old one now calls the new one.
    /// </summary>
    public static CodeUpgrade PropertyOverrideRename(string declaringType, string propertyName, string newPropertyName, SyntaxKind newAccessibility)
    {
        ArgumentNullException.ThrowIfNull(declaringType);
        ArgumentNullException.ThrowIfNull(propertyName);
        ArgumentNullException.ThrowIfNull(newPropertyName);
        if (!SyntaxFacts.IsAccessibilityModifier(newAccessibility))
            throw new ArgumentException("The accessibility must be an accessibility keyword.", nameof(newAccessibility));

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
                    var overridden = semanticModel?.Compilation.GetTypeByMetadataName(declaringType)?.GetMembers(propertyName).OfType<IPropertySymbol>().FirstOrDefault();
                    if (root is null || overridden is null)
                        continue;

                    var overrides = root.DescendantNodes().OfType<PropertyDeclarationSyntax>()
                        .Where(declaration => Overrides(semanticModel.GetDeclaredSymbol(declaration, cancellationToken), overridden))
                        .ToList();
                    if (overrides.Count == 0)
                        continue;

                    var newRoot = root.ReplaceNodes(overrides, (_, declaration) =>
                    {
                        var baseUses = declaration.DescendantNodes().OfType<MemberAccessExpressionSyntax>()
                            .Where(x => x.Expression is BaseExpressionSyntax && x.Name.Identifier.ValueText == propertyName);
                        declaration = declaration.ReplaceNodes(baseUses, (_, access) => access.WithName(SyntaxFactory.IdentifierName(newPropertyName).WithTriviaFrom(access.Name)));

                        // The first accessibility keyword becomes the new one and the others go (public → protected, protected internal → protected)
                        var modifiers = new SyntaxTokenList();
                        var replaced = false;
                        foreach (var modifier in declaration.Modifiers)
                        {
                            if (!SyntaxFacts.IsAccessibilityModifier(modifier.Kind()))
                                modifiers = modifiers.Add(modifier);
                            else if (!replaced)
                            {
                                modifiers = modifiers.Add(SyntaxFactory.Token(newAccessibility).WithTriviaFrom(modifier));
                                replaced = true;
                            }
                        }

                        return declaration
                            .WithModifiers(modifiers)
                            .WithIdentifier(SyntaxFactory.Identifier(newPropertyName).WithTriviaFrom(declaration.Identifier));
                    });
                    solution = solution.WithDocumentSyntaxRoot(documentId, newRoot);
                }
            }
            return solution;
        };

        static bool Overrides(IPropertySymbol property, IPropertySymbol overridden)
        {
            for (var current = property?.OverriddenProperty; current is not null; current = current.OverriddenProperty)
            {
                if (SymbolEqualityComparer.Default.Equals(current.OriginalDefinition, overridden))
                    return true;
            }
            return false;
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
