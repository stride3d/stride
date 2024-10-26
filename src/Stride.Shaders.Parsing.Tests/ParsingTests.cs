
using Stride.Shaders.Parsing;
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
    }

    [Theory]
    [MemberData(nameof(GetShaderFilePaths))]
    public void TestAllFiles(string path)
    {
        var text = MonoGamePreProcessor.Run(path, []);
        var result = SDSLParser.Parse(text);
        Assert.True(result.Errors.Count == 0, path + string.Join("\n", result.Errors.Select(x => x.ToString())));
    }
    // [Theory]
    // [InlineData("assets/SDSL/Commented.sdsl")]
    // public void Test1(string path)
    // {
    //     var shader = File.ReadAllText(path);
    //     Assert.True(shader.Length > 0);
    // }
    // [Theory]
    // [InlineData("assets/Stride/Commented.sdsl")]
    // public void TestMacro(string path)
    // {
    //     var shader = File.ReadAllText(path);
    //     Assert.True(shader.Length > 0);
    // }

    // [Fact]
    // public void Test2()
    // {
    //     Assert.True(true);
    // }
}