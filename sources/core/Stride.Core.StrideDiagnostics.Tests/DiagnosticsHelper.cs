using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Stride.Core.StrideDiagnostics.Tests;

internal static class DiagnosticsHelper
{

    public static IEnumerable<Diagnostic> GetDiagnostics(string sourceCode)
    {

        var compilation = CSharpCompilation.Create("test")
                    .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
                    .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
                    .AddReferences(MetadataReference.CreateFromFile(typeof(DataMemberAttribute).Assembly.Location))
                    .AddSyntaxTrees(CSharpSyntaxTree.ParseText(sourceCode));
        var sourceGenerator = new NexGenerator(); // Replace with your actual source generator type

        // Create a generator driver
        var generatorDriver = CSharpGeneratorDriver.Create(new[] { sourceGenerator });

        // Trigger the source generator on the compilation
        generatorDriver.RunGeneratorsAndUpdateCompilation(compilation, out var updatedCompilation, out var diagnostics);

        // Get the generated diagnostics
        IEnumerable<Diagnostic> generatedDiagnostics = diagnostics;
        return generatedDiagnostics;
    }
}
