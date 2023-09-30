namespace Stride.Core.CompilerServices.Generators
{
    [Generator]
    public class TestGenerator : ISourceGenerator
    {
        private static readonly DiagnosticDescriptor testDescriptor = new DiagnosticDescriptor("TEST01", "Test", "This is a test", "Test", DiagnosticSeverity.Info, true);
        public void Initialize(GeneratorInitializationContext context)
        {
        }

        public void Execute(GeneratorExecutionContext context)
        {
            context.ReportDiagnostic(Diagnostic.Create(testDescriptor, Location.None));
        }
    }
}
