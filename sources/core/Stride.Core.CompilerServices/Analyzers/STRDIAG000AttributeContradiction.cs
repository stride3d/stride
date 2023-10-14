using System.Collections.Immutable;
using Stride.Core.CompilerServices.Common;

namespace Stride.Core.CompilerServices.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class STRDIAG000AttributeContradiction : DiagnosticAnalyzer
{
    public const string DiagnosticId = "STRDIAG000";
    private const string Title = "Inaccessible Member";
    private const string MessageFormat = "There is an Attribute Contradiction on '{0}' Member. [DataMemberIgnore] Attribute on a [DataMember] is not supported. Except if it has also [DataMemberUpdatable] Attribute.";
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
        var dataMemberIgnoreAttribute = WellKnownReferences.DataMemberIgnoreAttribute(context.Compilation);
        var dataMemberUpdatableAttribute = WellKnownReferences.DataMemberUpdatableAttribute(context.Compilation);
        if (dataMemberAttribute is null || dataMemberIgnoreAttribute is null)
            return;

        context.RegisterSymbolAction(symbolContext => AnalyzeSymbol(symbolContext, dataMemberAttribute, dataMemberIgnoreAttribute, dataMemberUpdatableAttribute), SymbolKind.Property, SymbolKind.Field);
    }

    /// <summary>
    /// Analyzes the Symbol for a Attribute Contradiction.
    /// An invalid combination would be [DataMember] with [DataMemberIgnore].
    /// This Combination gets valid if [DataMemberUpdatable] is also applied.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="dataMemberAttribute">The DataMember Attribute of the <see cref="Compilation"/></param>
    /// <param name="dataMemberIgnoreAttribute">The DataMemberIgnore Attribute of the  <see cref="Compilation"/></param>
    /// <param name="dataMemberUpdatableAttribute">The DataMemberUpdatable Attribute of the <see cref="Compilation"/>. It may be null when the target Project doesn't reference Stride.Engine as it's not located in Stride.Core which must be there</param>
    private static void AnalyzeSymbol(SymbolAnalysisContext context, INamedTypeSymbol dataMemberAttribute, INamedTypeSymbol dataMemberIgnoreAttribute, INamedTypeSymbol? dataMemberUpdatableAttribute)
    {
        var symbol = context.Symbol;

        if (symbol.HasAttribute(dataMemberAttribute) && symbol.HasAttribute(dataMemberIgnoreAttribute))
        {
            if (dataMemberUpdatableAttribute is null || !symbol.HasAttribute(dataMemberUpdatableAttribute))
            {
                Rule.ReportDiagnostics(context, symbol);
            }
        }
    }
}
