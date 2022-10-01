using Microsoft.CodeAnalysis;

namespace Stride.Core.CompilerServices
{
    internal class GeneratorContext
    {
        private readonly GeneratorExecutionContext context;

        public GeneratorContext(GeneratorExecutionContext context)
        {
            this.context = context;
            WellKnownReferences = new WellKnownReferences(Compilation);
        }

        public WellKnownReferences WellKnownReferences { get; }

        public Compilation Compilation => context.Compilation;

        public void ReportDiagnostic(Diagnostic diagnostic) => context.ReportDiagnostic(diagnostic);
    }
}
