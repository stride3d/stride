using System.Reflection;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using System.IO;

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
                    MetadataReference.CreateFromFile($"{assembliesDirectory}/Stride.Core.dll"),
                },
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }
}
