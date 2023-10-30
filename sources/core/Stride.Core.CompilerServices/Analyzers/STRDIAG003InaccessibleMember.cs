using System.Collections.Immutable;
using Stride.Core.CompilerServices.Common;

namespace Stride.Core.CompilerServices.Analyzers;

/// <summary>
/// An Analyzer which verfifys that the [DataMember] Attribute can't be put on fields/properties that don't have a public/internal Accessor.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class STRDIAG003InaccessibleMember : DiagnosticAnalyzer
{
    public const string DiagnosticId = "STRDIAG003";
    private const string Title = "Inaccessible Member";
    private const string MessageFormat = "The member '{0}' with [DataMember] is not accesssible to the serializer. Only public/internal/internal protected visibility is supported, when the [DataMember] attribute is applied.";
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
        var dataMemberAttribute = WellKnownReferences.DataMemberAttribute(context.Compilation);
        if (dataMemberAttribute is null)
        {
            return;
        }

        context.RegisterSymbolAction(symbolContext => AnalyzeSymbol(symbolContext, dataMemberAttribute), SymbolKind.Property, SymbolKind.Field);
    }

    private static void AnalyzeSymbol(SymbolAnalysisContext context, INamedTypeSymbol dataMemberAttribute)
    {
        var symbol = context.Symbol;
        if (!symbol.HasAttribute(dataMemberAttribute))
            return;

        if (!symbol.IsVisibleToSerializer(dataMemberAttribute))
        {
            Rule.ReportDiagnostics(context, symbol);
        }
    }
}
