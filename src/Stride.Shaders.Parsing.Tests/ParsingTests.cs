namespace Stride.Shaders.Parsing.Tests;

public class ParsingTests1
{
    [Theory]
    [InlineData("assets/SDSL/Commented.sdsl")]
    public void Test1(string path)
    {
        var shader = File.ReadAllText(path);
        Assert.True(shader.Length > 0);
    }
    [Theory]
    [InlineData("assets/Stride/Commented.sdsl")]
    public void TestMacro(string path)
    {
        var shader = File.ReadAllText(path);
        Assert.True(shader.Length > 0);
    }

    [Fact]
    public void Test2()
    {
        Assert.True(true);
    }
}