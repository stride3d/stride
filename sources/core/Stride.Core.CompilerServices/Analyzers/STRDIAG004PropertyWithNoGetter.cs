using System.Collections.Immutable;
using Stride.Core.CompilerServices.Common;

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
        var dataMemberAttribute = WellKnownReferences.DataMemberAttribute(context.Compilation);
        if (dataMemberAttribute is null)
            return;
        if(!WellKnownReferences.HasAttribute(propertySymbol, dataMemberAttribute)) 
            return;
        if(propertySymbol.DeclaredAccessibility != Accessibility.Public && propertySymbol.DeclaredAccessibility != Accessibility.Internal) 
            return;
        if (propertySymbol.GetMethod is null)
        {
            this.ReportDiagnostics(Rule, context, dataMemberAttribute, propertySymbol);
        }
        else
        {
            var getterAccessibility = propertySymbol.GetMethod.DeclaredAccessibility;

            if (getterAccessibility != Accessibility.Public && getterAccessibility != Accessibility.Internal)
            {
                this.ReportDiagnostics(Rule, context, dataMemberAttribute, propertySymbol);
            }
        }
    }
}
