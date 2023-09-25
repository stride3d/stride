using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace StrideDiagnostics;
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class PropertyDataMemberAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "STRDIAG004";
    private const string Title = "MemberWithNoGetter";
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

    private static void AnalyzeProperty(SymbolAnalysisContext context)
    {
        var propertySymbol = (IPropertySymbol)context.Symbol;
        var dataMemberAttribute = context.Compilation.GetTypeByMetadataName("Stride.Core.DataMemberAttribute");

        if (propertySymbol.GetAttributes().Any(attr => attr.AttributeClass.Equals(dataMemberAttribute, SymbolEqualityComparer.Default)))
        {
            if (propertySymbol.GetMethod != null)
            {
                var getterAccessibility = propertySymbol.GetMethod.DeclaredAccessibility;

                if (getterAccessibility != Accessibility.Public && getterAccessibility != Accessibility.Internal)
                {
                    ReportDiagnostics(context, dataMemberAttribute, propertySymbol);
                }
            }
            else
            {
                ReportDiagnostics(context, dataMemberAttribute, propertySymbol);
            }
        }
    }
    private static void ReportDiagnostics(SymbolAnalysisContext context, INamedTypeSymbol dataMemberAttribute, IPropertySymbol symbol)
    {
        if (symbol.GetAttributes().Any(attr => attr.AttributeClass.Equals(dataMemberAttribute, SymbolEqualityComparer.Default)))
        {
            var identifier = symbol.Locations;
            foreach (var location in identifier)
            {
                var diagnostic = Diagnostic.Create(Rule, location, symbol.Name);
                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
