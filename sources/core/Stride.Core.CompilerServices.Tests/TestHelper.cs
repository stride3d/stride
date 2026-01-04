using Xunit;

namespace Stride.Core.CompilerServices.Tests;

/// <summary>
/// Provides helper methods for testing analyzer diagnostics.
/// </summary>
internal static class TestHelper
{
    /// <summary>
    /// Verifies that the provided source code does not produce any analyzer diagnostics.
    /// </summary>
    /// <param name="sourceCode">The C# source code to analyze.</param>
    public static async Task ExpectNoDiagnosticsAsync(string sourceCode)
    {
        var diagnostics = await CompilerUtils.CompileAndGetAnalyzerDiagnosticsAsync(sourceCode, CompilerUtils.AllAnalyzers);
        bool hasError = diagnostics.Any();
        Assert.False(hasError, $"The test is valid and shouldn't throw diagnostics. Thrown diagnostics: {string.Join(", ", diagnostics.Select(x => x.Id))}, SourceCode: \n{sourceCode}");
    }

    /// <summary>
    /// Verifies that the provided source code produces at least one diagnostic with the specified ID.
    /// </summary>
    /// <param name="sourceCode">The C# source code to analyze.</param>
    /// <param name="expectedDiagnosticId">The expected diagnostic ID.</param>
    public static async Task ExpectDiagnosticAsync(string sourceCode, string expectedDiagnosticId)
    {
        var diagnostics = await CompilerUtils.CompileAndGetAnalyzerDiagnosticsAsync(sourceCode, CompilerUtils.AllAnalyzers);
        bool hasError = diagnostics.Any(x => x.Id == expectedDiagnosticId);
        Assert.True(hasError, $"The test is invalid and should throw the '{expectedDiagnosticId}' diagnostic. Thrown diagnostics: {string.Join(", ", diagnostics.Select(x => x.Id))}, SourceCode: \n{sourceCode}");
    }
}
