using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Stride.Core.StrideDiagnostics.Tests;
internal static class DIAGTestHelper
{
    // Helper method to create a compilation from source code
    public static Compilation CreateCompilation(string sourceCode)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
        var compilationOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
        var references = new[]
        {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location), // Reference to system assemblies
                MetadataReference.CreateFromFile(typeof(Stride.Core.DataMemberAttribute).Assembly.Location),
        };

        return CSharpCompilation.Create("TestCompilation", new[] { syntaxTree }, references, compilationOptions);
    }

    // Helper method to analyze a compilation using the given analyzer
    public static IEnumerable<Diagnostic> AnalyzeCompilation(Compilation compilation, DiagnosticAnalyzer analyzer)
    {
        var compilationWithAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create(analyzer));
        var diagnostics = compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync().Result;

        return diagnostics;
    }
}
