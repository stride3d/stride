using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using System.Runtime.Serialization;

namespace Stride.Core.StrideDiagnostics.StrideDiagnostics;
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ReadOnlyDataMemberAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "STRDIAG005";
    private const string Title = "ReadOnlyMemberIsReferenceType";
    private const string MessageFormat = "[DataMember] applied to read-only member '{0}' of type '{1}' is not allowed. Use a reference type instead.";
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
        context.RegisterSymbolAction(AnalyzeField, SymbolKind.Field);
    }
    private static void AnalyzeField(SymbolAnalysisContext context)
    {
        var symbol = context.Symbol;
        var dataMemberAttribute = context.Compilation.GetTypeByMetadataName("Stride.Core.DataMemberAttribute");

        if (symbol is IFieldSymbol fieldSymbol)
        {
            if (fieldSymbol.GetAttributes().Any(attr => attr.AttributeClass.Equals(dataMemberAttribute, SymbolEqualityComparer.Default)) &&
                fieldSymbol.IsReadOnly)
            {
                var fieldType = fieldSymbol.Type;
                if (fieldType is null)
                    return;
                if (fieldType.SpecialType == SpecialType.System_String || !fieldType.IsReferenceType)
                {
                    var diagnostic = Diagnostic.Create(Rule, fieldSymbol.Locations.First(), fieldSymbol.Name, fieldType);
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }
    }
    private static void AnalyzeProperty(SymbolAnalysisContext context)
    {
        var propertySymbol = (IPropertySymbol)context.Symbol;
        var dataMemberAttribute = context.Compilation.GetTypeByMetadataName("Stride.Core.DataMemberAttribute");

        if (propertySymbol.GetAttributes().Any(attr => attr.AttributeClass.Equals(dataMemberAttribute, SymbolEqualityComparer.Default)))
        {
            if (propertySymbol.GetMethod != null && propertySymbol.SetMethod == null)
            {
                var propertyType = propertySymbol.Type;
                if (propertyType == null)
                    return;
                if (propertyType.SpecialType == SpecialType.System_String || !propertyType.IsReferenceType)
                {
                    var diagnostic = Diagnostic.Create(Rule, propertySymbol.Locations.First(), propertySymbol.Name, propertyType);
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }
    }
}
