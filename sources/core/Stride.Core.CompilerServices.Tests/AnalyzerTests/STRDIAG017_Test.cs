using Microsoft.CodeAnalysis;
using Stride.Core.CompilerServices.Analyzers;
using Xunit;

namespace Stride.Core.CompilerServices.Tests.AnalyzerTests;

public class STRDIAG017_Test
{
    private const string Source = """
        namespace Stride.Rendering
        {{
            [System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Struct)]
            public sealed class EffectKeysAttribute : System.Attribute {{ }}
        }}
        namespace Test
        {{
            {0}
        }}
        """;

    [Fact]
    public async Task Error_When_EffectKeysAttribute_Is_Applied_To_Generic_Type()
    {
        var sourceCode = string.Format(Source, "[Stride.Rendering.EffectKeys] public class KeyHolder<T> { }");
        var diagnostics = await CompilerUtils.CompileAndGetAnalyzerDiagnosticsAsync(sourceCode, CompilerUtils.AllAnalyzers);

        Assert.Contains(diagnostics, diagnostic =>
            diagnostic.Id == STRDIAG017EffectKeysAttributeOnGenericType.DiagnosticId
            && diagnostic.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public async Task Error_When_EffectKeysAttribute_Is_Applied_To_Nongeneric_Nested_Type_In_Generic_Type()
    {
        var sourceCode = string.Format(
            Source,
            "public class Outer<T> { [Stride.Rendering.EffectKeys] public class Inner { } }");
        var diagnostics = await CompilerUtils.CompileAndGetAnalyzerDiagnosticsAsync(sourceCode, CompilerUtils.AllAnalyzers);

        Assert.Contains(diagnostics, diagnostic =>
            diagnostic.Id == STRDIAG017EffectKeysAttributeOnGenericType.DiagnosticId
            && diagnostic.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public async Task No_Error_When_EffectKeysAttribute_Is_Applied_To_NonGeneric_Type()
    {
        var sourceCode = string.Format(Source, "[Stride.Rendering.EffectKeys] public class KeyHolder { }");
        await TestHelper.ExpectNoDiagnosticsAsync(sourceCode);
    }

    [Fact]
    public async Task No_Error_When_Generic_Type_Is_Not_Attributed()
    {
        var sourceCode = string.Format(Source, "public class KeyHolder<T> { }");
        await TestHelper.ExpectNoDiagnosticsAsync(sourceCode);
    }
}
