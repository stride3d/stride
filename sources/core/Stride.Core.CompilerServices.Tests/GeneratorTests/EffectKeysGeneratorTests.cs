using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Stride.Core.CompilerServices.Generators;
using Xunit;

namespace Stride.Core.CompilerServices.Tests.GeneratorTests;

public class EffectKeysGeneratorTests
{
    private const string Source = """
        namespace Stride.Rendering
        {{
            [System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Struct)]
            public sealed class EffectKeysAttribute : System.Attribute {{ }}
            public abstract class ParameterKey {{ }}
            public sealed class ConcreteParameterKey : ParameterKey {{ }}
            public static class ParameterKeys
            {{
                public static void Merge(ParameterKey key, System.Type ownerType, string name) {{ }}
            }}
        }}
        namespace Test
        {{
            {0}
        }}
        """;

    [Fact]
    public void Names_Private_Key_In_Static_Class()
    {
        var generated = RunGenerator("""
            [Stride.Rendering.EffectKeys]
            public static class MyKeys
            {
                private static readonly Stride.Rendering.ConcreteParameterKey Key = new();
            }
            """);

        Assert.Contains("\"My.Key\"", generated);
    }

    [Fact]
    public void Names_Key_In_Private_Nested_Type()
    {
        var generated = RunGenerator("""
            public class Outer
            {
                [Stride.Rendering.EffectKeys]
                private static class InnerKeys
                {
                    public static readonly Stride.Rendering.ConcreteParameterKey Key = new();
                }
            }
            """);

        Assert.Contains("\"Inner.Key\"", generated);
    }

    [Fact]
    public void Names_Key_With_Private_Key_Type()
    {
        var generated = RunGenerator("""
            [Stride.Rendering.EffectKeys]
            public class MyKeys
            {
                private static readonly PrivateKey Key = new();

                private sealed class PrivateKey : Stride.Rendering.ParameterKey { }
            }
            """);

        Assert.Contains("\"My.Key\"", generated);
    }

    private static string RunGenerator(string code)
    {
        var compilation = CompilerUtils.CreateCompilation(string.Format(Source, code));
        GeneratorDriver driver = CSharpGeneratorDriver.Create(new EffectKeysGenerator());
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out _);

        var result = Assert.Single(driver.GetRunResult().Results);
        Assert.Null(result.Exception);
        var errors = outputCompilation.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.Empty(errors);
        return Assert.Single(result.GeneratedSources).SourceText.ToString();
    }
}
