using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Stride.Core.CompilerServices.Tests;
internal class TestHelper
{
    public static void ExpectNoDiagnosticsErrors(string sourceCode)
    {
        var diagnostics = CompilerUtils.CompileAndGetAnalyzerDiagnostics(sourceCode, CompilerUtils.AllAnalyzers);
        bool hasError = diagnostics.Any();
        Assert.False(hasError, $"The Test is valid and shouldn't throw Diagnostics. Thrown Diagnostics: {string.Concat(diagnostics.Select(x => x.Id),",")}");
    }
    public static void ExpectDiagnosticsError(string sourceCode,string diagnosticID)
    {
        var diagnostics = CompilerUtils.CompileAndGetAnalyzerDiagnostics(sourceCode, CompilerUtils.AllAnalyzers);
        bool hasError = diagnostics.Any(x => x.Id == diagnosticID);
        Assert.True(hasError, $"The Test is invalid and should throw the '{diagnosticID}' Diagnostics. Thrown Diagnostics: {string.Concat(diagnostics.Select(x => x.Id), ",")}, SourceCode: \n{sourceCode}");
    }
}
