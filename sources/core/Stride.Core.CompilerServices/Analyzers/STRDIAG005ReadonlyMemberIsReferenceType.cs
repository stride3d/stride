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
using Stride.Core.CompilerServices.Common;

namespace Stride.Core.CompilerServices.Analyzers;
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class STRDIAG005ReadonlyMemberIsReferenceType : DiagnosticAnalyzer
{
    public const string DiagnosticId = "STRDIAG005";
    private const string Title = "ReadOnlyMemberIsReferenceType";
    private const string MessageFormat = "The [DataMember] Attribute is applied to a read-only member '{0}' with a non allowed Type. Use a reference type instead, string isn't allowed either.";
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
        var dataMemberAttribute = WellKnownReferences.DataMemberAttribute(context.Compilation);
        if (dataMemberAttribute is null)
            return;

        if (symbol is IFieldSymbol fieldSymbol)
        {
            if (fieldSymbol.GetAttributes().Any(attr => attr.AttributeClass?.Equals(dataMemberAttribute, SymbolEqualityComparer.Default) ?? false) &&
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
    private void AnalyzeProperty(SymbolAnalysisContext context)
    {
        var propertySymbol = (IPropertySymbol)context.Symbol;
        var dataMemberAttribute = WellKnownReferences.DataMemberAttribute(context.Compilation);
        if (dataMemberAttribute is null)
            return;
        if (!WellKnownReferences.HasAttribute(propertySymbol, dataMemberAttribute))
            return;

        var propertyType = propertySymbol.Type;
        if (propertyType is null)
            return;

        if (propertySymbol.GetMethod != null && (propertyType.SpecialType == SpecialType.System_String || !propertyType.IsReferenceType))
        {
            var setMethod = propertySymbol.SetMethod;
            if (setMethod is null || (setMethod.DeclaredAccessibility != Accessibility.Public && setMethod.DeclaredAccessibility != Accessibility.Internal))
            {
                this.ReportDiagnostics(Rule, context, dataMemberAttribute, propertySymbol);
            }
        }

    }
}