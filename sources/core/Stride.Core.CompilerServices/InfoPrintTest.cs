using Microsoft.CodeAnalysis;

namespace Stride.Core.CompilerServices
{
    [Generator]
    public class InfoPrintTest : ISourceGenerator
    {
        private static readonly DiagnosticDescriptor testDescriptor = new DiagnosticDescriptor("TEST01", "Test", "This is a test", "Test", DiagnosticSeverity.Warning, true);
        public void Initialize(GeneratorInitializationContext context)
        {
        }

        public void Execute(GeneratorExecutionContext context)
        {
            context.ReportDiagnostic(Diagnostic.Create(testDescriptor, Location.None));
        }
    }
}
