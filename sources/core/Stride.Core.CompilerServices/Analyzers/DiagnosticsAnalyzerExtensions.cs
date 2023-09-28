
namespace Stride.Core.CompilerServices.Analyzers;
public static class DiagnosticsAnalyzerExtensions
{
    public static void ReportDiagnostics(this DiagnosticAnalyzer analyzer, DiagnosticDescriptor rule, SymbolAnalysisContext context, INamedTypeSymbol dataMemberAttribute, ISymbol symbol)
    {
        var identifier = symbol.Locations;
        foreach (var location in identifier)
        {
            var diagnostic = Diagnostic.Create(rule, location, symbol.Name);
            context.ReportDiagnostic(diagnostic);
        }
    }
}
