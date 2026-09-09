
using Stride.Shaders.Parsing;
using Stride.Shaders.Parsing.Analysis;
namespace Stride.Shaders.Parsers.Tests;

public class ParsingTests1
{
    public static IEnumerable<object[]> GetShaderFilePaths()
    {
        var files = Directory.GetFiles("assets/Stride/SDSL", "*.sdsl");
        foreach (var file in files)
        {
            yield return new object[] { file };
        }
        files = Directory.GetFiles("assets/Stride/SDFX", "*.sdfx");
        foreach (var file in files)
        {
            yield return new object[] { file };
        }
        files = Directory.GetFiles("assets/SDSL", "*.sdsl", SearchOption.AllDirectories);
        foreach (var file in files)
        {
            yield return new object[] { file };
        }
    }

    [Theory]
    [MemberData(nameof(GetShaderFilePaths))]
    public void ParseFile(string path)
    {
        var text = MonoGamePreProcessor.OpenAndRun(path, []);
        var result = SDSLParser.Parse(text);
        Assert.True(result.Errors.Count == 0, path + string.Join("\n", result.Errors.Select(x => x.ToString())));
    }

    // Regression test: a trailing-dot float literal (e.g. "1.") is valid per the HLSL grammar
    // (https://learn.microsoft.com/en-us/windows/win32/direct3dhlsl/dx-graphics-hlsl-appendix-grammar#floating-point-numbers),
    // but a garbage character directly following it (e.g. "1.q") should still be reported as a parse error.
    [Fact]
    public void InvalidCharacterAfterTrailingDotFloatLiteralStillErrors()
    {
        var text = "shader Foo { void Bar() { float i = 1.q; } };";
        var result = SDSLParser.Parse(text);
        Assert.True(result.Errors.Count > 0, "Expected a parse error for invalid float suffix '1.q' but got none. Errors: " + string.Join("\n", result.Errors.Select(x => x.ToString())));
    }

    // [Theory]
    // [MemberData(nameof(GetShaderFilePaths))]
    // public void AnalyseFile(string path)
    // {
    //     var text = MonoGamePreProcessor.OpenAndRun(path, []);
    //     var result = SDSLParser.Parse(text);
    //     Assert.True(result.Errors.Count == 0, path + string.Join("\n", result.Errors.Select(x => x.ToString())));
    // }
}
