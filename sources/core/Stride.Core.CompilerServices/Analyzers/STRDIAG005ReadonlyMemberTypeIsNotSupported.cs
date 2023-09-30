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
public class STRDIAG005ReadonlyMemberTypeIsNotSupported : DiagnosticAnalyzer
{
    public const string DiagnosticId = "STRDIAG005";
    private const string Title = "ReadOnlyMemberIsReferenceType";
    private const string MessageFormat = "The [DataMember] Attribute is applied to a read-only member '{0}' with a non supported type. Only mutable reference types are supported for read-only members.";
    private const string Category = DiagnosticCategory.Serialization;

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
        context.RegisterSymbolAction(symbolContext => AnalyzeField(symbolContext, dataMemberAttribute), SymbolKind.Field);
    }
    private static void AnalyzeField(SymbolAnalysisContext context, INamedTypeSymbol dataMemberAttribute)
    {
        var symbol = (IFieldSymbol)context.Symbol;
        if (symbol.DeclaredAccessibility != Accessibility.Public || symbol.DeclaredAccessibility != Accessibility.Internal)
            return;
        if (!symbol.HasAttribute(dataMemberAttribute) && symbol.IsReadOnly)
            return;

        var fieldType = symbol.Type;

        if (fieldType.SpecialType == SpecialType.System_String || !fieldType.IsReferenceType)
        {
            var diagnostic = Diagnostic.Create(Rule, symbol.Locations.First(), symbol.Name, fieldType);
            context.ReportDiagnostic(diagnostic);
        }
    }
    private static void AnalyzeProperty(SymbolAnalysisContext context, INamedTypeSymbol dataMemberAttribute)
    {
        var propertySymbol = (IPropertySymbol)context.Symbol;
        if (propertySymbol.DeclaredAccessibility != Accessibility.Public || propertySymbol.DeclaredAccessibility != Accessibility.Internal)
            return;

        if (!propertySymbol.HasAttribute(dataMemberAttribute))
            return;
        
        var propertyType = propertySymbol.Type;

        if (propertySymbol.GetMethod != null && (propertyType.SpecialType == SpecialType.System_String || !propertyType.IsReferenceType))
        {
            var setMethod = propertySymbol.SetMethod;
            if (setMethod is null || (setMethod.DeclaredAccessibility != Accessibility.Public && setMethod.DeclaredAccessibility != Accessibility.Internal))
            {
                DiagnosticsAnalyzerExtensions.ReportDiagnostics(Rule, context, dataMemberAttribute, propertySymbol);
            }
        }

    }
}
