using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Text;
using System.Xml.Linq;
using System.Linq;

namespace StrideDiagnostics;
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class PrivateDataMemberAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "STRDIAG003";
    private const string Title = "InaccessibleMember";
    private const string MessageFormat = "The [Stride.Core.DataMemberAttribute] is invalid on the {1} Member '{0}'";
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
        context.RegisterSymbolAction(AnalyzeNode, SymbolKind.Field, SymbolKind.Property);
    }

    private static void AnalyzeNode(SymbolAnalysisContext context)
    {
        var symbol = context.Symbol;
        var dataMemberAttribute = context.Compilation.GetTypeByMetadataName("Stride.Core.DataMemberAttribute");

        if (symbol.DeclaredAccessibility != Accessibility.Public &&
            symbol.DeclaredAccessibility != Accessibility.Internal)
        {
            ReportDiagnostics(context, dataMemberAttribute, symbol);
        }
    }

    private static void ReportDiagnostics(SymbolAnalysisContext context, INamedTypeSymbol dataMemberAttribute, ISymbol symbol)
    {
        if (symbol.GetAttributes().Any(attr => attr.AttributeClass.Equals(dataMemberAttribute, SymbolEqualityComparer.Default)))
        {
            var identifier = symbol.Locations;
            foreach (var location in identifier)
            {
                var diagnostic = Diagnostic.Create(Rule, location, symbol.Name, symbol.DeclaredAccessibility, symbol.Kind);
                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
