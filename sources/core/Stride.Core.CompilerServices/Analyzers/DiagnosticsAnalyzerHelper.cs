
namespace Stride.Core.CompilerServices.Analyzers;

public static class DiagnosticsAnalyzerHelper
{
    public static void ReportDiagnostics(this DiagnosticDescriptor rule, SymbolAnalysisContext context, ISymbol symbol)
    {
        var identifier = symbol.Locations;
        foreach (var location in identifier)
        {
            var diagnostic = Diagnostic.Create(rule, location, symbol.Name);
            context.ReportDiagnostic(diagnostic);
        }
    }
}
