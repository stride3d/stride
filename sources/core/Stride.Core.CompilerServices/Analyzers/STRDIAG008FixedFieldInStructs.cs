using System.Collections.Immutable;
using Stride.Core.CompilerServices.Common;

namespace Stride.Core.CompilerServices.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class STRDIAG008FixedFieldInStructs : DiagnosticAnalyzer
{
    public const string DiagnosticId = "STRDIAG008";
    private const string Title = "Invalid Struct Member";
    private const string MessageFormat = "Struct members with the 'fixed' Modifier are not supported as a Serialization target on member '{0}'";
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
        var dataMemberAttribute = WellKnownReferences.DataMemberAttribute(context.Compilation);
        var dataMemberIgnoreAttribute = WellKnownReferences.DataMemberIgnoreAttribute(context.Compilation);
        if (dataContractAttribute is null || dataMemberIgnoreAttribute is null || dataMemberAttribute is null)
        {
            return;
        }

        context.RegisterSymbolAction(symbolContext => AnalyzeField(symbolContext, dataContractAttribute, dataMemberIgnoreAttribute, dataMemberAttribute), SymbolKind.Field);
    }

    private static void AnalyzeField(SymbolAnalysisContext context, INamedTypeSymbol dataContractAttribute, INamedTypeSymbol dataMemberIgnoreAttribute, INamedTypeSymbol dataMemberAttribute)
    {
        var fieldSymbol = (IFieldSymbol)context.Symbol;
        if (!fieldSymbol.IsVisibleToSerializer(dataMemberAttribute))
            return;
        var containingType = fieldSymbol.ContainingType;

        if (containingType.HasAttribute(dataContractAttribute))
        {
            if (fieldSymbol.IsFixedSizeBuffer && !fieldSymbol.HasAttribute(dataMemberIgnoreAttribute))
            {
                Rule.ReportDiagnostics(context, fieldSymbol);
            }
        }
    }
}
