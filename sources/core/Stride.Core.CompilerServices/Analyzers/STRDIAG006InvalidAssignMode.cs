using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Diagnostics;

namespace Stride.Core.CompilerServices.Analyzers;
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class DataMemberModeAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "STRDIAG006";
    private const string Title = "Invalid Assign Mode";
    private const string MessageFormat = "Invalid [DataMember] mode '{0}' applied to member '{1}'. A public or internal setter is required for mode '{0}'.";
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

    private static void AnalyzeSymbol(SymbolAnalysisContext context)
    {
        var propertySymbol = (IPropertySymbol)context.Symbol;
        var dataMemberAttribute = context.Compilation.GetTypeByMetadataName("Stride.Core.DataMemberAttribute");
        var dataMemberMode = context.Compilation.GetTypeByMetadataName("Stride.Core.DataMemberMode");
        if (propertySymbol.GetAttributes().Any(attr => attr.AttributeClass.Equals(dataMemberAttribute, SymbolEqualityComparer.Default)))
        {
            var modeParamter = propertySymbol.GetAttributes().ToList().FirstOrDefault(attr => attr.AttributeClass.Equals(dataMemberAttribute, SymbolEqualityComparer.Default))?
                .ConstructorArguments.First(x => x.Type.Equals(dataMemberMode, SymbolEqualityComparer.Default));
            // 1 is the Enums Value of DataMemberMode for Assign
            if (modeParamter is not null && modeParamter.HasValue && (int)modeParamter.Value.Value == 1)
            {
                if (propertySymbol.GetMethod != null && propertySymbol.SetMethod == null)
                {
                    var diagnostic = Diagnostic.Create(Rule, propertySymbol.Locations.First(), "DataMemberMode.Assign", propertySymbol.Name);
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }
    }
}
