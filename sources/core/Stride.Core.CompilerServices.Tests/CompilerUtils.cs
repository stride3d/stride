using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Stride.Core.CompilerServices.Analyzers;

namespace Stride.Core.CompilerServices.Tests
{
    public static class CompilerUtils
    {
        private static string assembliesDirectory = Path.GetDirectoryName(typeof(object).GetTypeInfo().Assembly.Location);

        /// <summary>
        /// Runs compilation over <paramref name="source"/> and applies generator <typeparamref name="TGenerator"/> over it.
        /// </summary>
        public static (Compilation Compilation, ImmutableArray<Diagnostic> Diagnostics) CompileWithGenerator<TGenerator>(string assemblyName, string source)
            where TGenerator : ISourceGenerator, new()
        {
            var generator = new TGenerator();
            GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
            driver.RunGeneratorsAndUpdateCompilation(CreateCompilation(assemblyName, source), out var compilation, out var diagnostics);
            return (compilation, diagnostics);
        }

        private static Compilation CreateCompilation(string assemblyName, string source)
            => CSharpCompilation.Create(assemblyName,
                new[] { CSharpSyntaxTree.ParseText(source) },
                new[]
                {
                    // System.Runtime.dll
                    MetadataReference.CreateFromFile($"{assembliesDirectory}/System.Runtime.dll"),
                    // System.Private.CoreLib.dll
                    MetadataReference.CreateFromFile($"{assembliesDirectory}/System.Private.CoreLib.dll"),
                    // Stride.Core.dll
                    MetadataReference.CreateFromFile(typeof(Stride.Core.DataContractAttribute).GetTypeInfo().Assembly.Location),
                },
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        public static DiagnosticAnalyzer[] AllAnalyzers => typeof(DiagnosticsAnalyzerHelper).Assembly.GetTypes()
            .Where(t => t.IsSubclassOf(typeof(DiagnosticAnalyzer)) && !t.IsAbstract)
            .Select(type => (DiagnosticAnalyzer)Activator.CreateInstance(type)).ToArray();

        public static Compilation CreateCompilation(string sourceCode) => CreateCompilation("TestAssembly", sourceCode);

        public static ImmutableArray<Diagnostic> GetAnalyzerDiagnostics(
            this Compilation compilation,
            params DiagnosticAnalyzer[] analyzers)
        {
            // Analyze the compilation and get diagnostics
            var compilationWithAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create(analyzers));
            var diagnostics = compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync().Result;

            return diagnostics;
        }

        public static ImmutableArray<Diagnostic> CompileAndGetAnalyzerDiagnostics(string sourceCode, params DiagnosticAnalyzer[] analyzers)
        {
            Compilation compilation = CreateCompilation(sourceCode);
            return GetAnalyzerDiagnostics(compilation, analyzers);
        }
    }
}
