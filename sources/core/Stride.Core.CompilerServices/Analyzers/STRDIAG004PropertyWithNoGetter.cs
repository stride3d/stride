using System.Collections.Immutable;

namespace Stride.Core.CompilerServices.Analyzers;
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class STRDIAG004PropertyWithNoGetter : DiagnosticAnalyzer
{
    public const string DiagnosticId = "STRDIAG004";
    private const string Title = "Property with no Getter";
    private const string MessageFormat = "The [DataMember] Attribute is applied to property '{0}' with an invalid getter accessibility level. Expected is a public/internal getter.";
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
        context.RegisterSymbolAction(AnalyzeProperty, SymbolKind.Property);
    }

    private void AnalyzeProperty(SymbolAnalysisContext context)
    {
        var propertySymbol = (IPropertySymbol)context.Symbol;
        var dataMemberAttribute = context.Compilation.GetTypeByMetadataName("Stride.Core.DataMemberAttribute");
        if (dataMemberAttribute is null)
            return;
        if (propertySymbol.GetAttributes().Any(attr => attr.AttributeClass.Equals(dataMemberAttribute, SymbolEqualityComparer.Default)))
        {
            if (propertySymbol.GetMethod != null)
            {
                var getterAccessibility = propertySymbol.GetMethod.DeclaredAccessibility;

                if (getterAccessibility != Accessibility.Public && getterAccessibility != Accessibility.Internal)
                {
                    this.ReportDiagnostics(Rule, context, dataMemberAttribute, propertySymbol);
                }
            }
            else
            {
                this.ReportDiagnostics(Rule, context, dataMemberAttribute, propertySymbol);
            }
        }
    }
}
