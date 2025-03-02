using System.Collections.Immutable;
using Stride.Core.CompilerServices.Common;

namespace Stride.Core.CompilerServices.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class STRDIAG004PropertyWithNoGetter : DiagnosticAnalyzer
{
    public const string DiagnosticId = "STRDIAG004";
    private const string Title = "Property with no Getter";
    private const string NonExistentGetterMessageFormat = "The property '{0}' with [DataMember] does not have a getter which is required for serialization";
    private const string InvalidAccessibilityOnGetterMessageFormat = "The property '{0}' with [DataMember] does not have an accessible getter which is required for serialization. A public/internal/internal protected getter is expected.";
    private const string Category = DiagnosticCategory.Serialization;

    private static DiagnosticDescriptor NonExistentGetterRule = new(
        DiagnosticId,
        Title,
        NonExistentGetterMessageFormat,
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: string.Format(DiagnosticCategory.LinkFormat, DiagnosticId));

    private static DiagnosticDescriptor InvalidAccesibilityRule = new(
        DiagnosticId,
        Title,
        InvalidAccessibilityOnGetterMessageFormat,
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: string.Format(DiagnosticCategory.LinkFormat, DiagnosticId));
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(NonExistentGetterRule, InvalidAccesibilityRule); } }

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

        context.RegisterSymbolAction(symbolContext => AnalyzeProperty(symbolContext, dataMemberAttribute), SymbolKind.Property);
    }

    private static void AnalyzeProperty(SymbolAnalysisContext context, INamedTypeSymbol dataMemberAttribute)
    {
        var propertySymbol = (IPropertySymbol)context.Symbol;

        if (!propertySymbol.HasAttribute(dataMemberAttribute))
            return;

        if (propertySymbol.GetMethod is null)
        {
            NonExistentGetterRule.ReportDiagnostics(context, propertySymbol);
        }
        else if (!propertySymbol.GetMethod.IsVisibleToSerializer(hasDataMemberAttribute: true))
        {
            InvalidAccesibilityRule.ReportDiagnostics(context, propertySymbol);
        }
    }
}
