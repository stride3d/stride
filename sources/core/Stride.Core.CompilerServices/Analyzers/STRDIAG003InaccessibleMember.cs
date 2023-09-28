using System.Collections.Immutable;

namespace Stride.Core.CompilerServices.Analyzers;
/// <summary>
/// An Analyzer which verfifys that the [DataMember] Attribute can't be put on fields/properties that don't have a public/internal Accessor.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class STRDIAG003InaccessibleMember : DiagnosticAnalyzer
{
    public const string DiagnosticId = "STRDIAG003";
    private const string Title = "Inaccessible Member";
    private const string MessageFormat = "The [Stride.Core.DataMemberAttribute] is invalid on the {1} Member '{0}'";
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
        context.RegisterSymbolAction(AnalyzeNode, SymbolKind.Field, SymbolKind.Property);
    }

    private void AnalyzeNode(SymbolAnalysisContext context)
    {
        var symbol = context.Symbol;
        var dataMemberAttribute = context.Compilation.GetTypeByMetadataName("Stride.Core.DataMemberAttribute");
        if (dataMemberAttribute is null)
            return;

        if (symbol.DeclaredAccessibility != Accessibility.Public &&
            symbol.DeclaredAccessibility != Accessibility.Internal)
        {
            this.ReportDiagnostics(Rule,context, dataMemberAttribute, symbol);
        }
    }
}
