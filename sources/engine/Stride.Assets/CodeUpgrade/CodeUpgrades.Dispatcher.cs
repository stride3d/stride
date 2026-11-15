// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Stride.Assets;

public static partial class CodeUpgrades
{
    /// <summary>
    /// Moves the callbacks of <c>Dispatcher.For</c>, <c>ForEach</c> and <c>ForBatched</c> to the overloads that take a state
    /// and a static lambda: what the callbacks capture (<c>this</c>, locals) becomes the state, and their uses go through the
    /// lambda's state parameter. A call with a per-batch local (<c>initializeLocal</c>/<c>finalizeLocal</c>) becomes a
    /// <c>ForBatched</c> with the local set up around the loop. A <c>ForEach</c> over a dictionary gets the new callback shape
    /// too, and no longer compiles, so its author picks a collection with an index.
    /// A call is left unchanged when the move would change what it does, e.g. a callback that writes a captured local,
    /// or when it holds another Dispatcher call to move (that one moves).
    /// </summary>
    public static CodeUpgrade DispatcherCallbacksToStaticLambdas()
    {
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
                    var dispatcher = semanticModel?.Compilation.GetTypeByMetadataName("Stride.Core.Threading.Dispatcher");
                    if (root is null || dispatcher is null)
                        continue;

                    var calls = new List<DispatcherCall>();
                    foreach (var invocation in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
                    {
                        if (semanticModel.GetSymbolInfo(invocation, cancellationToken).Symbol is IMethodSymbol method
                            && SymbolEqualityComparer.Default.Equals(method.ContainingType, dispatcher)
                            && GetDispatcherCall(invocation, method) is { } call)
                            calls.Add(call);
                    }

                    var text = await document.GetTextAsync(cancellationToken);
                    var changes = new List<TextChange>();
                    foreach (var call in calls)
                    {
                        // The inner call moves; moving both would edit the same text
                        if (calls.Any(other => other != call && call.Callbacks.Contains(other.Invocation.Span)))
                            continue;

                        if (call.Local)
                        {
                            if (MoveLocalCallbacks(semanticModel, text, call, cancellationToken) is { } change)
                                changes.Add(change);
                        }
                        else if (MoveCallback(semanticModel, call, cancellationToken) is { } callChanges)
                        {
                            changes.AddRange(callChanges);
                        }
                    }
                    if (changes.Count == 0)
                        continue;

                    // An insertion goes before a replacement at the same position
                    changes = changes.OrderBy(x => x.Span.Start).ThenBy(x => x.Span.Length).ToList();
                    solution = solution.WithDocumentText(documentId, text.WithChanges(changes));
                }
            }
            return solution;
        };
    }

    /// <param name="Invocation">The call.</param>
    /// <param name="Method">The called overload.</param>
    /// <param name="Callbacks">The span of the callback arguments.</param>
    /// <param name="Local">Whether the overload has a per-batch local.</param>
    /// <param name="ParameterNames">The callback's parameter names to use when it has none (an anonymous method).</param>
    private sealed record DispatcherCall(InvocationExpressionSyntax Invocation, IMethodSymbol Method, TextSpan Callbacks, bool Local, string[] ParameterNames);

    /// <summary>
    /// A call to an overload that has a counterpart taking a state: <c>For(int, int, Action&lt;int&gt;)</c>,
    /// <c>ForBatched(int, Action&lt;int, int&gt;)</c>, the <c>ForEach</c> overloads taking an <c>Action&lt;T&gt;</c>,
    /// and the <c>For</c>/<c>ForEach</c> overloads with a per-batch local (over a list).
    /// </summary>
    private static DispatcherCall GetDispatcherCall(InvocationExpressionSyntax invocation, IMethodSymbol method)
    {
        var parameters = method.OriginalDefinition.Parameters;
        if (parameters.Length < 2)
            return null;

        var local = parameters.Any(x => x.Name == "initializeLocal");
        var callbackIndex = local ? parameters.IndexOf(parameters.First(x => x.Name == "initializeLocal")) : parameters.Length - 1;
        if (!local && !IsAction(parameters[^1].Type))
            return null;

        var known = (method.Name, parameters.Length, local) switch
        {
            ("For", 3, false) => ["index"],
            ("ForBatched", 2, false) => ["from", "toExclusive"],
            ("ForEach", 2, false) => ["item"],
            ("For", 5, true) => ["index", "local"],
            ("ForEach", 4, true) when !IsDictionary(parameters[0].Type) => ["item", "local"],
            _ => (string[])null,
        };
        if (known is null)
            return null;

        var arguments = Enumerable.Range(callbackIndex, parameters.Length - callbackIndex).Select(index => FindArgument(invocation, method, index)).Where(x => x is not null).ToList();
        if (arguments.Count == 0)
            return null;
        var span = TextSpan.FromBounds(arguments.Min(x => x.SpanStart), arguments.Max(x => x.Span.End));
        return new DispatcherCall(invocation, method, span, local, known);

        static bool IsAction(ITypeSymbol type) => type is INamedTypeSymbol { TypeKind: TypeKind.Delegate, Name: "Action" } action && action.ContainingNamespace?.ToDisplayString() == "System";
        static bool IsDictionary(ITypeSymbol type) => type is INamedTypeSymbol { Name: "Dictionary", Arity: 2 };
    }

    private static ArgumentSyntax FindArgument(InvocationExpressionSyntax invocation, IMethodSymbol method, int index)
    {
        var arguments = invocation.ArgumentList.Arguments;
        var name = method.Parameters[index].Name;
        return arguments.FirstOrDefault(x => x.NameColon?.Name.Identifier.ValueText == name)
            ?? (index < arguments.Count && arguments[index].NameColon is null ? arguments[index] : null);
    }

    private static List<TextChange> MoveCallback(SemanticModel semanticModel, DispatcherCall call, CancellationToken cancellationToken)
    {
        var invocation = call.Invocation;
        var callback = FindArgument(invocation, call.Method, call.Method.Parameters.Length - 1);
        var changes = new List<TextChange>();
        var expression = callback.Expression;
        string state, header;
        TextSpan headerSpan;

        if (expression is AnonymousFunctionExpressionSyntax function)
        {
            var parameterNames = GetParameterNames(function, call.ParameterNames);
            if (parameterNames is null || parameterNames.Length != call.ParameterNames.Length)
                return null;

            if (CollectCaptures(semanticModel, [function], cancellationToken) is not { } captures)
                return null;

            var stateName = UniqueName(semanticModel, function, captures.Count == 0 ? "_" : "state", parameterNames);
            state = BuildState(captures, stateName, null, changes);

            header = $"static ({stateName}, {string.Join(", ", parameterNames)}) =>";
            var headerEnd = function switch
            {
                LambdaExpressionSyntax lambda => lambda.ArrowToken.Span.End,
                AnonymousMethodExpressionSyntax anonymousMethod => anonymousMethod.ParameterList?.Span.End ?? anonymousMethod.DelegateKeyword.Span.End,
                _ => function.Body.SpanStart,
            };
            headerSpan = TextSpan.FromBounds(function.SpanStart, headerEnd);
        }
        else if (semanticModel.GetSymbolInfo(expression, cancellationToken).Symbol is IMethodSymbol { MethodKind: MethodKind.Ordinary } method)
        {
            // A method group: an instance method keeps its receiver as the state
            var parameterNames = method.Parameters.Select(x => x.Name).ToArray();
            if (parameterNames.Length != call.ParameterNames.Length || parameterNames.Contains("state") || parameterNames.Contains("_"))
                return null;

            var arguments = string.Join(", ", parameterNames);
            var methodName = expression is MemberAccessExpressionSyntax memberAccess ? memberAccess.Name.ToString() : expression.ToString();
            if (method.IsStatic)
            {
                state = "default(object)";
                header = $"static (_, {arguments}) => {expression}({arguments})";
            }
            else
            {
                if (expression is MemberAccessExpressionSyntax { Expression: BaseExpressionSyntax })
                    return null;
                state = expression is MemberAccessExpressionSyntax { Expression: var target } ? target.ToString() : "this";
                header = $"static (state, {arguments}) => state.{methodName}({arguments})";
            }
            headerSpan = expression.Span;
        }
        else
        {
            return null;
        }

        changes.Add(new TextChange(headerSpan, header));
        changes.Add(new TextChange(new TextSpan(callback.SpanStart, 0), callback.NameColon is null ? $"{state}, " : $"state: {state}, "));

        // The overloads taking a state have their own type parameters
        if (invocation.Expression is MemberAccessExpressionSyntax { Name: GenericNameSyntax genericName })
            changes.Add(new TextChange(genericName.Span, genericName.Identifier.Text));
        else if (invocation.Expression is GenericNameSyntax bareGenericName)
            changes.Add(new TextChange(bareGenericName.Span, bareGenericName.Identifier.Text));

        return changes;
    }

    /// <summary>
    /// Rewrites a call with a per-batch local to a <c>ForBatched</c> that sets up the local, loops over the batch and finalizes it.
    /// </summary>
    private static TextChange? MoveLocalCallbacks(SemanticModel semanticModel, SourceText text, DispatcherCall call, CancellationToken cancellationToken)
    {
        var invocation = call.Invocation;
        var method = call.Method;
        var isFor = method.Name == "For";
        var parameterIndex = method.Parameters.Select((x, i) => (x.Name, i)).ToDictionary(x => x.Name, x => x.i);
        var initializeArgument = FindArgument(invocation, method, parameterIndex["initializeLocal"]);
        var actionArgument = FindArgument(invocation, method, parameterIndex["action"]);
        var finalizeArgument = FindArgument(invocation, method, parameterIndex["finalizeLocal"]);

        // The local's initial value, the loop body and the finalization, all as lambdas
        if (initializeArgument?.Expression is not ParenthesizedLambdaExpressionSyntax { ParameterList.Parameters.Count: 0, AsyncKeyword.RawKind: 0 } initialize
            || initialize.Modifiers.Any(SyntaxKind.StaticKeyword)
            || GetReturnedExpression(initialize) is not { } initialValue
            || actionArgument?.Expression is not LambdaExpressionSyntax action || action.AsyncKeyword != default || action.Modifiers.Any(SyntaxKind.StaticKeyword)
            || GetParameterNames(action, call.ParameterNames) is not [var itemName, var localName])
            return null;

        LambdaExpressionSyntax finalize = null;
        string finalizeParameter = null;
        if (finalizeArgument is not null && !finalizeArgument.Expression.IsKind(SyntaxKind.NullLiteralExpression))
        {
            if (finalizeArgument.Expression is not LambdaExpressionSyntax finalizeLambda || finalizeLambda.AsyncKeyword != default
                || GetParameterNames(finalizeLambda, ["local"]) is not [var name])
                return null;
            finalize = finalizeLambda;
            finalizeParameter = name;
        }

        // The callbacks' parameters were copies for each call; the loop shares them
        var actionFlow = action.Body switch
        {
            ExpressionSyntax expression => semanticModel.AnalyzeDataFlow(expression),
            StatementSyntax statement => semanticModel.AnalyzeDataFlow(statement),
            _ => null,
        };
        var actionParameters = action is ParenthesizedLambdaExpressionSyntax parenthesizedAction
            ? parenthesizedAction.ParameterList.Parameters.Select(x => semanticModel.GetDeclaredSymbol(x, cancellationToken)).ToArray()
            : [];
        if (actionFlow is null || !actionFlow.Succeeded || actionParameters.Length != 2 || actionParameters.Any(x => x is null || actionFlow.WrittenInside.Contains(x)))
            return null;

        // A return leaves the item, which is a continue in the loop, unless a nested loop would take that continue
        var returns = action.Body is BlockSyntax ? FindOwnReturns(action.Body).ToList() : [];
        if (returns.Any(x => x.Expression is not null || x.Ancestors().TakeWhile(node => node != action.Body).Any(node => node is ForStatementSyntax or ForEachStatementSyntax or WhileStatementSyntax or DoStatementSyntax or ForEachVariableStatementSyntax)))
            return null;

        // The loop's names: the names the callbacks used must mean the same inside it
        var initializeNames = initialize.Body.DescendantTokens().Where(x => x.IsKind(SyntaxKind.IdentifierToken)).Select(x => x.ValueText).ToHashSet();
        if (initializeNames.Contains(itemName) || initializeNames.Contains(localName))
            return null;
        if (finalize is not null && finalize.Body.DescendantTokens().Any(x => x.IsKind(SyntaxKind.IdentifierToken) && x.ValueText == localName && localName != finalizeParameter))
            return null;

        string count, offset = null, collectionText = null;
        if (isFor)
        {
            var fromArgument = FindArgument(invocation, method, parameterIndex["fromInclusive"]);
            var toArgument = FindArgument(invocation, method, parameterIndex["toExclusive"]);
            if (fromArgument is null || toArgument is null || semanticModel.GetConstantValue(fromArgument.Expression, cancellationToken) is not { HasValue: true, Value: int from })
                return null;
            count = from == 0 ? toArgument.Expression.ToString() : $"{toArgument.Expression} - {from}";
            offset = from == 0 ? null : $" + {from}";
        }
        else
        {
            // The collection is read twice: for its count and in the state
            var collectionArgument = FindArgument(invocation, method, parameterIndex["collection"]);
            if (collectionArgument is null || collectionArgument.Expression.DescendantNodesAndSelf().Any(x => x is InvocationExpressionSyntax or ObjectCreationExpressionSyntax or ImplicitObjectCreationExpressionSyntax or ElementAccessExpressionSyntax))
                return null;
            collectionText = collectionArgument.Expression.ToString();
            count = $"{collectionText}.Count";
        }

        LambdaExpressionSyntax[] functions = finalize is null ? [initialize, action] : [initialize, action, finalize];
        if (CollectCaptures(semanticModel, functions, cancellationToken) is not { } captures)
            return null;

        var used = functions.SelectMany(x => x.DescendantTokens()).Where(x => x.IsKind(SyntaxKind.IdentifierToken)).Select(x => x.ValueText).Append(itemName).Append(localName).ToArray();
        var stateName = UniqueName(semanticModel, invocation, "state", used);
        var startName = UniqueName(semanticModel, invocation, "start", used);
        var endName = UniqueName(semanticModel, invocation, "endExclusive", used);
        var indexName = isFor ? itemName : UniqueName(semanticModel, invocation, "index", used);
        var collectionName = isFor ? null : UniqueName(semanticModel, invocation, "collection", captures.Variables.Select(x => x.Name).ToArray());

        var replacements = new List<TextChange>();
        var state = BuildState(captures, stateName, collectionText is null ? null : (collectionName, collectionText), replacements);
        var collectionAccess = collectionText is null ? null : captures.Count == 0 ? stateName : $"{stateName}.{collectionName}";
        foreach (var returnStatement in returns)
            replacements.Add(new TextChange(returnStatement.Span, "continue;"));
        if (finalize is not null && finalizeParameter != localName)
        {
            foreach (var identifier in finalize.Body.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>())
            {
                if (identifier.Identifier.ValueText == finalizeParameter && semanticModel.GetSymbolInfo(identifier, cancellationToken).Symbol is IParameterSymbol)
                    replacements.Add(new TextChange(identifier.Span, localName));
            }
        }

        var receiver = invocation.Expression is MemberAccessExpressionSyntax memberAccess ? $"{memberAccess.Expression}." : "";
        var lineIndent = LeadingWhitespace(text.Lines.GetLineFromPosition(invocation.SpanStart).ToString());
        var unit = lineIndent.Contains('\t') ? "\t" : "    ";
        var lines = new List<string> { $"{receiver}ForBatched({count}, {state}, static ({stateName}, {startName}, {endName}) =>" };
        void Add(int level, string line) => lines.Add(lineIndent + string.Concat(Enumerable.Repeat(unit, level)) + line);
        void AddBody(int level, CSharpSyntaxNode node)
        {
            IEnumerable<SyntaxNode> statements = node is BlockSyntax block ? block.Statements : [node];
            foreach (var statement in statements)
            {
                var code = Reindented(text, statement, replacements, lineIndent + string.Concat(Enumerable.Repeat(unit, level)));
                Add(level, node is BlockSyntax ? code : code + ";");
            }
        }

        Add(0, "{");
        var level = 1;
        Add(level, $"var {localName} = {Reindented(text, initialValue, replacements, lineIndent + unit)};");
        if (finalize is not null)
        {
            Add(level, "try");
            Add(level++, "{");
        }
        Add(level, $"for (int {indexName} = {startName}{offset}; {indexName} < {endName}{offset}; {indexName}++)");
        Add(level, "{");
        if (!isFor)
            Add(level + 1, $"var {itemName} = {collectionAccess}[{indexName}];");
        AddBody(level + 1, action.Body);
        Add(level, "}");
        if (finalize is not null)
        {
            Add(--level, "}");
            Add(level, "finally");
            Add(level, "{");
            AddBody(level + 1, finalize.Body);
            Add(level, "}");
        }
        Add(0, "})");

        return new TextChange(invocation.Span, string.Join("\n", lines).Replace("\n", DetectNewLine(text)));

        static IEnumerable<ReturnStatementSyntax> FindOwnReturns(SyntaxNode body)
            => body.DescendantNodes(node => node is not (AnonymousFunctionExpressionSyntax or LocalFunctionStatementSyntax)).OfType<ReturnStatementSyntax>();
    }

    private static ExpressionSyntax GetReturnedExpression(ParenthesizedLambdaExpressionSyntax lambda) => lambda.Body switch
    {
        ExpressionSyntax expression => expression,
        BlockSyntax { Statements: [ReturnStatementSyntax { Expression: { } returned }] } => returned,
        _ => null,
    };

    private static string[] GetParameterNames(AnonymousFunctionExpressionSyntax function, string[] defaultNames)
    {
        if (function.AsyncKeyword != default || function.Modifiers.Any(SyntaxKind.StaticKeyword))
            return null;
        return function switch
        {
            SimpleLambdaExpressionSyntax simple => [simple.Parameter.Identifier.Text],
            ParenthesizedLambdaExpressionSyntax parenthesized => parenthesized.ParameterList.Parameters.Select(x => x.Identifier.Text).ToArray(),
            AnonymousMethodExpressionSyntax { ParameterList: { } parameterList } => parameterList.Parameters.Select(x => x.Identifier.Text).ToArray(),
            _ => defaultNames,
        };
    }

    /// <summary>The source of <paramref name="node"/>, with the <paramref name="replacements"/> that fall inside it.</summary>
    private static string Rewritten(SourceText text, SyntaxNode node, List<TextChange> replacements)
    {
        var inside = replacements.Where(x => node.Span.Contains(x.Span)).OrderBy(x => x.Span.Start).ToList();
        return text.GetSubText(node.Span).WithChanges(inside.Select(x => new TextChange(new TextSpan(x.Span.Start - node.SpanStart, x.Span.Length), x.NewText))).ToString();
    }

    /// <summary>
    /// The source of <paramref name="node"/> with its <paramref name="replacements"/>, its next lines moved from the node's
    /// column to <paramref name="indent"/>.
    /// </summary>
    private static string Reindented(SourceText text, SyntaxNode node, List<TextChange> replacements, string indent)
    {
        var line = text.Lines.GetLineFromPosition(node.SpanStart);
        var column = node.SpanStart - line.Start;
        var codeLines = Rewritten(text, node, replacements).Split('\n');
        for (var i = 1; i < codeLines.Length; i++)
        {
            var removed = Math.Min(column, LeadingWhitespace(codeLines[i]).Length);
            codeLines[i] = codeLines[i].Trim('\r').Length == 0 ? "" : indent + codeLines[i][removed..].TrimEnd('\r');
        }
        codeLines[0] = codeLines[0].TrimEnd('\r');
        return string.Join("\n", codeLines);
    }

    private static string LeadingWhitespace(string line) => line[..(line.Length - line.TrimStart(' ', '\t').Length)];

    private static string DetectNewLine(SourceText text) => text.ToString().Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";

    private sealed class Captures
    {
        public bool This;
        public readonly List<ISymbol> Variables = [];
        public readonly List<(SyntaxNode Node, ISymbol Variable, bool ImplicitThis)> Uses = [];
        public int Count => (This ? 1 : 0) + Variables.Count;
    }

    /// <summary>
    /// What <paramref name="functions"/> capture from outside, and where they use it; null when they can't move to a static
    /// lambda without changing what they do.
    /// </summary>
    private static Captures CollectCaptures(SemanticModel semanticModel, IEnumerable<AnonymousFunctionExpressionSyntax> functions, CancellationToken cancellationToken)
    {
        var captures = new Captures();
        foreach (var function in functions)
        {
            var dataFlow = semanticModel.AnalyzeDataFlow(function);
            if (dataFlow is null || !dataFlow.Succeeded)
                return null;

            var outside = dataFlow.CapturedInside.Where(x => !x.Locations.Any(location => function.Span.Contains(location.SourceSpan))).ToList();
            // A write to a captured variable would go to the state's copy
            if (outside.Any(x => x is not IParameterSymbol { IsThis: true } && dataFlow.WrittenInside.Contains(x)))
                return null;

            foreach (var variable in outside)
            {
                if (variable is IParameterSymbol { IsThis: true })
                {
                    captures.This = true;
                    continue;
                }

                var type = variable switch { ILocalSymbol local => local.Type, IParameterSymbol parameter => parameter.Type, _ => null };
                if (type is null || type.TypeKind is TypeKind.Pointer or TypeKind.FunctionPointer || type.IsRefLikeType
                    || variable is ILocalSymbol { IsRef: true } || variable.Name == "self")
                    return null;
                if (!captures.Variables.Contains(variable, SymbolEqualityComparer.Default))
                    captures.Variables.Add(variable);
            }
        }

        foreach (var function in functions)
        {
            foreach (var node in function.Body.DescendantNodesAndSelf())
            {
                switch (node)
                {
                    case BaseExpressionSyntax:
                        return null;
                    case ThisExpressionSyntax:
                        captures.Uses.Add((node, null, false));
                        break;
                    case IdentifierNameSyntax identifier:
                    {
                        var symbol = semanticModel.GetSymbolInfo(identifier, cancellationToken).Symbol;
                        if (symbol is null)
                            continue;

                        if (symbol is IMethodSymbol { MethodKind: MethodKind.LocalFunction, IsStatic: false } localFunction
                            && !localFunction.Locations.Any(location => function.Span.Contains(location.SourceSpan)))
                            return null;

                        if (captures.Variables.Contains(symbol, SymbolEqualityComparer.Default))
                        {
                            // A method on a mutable struct could change the state's copy
                            var type = symbol is ILocalSymbol local ? local.Type : ((IParameterSymbol)symbol).Type;
                            if (type.IsValueType && !type.IsReadOnly && type.SpecialType == SpecialType.None && type.TypeKind != TypeKind.Enum
                                && identifier.Parent is MemberAccessExpressionSyntax receiverAccess && receiverAccess.Expression == identifier)
                                return null;
                            captures.Uses.Add((identifier, symbol, false));
                        }
                        else if (captures.This && IsImplicitThisMember(identifier, symbol))
                        {
                            captures.Uses.Add((identifier, null, true));
                        }
                        break;
                    }
                }
            }
        }
        return captures;
    }

    private static bool IsImplicitThisMember(IdentifierNameSyntax identifier, ISymbol symbol)
    {
        if (symbol.IsStatic || symbol is not (IFieldSymbol or IPropertySymbol or IMethodSymbol { MethodKind: MethodKind.Ordinary } or IEventSymbol))
            return false;

        // A name with its own receiver, or the member of an object initializer
        return identifier.Parent switch
        {
            MemberAccessExpressionSyntax memberAccess => memberAccess.Name != identifier,
            MemberBindingExpressionSyntax => false,
            NameColonSyntax or NameEqualsSyntax => false,
            AssignmentExpressionSyntax { Parent: InitializerExpressionSyntax } assignment => assignment.Left != identifier,
            _ => !SyntaxFacts.IsInNamespaceOrTypeContext(identifier),
        };
    }

    /// <summary>
    /// The state argument for <paramref name="captures"/> (and an <paramref name="extra"/> named value), adding the edits that
    /// route their uses through <paramref name="stateName"/>.
    /// </summary>
    private static string BuildState(Captures captures, string stateName, (string Name, string Value)? extra, List<TextChange> changes)
    {
        var count = captures.Count + (extra is null ? 0 : 1);
        var single = count == 1;
        var thisAccess = single ? stateName : $"{stateName}.self";
        foreach (var (node, variable, implicitThis) in captures.Uses)
        {
            var replacement = variable is not null ? (single ? stateName : $"{stateName}.{variable.Name}")
                : implicitThis ? $"{thisAccess}.{node}"
                : thisAccess;
            changes.Add(new TextChange(node.Span, replacement));
        }

        if (count == 0)
            return "default(object)";
        if (single)
            return extra?.Value ?? (captures.This ? "this" : captures.Variables[0].Name);

        var elements = (captures.This ? ["self: this"] : Array.Empty<string>())
            .Concat(captures.Variables.Select(x => x.Name))
            .Concat(extra is { } named ? [$"{named.Name}: {named.Value}"] : []);
        return $"({string.Join(", ", elements)})";
    }

    private static string UniqueName(SemanticModel semanticModel, SyntaxNode scope, string name, string[] takenNames)
    {
        if (name == "_" && !takenNames.Contains("_"))
            return name;

        var usedInside = scope.DescendantTokens().Where(x => x.IsKind(SyntaxKind.IdentifierToken)).Select(x => x.ValueText).ToHashSet();
        var baseName = name == "_" ? "state" : name;
        var candidate = baseName;
        for (var i = 2; semanticModel.LookupSymbols(scope.SpanStart, name: candidate).Any() || usedInside.Contains(candidate) || takenNames.Contains(candidate); i++)
            candidate = $"{baseName}{i}";
        return candidate;
    }
}
