using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Stride.Core.CompilerServices.Common;

namespace Stride.Core.CompilerServices.Analyzers;
[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal class STRDIAG007DataMemberOnDelegate : DiagnosticAnalyzer
{
    public const string DiagnosticId = "STRDIAG007";
    private const string Title = "Invalid DataMemberAttribute";
    private const string MessageFormat = "Invalid DataMembermode for the specified member '{0}'. A Delegate can't be a Delegate.";
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
        context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.Field);
    }
    private void AnalyzeSymbol(SymbolAnalysisContext context)
    {
        var fieldSymbol = (IFieldSymbol)context.Symbol;
        var dataMemberAttribute = WellKnownReferences.DataMemberAttribute(context.Compilation);
        if (dataMemberAttribute is null)
            return;

        if (!WellKnownReferences.HasAttribute(fieldSymbol, dataMemberAttribute))
            return;

        var fieldType = fieldSymbol.Type;

        if (fieldType is null)
            return;

        if (fieldType.SpecialType == SpecialType.System_Delegate)
        {
            this.ReportDiagnostics(Rule, context, dataMemberAttribute, fieldSymbol);
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

        if(propertyType.SpecialType == SpecialType.System_Delegate)
        {
            this.ReportDiagnostics(Rule, context, dataMemberAttribute, propertySymbol);
        }
    }
}
