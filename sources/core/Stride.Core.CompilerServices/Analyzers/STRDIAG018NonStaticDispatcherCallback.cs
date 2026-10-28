// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Stride.Core.CompilerServices.Common;

namespace Stride.Core.CompilerServices.Analyzers;

// Warns when a Dispatcher callback is a lambda that is not static or an instance method, since creating that delegate allocates on each call.
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class STRDIAG018NonStaticDispatcherCallback : DiagnosticAnalyzer
{
    public const string DiagnosticId = "STRDIAG018";
    private const string Title = "Dispatcher callback allocates";
    private const string MessageFormat = "The callback passed to Dispatcher.{0} allocates on each call. Use a static lambda or a static method, and pass what it needs through the state parameter.";
    private const string Category = DiagnosticCategory.Build;

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        Title,
        MessageFormat,
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: string.Format(DiagnosticCategory.LinkFormat, DiagnosticId));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterCompilationStartAction(compilationContext =>
        {
            var dispatcher = compilationContext.Compilation.GetTypeByMetadataName("Stride.Core.Threading.Dispatcher");
            var obsolete = compilationContext.Compilation.GetTypeByMetadataName("System.ObsoleteAttribute");
            if (dispatcher is null)
                return;

            compilationContext.RegisterOperationAction(operationContext => AnalyzeInvocation(operationContext, dispatcher, obsolete), OperationKind.Invocation);
        });
    }

    private static void AnalyzeInvocation(OperationAnalysisContext context, INamedTypeSymbol dispatcher, INamedTypeSymbol? obsolete)
    {
        var invocation = (IInvocationOperation)context.Operation;
        var method = invocation.TargetMethod;
        if (!SymbolEqualityComparer.Default.Equals(method.ContainingType, dispatcher))
            return;

        // The obsolete overloads have no state parameter, and already warn
        if (method.GetAttributes().Any(attribute => SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, obsolete)))
            return;

        foreach (var argument in invocation.Arguments)
        {
            if (argument.Value is not IDelegateCreationOperation creation)
                continue;

            var allocates = creation.Target switch
            {
                IAnonymousFunctionOperation lambda => !IsStatic(lambda.Syntax),
                IMethodReferenceOperation methodReference => !methodReference.Method.IsStatic,
                _ => false,
            };
            if (allocates)
                context.ReportDiagnostic(Diagnostic.Create(Rule, argument.Value.Syntax.GetLocation(), method.Name));
        }
    }

    private static bool IsStatic(SyntaxNode syntax) => syntax is AnonymousFunctionExpressionSyntax function && function.Modifiers.Any(SyntaxKind.StaticKeyword);
}
