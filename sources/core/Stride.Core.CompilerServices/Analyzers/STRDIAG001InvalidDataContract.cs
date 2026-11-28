using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Stride.Core.CompilerServices.Common;

namespace Stride.Core.CompilerServices.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class STRDIAG001InvalidDataContract : DiagnosticAnalyzer
{
    public const string DiagnosticId = "STRDIAG001";
    private const string Title = "Invalid [DataContract] attribute";
    private const string MessageFormat = "The [DataContract] attribute is not valid for type '{0}'. You have to change its accessor to public or internal.";
    private const string Category = DiagnosticCategory.Serialization;

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
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.RegisterCompilationStartAction(AnalyzeCompilationStart);
    }

    private static void AnalyzeCompilationStart(CompilationStartAnalysisContext context)
    {
        var dataContractAttribute = WellKnownReferences.DataContractAttribute(context.Compilation);

        if (dataContractAttribute is null)
            return;

        context.RegisterSymbolAction(symbolContext => AnalyzeSymbol(symbolContext, dataContractAttribute), SymbolKind.NamedType);
    }

    private static void AnalyzeSymbol(SymbolAnalysisContext context, INamedTypeSymbol dataContractAttribute)
    {
        var symbol = (INamedTypeSymbol)context.Symbol;

        if (!symbol.HasAttribute(dataContractAttribute))
            return;

        if (symbol.IsFileLocal)
            Rule.ReportDiagnostics(context, symbol);

        if (!symbol.IsVisibleToSerializer(hasDataMemberAttribute: true))
            Rule.ReportDiagnostics(context, symbol);
    }
}
