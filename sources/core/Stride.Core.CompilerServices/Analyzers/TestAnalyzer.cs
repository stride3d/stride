using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Stride.Core.CompilerServices.Analyzers
{
    // PLACEHOLDER ANALYZER
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class TestAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor testDescriptor = new DiagnosticDescriptor("TEST02", "Test", "This is a test", "Test", DiagnosticSeverity.Warning, true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create<DiagnosticDescriptor>(
            testDescriptor
        );

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSymbolAction(context =>
            {
                var symbol = context.Symbol;
                if (symbol.Name == "Test")
                {
                    context.ReportDiagnostic(Diagnostic.Create(testDescriptor, symbol.Locations[0]));
                }
            }, SymbolKind.NamedType);
        }
    }
}
