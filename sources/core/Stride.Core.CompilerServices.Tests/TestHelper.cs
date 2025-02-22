using Xunit;

namespace Stride.Core.CompilerServices.Tests;

internal static class TestHelper
{
    public static async Task ExpectNoDiagnosticsErrorsAsync(string sourceCode)
    {
        var diagnostics = await CompilerUtils.CompileAndGetAnalyzerDiagnosticsAsync(sourceCode, CompilerUtils.AllAnalyzers);
        bool hasError = diagnostics.Any();
        Assert.False(hasError, $"The Test is valid and shouldn't throw Diagnostics. Thrown Diagnostics: {string.Join(",", diagnostics.Select(x => x.Id))}, SourceCode: \n{sourceCode}");
    }

    public static async Task ExpectDiagnosticsErrorAsync(string sourceCode, string diagnosticID)
    {
        var diagnostics = await CompilerUtils.CompileAndGetAnalyzerDiagnosticsAsync(sourceCode, CompilerUtils.AllAnalyzers);
        bool hasError = diagnostics.Any(x => x.Id == diagnosticID);
        Assert.True(hasError, $"The Test is invalid and should throw the '{diagnosticID}' Diagnostics. Thrown Diagnostics: {string.Join(",", diagnostics.Select(x => x.Id))}, SourceCode: \n{sourceCode}");
    }
}
