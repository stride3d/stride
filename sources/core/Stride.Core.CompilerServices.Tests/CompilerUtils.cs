using System.Reflection;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Stride.Core.CompilerServices.Analyzers;
using System;
using System.Linq;
using System.Security.AccessControl;

namespace Stride.Core.CompilerServices.Tests
{
    public static class CompilerUtils
    {
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
                    MetadataReference.CreateFromFile(typeof(object).GetTypeInfo().Assembly.Location.Replace("Private.CoreLib", "Runtime")),
                    // System.Private.CoreLib.dll
                    MetadataReference.CreateFromFile(typeof(object).GetTypeInfo().Assembly.Location),
                    // Stride.Core.dll
                    MetadataReference.CreateFromFile(typeof(object).GetTypeInfo().Assembly.Location.Replace("System.Private.CoreLib", "Stride.Core")),
                },
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        public static DiagnosticAnalyzer[] AllAnalyzers => typeof(DiagnosticsAnalyzerHelper).Assembly.GetTypes()
            .Where(t => t.IsSubclassOf(typeof(DiagnosticAnalyzer)) && !t.IsAbstract)
            .Select(type => (DiagnosticAnalyzer)Activator.CreateInstance(type)).ToArray();

        public static Compilation CreateCompilation(string sourceCode)
        {
            // Create a syntax tree from the source code
            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(SourceText.From(sourceCode));

            // Create a compilation with your syntax tree and necessary references
            MetadataReference[] references = new[]
            {
                // System.Runtime.dll
                MetadataReference.CreateFromFile(typeof(object).GetTypeInfo().Assembly.Location.Replace("Private.CoreLib", "Runtime")),
                // System.Private.CoreLib.dll
                MetadataReference.CreateFromFile(typeof(object).GetTypeInfo().Assembly.Location),
                // Stride.Core.dll
                MetadataReference.CreateFromFile(typeof(object).GetTypeInfo().Assembly.Location.Replace("System.Private.CoreLib", "Stride.Core")),

            };

            CSharpCompilation compilation = CSharpCompilation.Create(
                "TestAssembly",
                syntaxTrees: new[] { syntaxTree },
                references: references,
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            return compilation;
        }

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
