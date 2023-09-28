using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Diagnostics;
using Stride.Core.CompilerServices.Common;

namespace Stride.Core.CompilerServices.Analyzers;
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class DataMemberModeAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "STRDIAG006";
    private const string Title = "Invalid Assign Mode";
    private const string MessageFormat = "Invalid DataMembermode for the specified [DataMember] member '{0}'. A public or internal setter is required for 'DataMemberMode.Assign'.";
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
        context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.Property);
    }

    private void AnalyzeSymbol(SymbolAnalysisContext context)
    {
        var propertySymbol = (IPropertySymbol)context.Symbol;
        var dataMemberAttribute = WellKnownReferences.DataMemberAttribute(context.Compilation);
        if (dataMemberAttribute is null)
            return;

        var dataMemberMode = WellKnownReferences.DataMemberMode(context.Compilation);
        if (dataMemberMode is null)
            return;

        if (!WellKnownReferences.HasAttribute(propertySymbol, dataMemberAttribute))
            return;

        var modeParamter = propertySymbol.GetAttributes().ToList().FirstOrDefault(attr => attr.AttributeClass?.Equals(dataMemberAttribute, SymbolEqualityComparer.Default) ?? false)?
            .ConstructorArguments.First(x => x.Type?.Equals(dataMemberMode, SymbolEqualityComparer.Default) ?? false);
        // 1 is the Enums Value of DataMemberMode for Assign
        if (modeParamter is not null && modeParamter.HasValue && (int)modeParamter.Value.Value == 1)
        {
            if (propertySymbol.GetMethod != null && propertySymbol.SetMethod == null)
            {
                this.ReportDiagnostics(Rule, context, dataMemberAttribute, propertySymbol);
            }
        }
    }
}
