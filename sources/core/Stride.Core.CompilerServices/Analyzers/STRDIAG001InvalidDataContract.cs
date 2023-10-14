using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Stride.Core.CompilerServices.Common;

namespace Stride.Core.CompilerServices.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class STRDIAG001InvalidDataContract : DiagnosticAnalyzer
{
    public const string DiagnosticId = "STRDIAG001";
    private const string Title = "Invalid [DataContract] Attribute";
    private const string MessageFormat = "The [DataContract] is not valid for the type '{0}'. Expected is a public/internal Accessor.";
    private const string Category = DiagnosticCategory.Serialization;

    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
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

        if(!symbol.IsVisibleToSerializer(hasDataMemberAttribute: true))
            Rule.ReportDiagnostics(context, symbol);
    }
}
