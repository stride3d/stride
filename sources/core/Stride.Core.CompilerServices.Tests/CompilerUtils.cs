using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Stride.Core.CompilerServices.Analyzers;

namespace Stride.Core.CompilerServices.Tests;

/// <summary>
/// Provides utility methods for compiling code and running analyzers and generators.
/// </summary>
public static class CompilerUtils
{
    private static readonly string assembliesDirectory = Path.GetDirectoryName(typeof(object).GetTypeInfo().Assembly.Location)!;

    /// <summary>
    /// Gets all diagnostic analyzers from the Stride.Core.CompilerServices assembly.
    /// </summary>
    public static DiagnosticAnalyzer[] AllAnalyzers => typeof(DiagnosticsAnalyzerHelper).Assembly.GetTypes()
        .Where(t => t.IsSubclassOf(typeof(DiagnosticAnalyzer)) && !t.IsAbstract)
        .Select(type => (DiagnosticAnalyzer)Activator.CreateInstance(type)!)
        .ToArray();

    /// <summary>
    /// Runs compilation over <paramref name="source"/> and applies generator <typeparamref name="TGenerator"/> over it.
    /// </summary>
    /// <typeparam name="TGenerator">The type of source generator to apply.</typeparam>
    /// <param name="assemblyName">The name of the assembly to create.</param>
    /// <param name="source">The source code to compile.</param>
    /// <returns>A tuple containing the resulting compilation and any diagnostics generated.</returns>
    public static (Compilation Compilation, ImmutableArray<Diagnostic> Diagnostics) CompileWithGenerator<TGenerator>(string assemblyName, string source)
        where TGenerator : ISourceGenerator, new()
    {
        var generator = new TGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver.RunGeneratorsAndUpdateCompilation(CreateCompilation(assemblyName, source), out var compilation, out var diagnostics);
        return (compilation, diagnostics);
    }

    /// <summary>
    /// Creates a C# compilation from the given source code.
    /// </summary>
    /// <param name="sourceCode">The source code to compile.</param>
    /// <returns>A <see cref="Compilation"/> object.</returns>
    public static Compilation CreateCompilation(string sourceCode) => CreateCompilation("TestAssembly", sourceCode);

    /// <summary>
    /// Creates a C# compilation with the specified assembly name and source code.
    /// </summary>
    /// <param name="assemblyName">The name of the assembly.</param>
    /// <param name="source">The source code to compile.</param>
    /// <returns>A <see cref="CSharpCompilation"/> object.</returns>
    private static CSharpCompilation CreateCompilation(string assemblyName, string source)
        => CSharpCompilation.Create(assemblyName,
            [CSharpSyntaxTree.ParseText(source)],
            [
                // System.Runtime.dll
                MetadataReference.CreateFromFile($"{assembliesDirectory}/System.Runtime.dll"),
                // System.Private.CoreLib.dll
                MetadataReference.CreateFromFile($"{assembliesDirectory}/System.Private.CoreLib.dll"),
                // Stride.Core.dll
                MetadataReference.CreateFromFile(typeof(DataContractAttribute).GetTypeInfo().Assembly.Location),
            ],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

    /// <summary>
    /// Runs the specified analyzers on a compilation and returns the diagnostics they produce.
    /// </summary>
    /// <param name="compilation">The compilation to analyze.</param>
    /// <param name="analyzers">The analyzers to run.</param>
    /// <returns>An immutable array of diagnostics.</returns>
    public static async Task<ImmutableArray<Diagnostic>> GetAnalyzerDiagnosticsAsync(
        this Compilation compilation,
        params DiagnosticAnalyzer[] analyzers)
    {
        var compilationWithAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create(analyzers));
        return await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();
    }

    /// <summary>
    /// Compiles the given source code and runs the specified analyzers on it.
    /// </summary>
    /// <param name="sourceCode">The source code to compile.</param>
    /// <param name="analyzers">The analyzers to run.</param>
    /// <returns>An immutable array of diagnostics.</returns>
    public static Task<ImmutableArray<Diagnostic>> CompileAndGetAnalyzerDiagnosticsAsync(string sourceCode, params DiagnosticAnalyzer[] analyzers)
    {
        Compilation compilation = CreateCompilation(sourceCode);
        return GetAnalyzerDiagnosticsAsync(compilation, analyzers);
    }
}
