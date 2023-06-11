using System.Reflection;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

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
    }
}
