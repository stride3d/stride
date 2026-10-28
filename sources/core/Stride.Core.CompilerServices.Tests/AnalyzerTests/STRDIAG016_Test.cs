using Stride.Core.CompilerServices.Analyzers;
using Xunit;

namespace Stride.Core.CompilerServices.Tests.AnalyzerTests;

public class STRDIAG016_Test
{
    private const string Source = """
        namespace Stride.Rendering
        {{
            [System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Struct)]
            public sealed class EffectKeysAttribute : System.Attribute {{ }}
            public abstract class ParameterKey {{ }}
            public sealed class ConcreteParameterKey : ParameterKey {{ }}
        }}
        namespace Test
        {{
            {0}
            public class KeyHolder
            {{
                public static Stride.Rendering.ConcreteParameterKey Key;
            }}
        }}
        """;

    [Fact]
    public async Task Warning_When_Key_Field_Holder_Is_Not_Attributed()
    {
        await TestHelper.ExpectDiagnosticAsync(string.Format(Source, ""), STRDIAG016MissingEffectKeysAttribute.DiagnosticId);
    }

    [Fact]
    public async Task No_Warning_When_Key_Field_Holder_Is_Attributed()
    {
        var sourceCode = string.Format(Source, "[Stride.Rendering.EffectKeys]");
        await TestHelper.ExpectNoDiagnosticsAsync(sourceCode);
    }

    [Fact]
    public async Task No_Warning_When_Key_Field_Holder_Is_Generic()
    {
        var sourceCode = string.Format(Source, "");
        sourceCode = sourceCode.Replace("public class KeyHolder", "public class KeyHolder<T>");
        await TestHelper.ExpectNoDiagnosticsAsync(sourceCode);
    }

    [Fact]
    public async Task No_Warning_When_Static_Field_Does_Not_Derive_From_ParameterKey()
    {
        var sourceCode = string.Format(Source, "");
        sourceCode = sourceCode.Replace("public static Stride.Rendering.ConcreteParameterKey Key;", "public static string Key;");
        await TestHelper.ExpectNoDiagnosticsAsync(sourceCode);
    }

    [Fact]
    public async Task No_Warning_When_Key_Field_Is_Generated()
    {
        var sourceCode = string.Format(Source, "");
        sourceCode = sourceCode.Replace(
            "public static Stride.Rendering.ConcreteParameterKey Key;",
            "[System.CodeDom.Compiler.GeneratedCode(\"generator\", \"1.0\")]\n            public static Stride.Rendering.ConcreteParameterKey Key;");
        await TestHelper.ExpectNoDiagnosticsAsync(sourceCode);
    }
}
