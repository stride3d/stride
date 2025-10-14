using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Stride.Core.CompilerServices.Common;

namespace Stride.Core.CompilerServices.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class STRDIAG009InvalidDictionaryKey : DiagnosticAnalyzer
{
    public const string DiagnosticId = "STRDIAG009";
    private const string Title = "Invalid Dictionary Key";
    private const string MessageFormat = "The member '{0}' implements IDictionary<T,K> with an unsupported type for the key. Only primitive types ( like int,float,.. ) are supported or string or enums as the Dictionary Key in asset serialization. When used in other contexts the warning may not apply and can be suppressed.";
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
        var dataMemberAttribute = WellKnownReferences.DataMemberAttribute(context.Compilation);
        var dictionaryInterface = WellKnownReferences.IDictionary_generic(context.Compilation);
        if (dataMemberAttribute is null || dictionaryInterface is null)
        {
            return;
        }

        context.RegisterSymbolAction(symbolContext => AnalyzeProperty(symbolContext, dataMemberAttribute, dictionaryInterface), SymbolKind.Property);
        context.RegisterSymbolAction(symbolContext => AnalyzeField(symbolContext, dataMemberAttribute, dictionaryInterface), SymbolKind.Field);
    }

    private static void AnalyzeProperty(SymbolAnalysisContext context, INamedTypeSymbol dataMemberAttribute, INamedTypeSymbol dictionaryInterface)
    {
        var symbol = (IPropertySymbol)context.Symbol;
        if (!symbol.HasAttribute(dataMemberAttribute))
            return;
        var interfaces = symbol.Type.AllInterfaces;
        foreach ( var i in interfaces )
        {
            if(i.OriginalDefinition.Equals(dictionaryInterface,SymbolEqualityComparer.Default))
            {
                var types = i.TypeArguments;
                if (!(types[0].TypeKind == TypeKind.Enum || types[0].IsImmutableType()))
                {
                    Rule.ReportDiagnostics(context, symbol);
                }
            }
        }
    }

    private static void AnalyzeField(SymbolAnalysisContext context, INamedTypeSymbol dataMemberAttribute, INamedTypeSymbol dictionaryInterface)
    {
        var symbol = (IFieldSymbol)context.Symbol;
        if (!symbol.HasAttribute(dataMemberAttribute))
            return;
        var interfaces = symbol.Type.AllInterfaces;
        foreach (var i in interfaces)
        {
            if (i.OriginalDefinition.Equals(dictionaryInterface, SymbolEqualityComparer.Default))
            {
                var types = i.TypeArguments;
                if (!types[0].IsImmutableType())
                {
                    Rule.ReportDiagnostics(context, symbol);
                }
            }
        }
    }
}
