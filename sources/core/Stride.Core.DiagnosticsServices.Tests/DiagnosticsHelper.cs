using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Stride.Core;
using StrideDiagnostics;

namespace StrideDiagnosticsTests;

internal static class DiagnosticsHelper
{

    public static IEnumerable<Diagnostic> GetDiagnostics(string sourceCode)
    {

        CSharpCompilation compilation = CSharpCompilation.Create("test")
                    .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
                    .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
                    .AddReferences(MetadataReference.CreateFromFile(typeof(DataMemberAttribute).Assembly.Location))
                    .AddSyntaxTrees(CSharpSyntaxTree.ParseText(sourceCode));
        StrideDiagnosticsGenerator sourceGenerator = new StrideDiagnosticsGenerator(); // Replace with your actual source generator type

        // Create a generator driver
        CSharpGeneratorDriver generatorDriver = CSharpGeneratorDriver.Create(new[] { sourceGenerator });

        // Trigger the source generator on the compilation
        generatorDriver.RunGeneratorsAndUpdateCompilation(compilation, out Compilation? updatedCompilation, out System.Collections.Immutable.ImmutableArray<Diagnostic> diagnostics);

        // Get the generated diagnostics
        IEnumerable<Diagnostic> generatedDiagnostics = diagnostics;
        return generatedDiagnostics;
    }
}
