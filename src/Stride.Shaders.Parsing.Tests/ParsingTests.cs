
using Stride.Shaders.Parsing;
using Stride.Shaders.Parsing.Analysis;
namespace Stride.Shaders.Parsing.Tests;

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
    }

    [Theory]
    [MemberData(nameof(GetShaderFilePaths))]
    public void ParseFile(string path)
    {
        var text = MonoGamePreProcessor.OpenAndRun(path, []);
        var result = SDSLParser.Parse(text);
        Assert.True(result.Errors.Count == 0, path + string.Join("\n", result.Errors.Select(x => x.ToString())));
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