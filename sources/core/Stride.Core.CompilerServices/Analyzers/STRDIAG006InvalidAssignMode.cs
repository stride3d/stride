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
        context.RegisterCompilationStartAction(AnalyzeCompilationStart);
    }
    private static void AnalyzeCompilationStart(CompilationStartAnalysisContext context)
    {
        var dataMemberAttribute = WellKnownReferences.DataMemberAttribute(context.Compilation);
        var dataMemberMode = WellKnownReferences.DataMemberMode(context.Compilation);
        if (dataMemberAttribute is null || dataMemberMode is null)
        {
            return;
        }

        context.RegisterSymbolAction(symbolContext => AnalyzeSymbol(symbolContext, dataMemberAttribute, dataMemberMode), SymbolKind.Property);
    }
    private static void AnalyzeSymbol(SymbolAnalysisContext context,INamedTypeSymbol dataMemberAttribute,INamedTypeSymbol dataMemberMode)
    {
        var propertySymbol = (IPropertySymbol)context.Symbol;
        if (propertySymbol is null)
            return;

        if (!WellKnownReferences.HasAttribute(propertySymbol, dataMemberAttribute))
            return;

        var attributes = propertySymbol.GetAttributes();
        foreach (var attribute in attributes)
        {
            if (attribute.AttributeClass?.Equals(dataMemberAttribute, SymbolEqualityComparer.Default) ?? false)
            {
                var modeParameter = attribute.ConstructorArguments.FirstOrDefault(x => x.Type?.Equals(dataMemberMode, SymbolEqualityComparer.Default) ?? false);
                // 1 is the Enums Value of DataMemberMode for Assign
                try
                {
                    if ((int)modeParameter.Value == 1)
                    {
                        if (propertySymbol.GetMethod != null && propertySymbol.SetMethod == null)
                        {
                            DiagnosticsAnalyzerExtensions.ReportDiagnostics(Rule, context, dataMemberAttribute, propertySymbol);
                        }
                    }
                    break;
                }
                catch (Exception)
                {
                }

            }
        }
    }
}

