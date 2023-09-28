using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Stride.Core.CompilerServices.Common;

namespace Stride.Core.CompilerServices.Analyzers;
[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal class STRDIAG008FixedFieldInStructs : DiagnosticAnalyzer
{
    public const string DiagnosticId = "STRDIAG008";
    private const string Title = "Invalid Struct Member";
    private const string MessageFormat = "Struct members with the 'fixed' Modifier are not allowed as a Serialization target on member '{0}'.";
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
        context.RegisterSymbolAction(AnalyzeField, SymbolKind.Field); 
    }

    private void AnalyzeField(SymbolAnalysisContext context)
    {
        var fieldSymbol = (IFieldSymbol)context.Symbol;
        var dataContractAttribute = WellKnownReferences.DataContractAttribute(context.Compilation);
        var dataMemberIgnore = WellKnownReferences.DataMemberIgnoreAttribute(context.Compilation);
        if (dataContractAttribute is null || dataMemberIgnore is null)
            return;
        var containingType = fieldSymbol.ContainingType;


        if (containingType is null)
            return;

        if(WellKnownReferences.HasDataContractAttribute(containingType)) 
        {
            if (fieldSymbol.DeclaredAccessibility == Accessibility.Public && fieldSymbol.IsFixedSizeBuffer && !WellKnownReferences.HasAttribute(fieldSymbol, dataMemberIgnore))
            {
                this.ReportDiagnostics(Rule, context, null, fieldSymbol);
            }
        }
    }

}
