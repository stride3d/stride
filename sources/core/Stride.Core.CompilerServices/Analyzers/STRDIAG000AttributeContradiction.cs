using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Stride.Core.CompilerServices.Common;

namespace Stride.Core.CompilerServices.Analyzers;
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class STRDIAG000AttributeContradiction : DiagnosticAnalyzer
{
    public const string DiagnosticId = "STRDIAG000";
    private const string Title = "Inaccessible Member";
    private const string MessageFormat = "There is an Attribute Contradiction on '{0}' Member. [DataMemberIgnoreAttribute] on a [DataMember] is not allowed. Except if it has also [DataMemberUpdatableAttribute] Attribute";
    private const string Category = "CompilerServices";

    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
        DiagnosticId,
        Title,
        MessageFormat,
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.RegisterCompilationStartAction(AnalyzeCompilationStart);
    }
    private static void AnalyzeCompilationStart(CompilationStartAnalysisContext context)
    {
        var dataMemberAttribute = WellKnownReferences.DataMemberAttribute(context.Compilation);
        var dataMemberIgnoreAttribute = WellKnownReferences.DataMemberIgnoreAttribute(context.Compilation);
        var dataMemberUpdatableAttribute = WellKnownReferences.DataMemberUpdatableAttribute (context.Compilation);
        if (dataMemberAttribute is null || dataMemberIgnoreAttribute is null)
            return;

        context.RegisterSymbolAction(symbolContext => AnalyzeSymbol(symbolContext, dataMemberAttribute, dataMemberIgnoreAttribute, dataMemberUpdatableAttribute), SymbolKind.Property, SymbolKind.Field);
    }
    private static void AnalyzeSymbol(SymbolAnalysisContext context,INamedTypeSymbol dataMemberAttribute, INamedTypeSymbol dataMemberIgnoreAttribute, INamedTypeSymbol? dataMemberUpdatableAttribute)
    {
        var symbol = context.Symbol;

        if(WellKnownReferences.HasAttribute(symbol, dataMemberAttribute) && WellKnownReferences.HasAttribute(symbol,dataMemberIgnoreAttribute))
        {
            if(dataMemberUpdatableAttribute is null || !WellKnownReferences.HasAttribute(symbol, dataMemberUpdatableAttribute))
            {
                DiagnosticsAnalyzerExtensions.ReportDiagnostics(Rule, context, dataMemberAttribute, symbol);
            }
        }        
    }
}