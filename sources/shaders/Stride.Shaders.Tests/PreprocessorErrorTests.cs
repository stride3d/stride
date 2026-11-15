// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Diagnostics;
using Stride.Shaders.Compilers.SDSL;
using Stride.Shaders.Parsing;

namespace Stride.Shaders.Parsers.Tests;

/// <summary>
/// Preprocessor errors and warnings start with file(line,col), the form build tools and editors link to.
/// </summary>
public class PreprocessorErrorTests : IDisposable
{
    private readonly DirectoryInfo sourceDir = Directory.CreateTempSubdirectory("stride-preprocessor-error");

    public void Dispose()
    {
        sourceDir.Delete(recursive: true);
        GC.SuppressFinalize(this);
    }

    [Theory]
    [InlineData("shader S { };\n\n#error bad thing\n", "S.sdsl(3,2): #error bad thing")]
    [InlineData("shader S { };\r\n\r\n#error bad thing\r\n", "S.sdsl(3,2): #error bad thing")]
    [InlineData("#define VALUE 1\n#ifdef VALUE\n\n// A comment\nshader S { float F() { return VALUE + 1; } };\n#error bad thing\n#endif\n", "S.sdsl(6,2): #error bad thing")]
    [InlineData("#define VALUE 1\r\n#ifdef VALUE\r\n\r\n// A comment\r\nshader S { float F() { return VALUE + 1; } };\r\n#error bad thing\r\n#endif\r\n", "S.sdsl(6,2): #error bad thing")]
    [InlineData("#define VALUE 1 \\\n  + 2\n#error bad thing\n", "S.sdsl(3,2): #error bad thing")]
    [InlineData("\n  #foo\n", "S.sdsl(2,4): Unknown preprocessor directive foo")]
    [InlineData("shader S { };\n#endif\n", "S.sdsl(2,2): #endif without #if")]
    [InlineData("\n\n#if 1 / 0\n#endif\n", "S.sdsl(3,7): Division by zero")]
    public void ErrorsNameTheFileAndLine(string code, string expected)
    {
        var exception = Assert.Throws<ShaderPreprocessorException>(() => MonoGamePreProcessor.Run(code, "S.sdsl"));

        Assert.Equal(expected, exception.Message);
    }

    [Theory]
    [InlineData("#ifdef VALUE foo\nshader S { };\n#endif\n", "S.sdsl(1,14): Unexpected nonwhite token")]
    [InlineData("shader S { };\n#warning look here\n", "S.sdsl(2,2): #warning look here")]
    [InlineData("shader S { };\n#pragma unknown\n", "S.sdsl(2,9): Unknown #pragma: unknown")]
    public void WarningsNameTheFileAndLineAndCodeStillRuns(string code, string expected)
    {
        var warnings = new List<string>();

        var text = MonoGamePreProcessor.Run(code, "S.sdsl", warnings.Add, ("VALUE", "1"));

        Assert.Equal(expected, Assert.Single(warnings));
        Assert.Contains("shader S { };", text);
    }

    [Fact]
    public void WarningsAreOptional()
    {
        Assert.Contains("shader S { };", MonoGamePreProcessor.Run("shader S { };\n#warning look here\n", "S.sdsl"));
    }

    [Fact]
    public void ErrorIsLoggedOnceWithTheFile()
    {
        File.WriteAllText(Path.Combine(sourceDir.FullName, "Broken.sdsl"), "shader Broken { };\n#error bad thing\n");
        File.WriteAllText(Path.Combine(sourceDir.FullName, "Child.sdsl"), "shader Child : Broken { };");
        var log = new LoggerResult();
        var mixer = new ShaderMixer(new ShaderLoader(sourceDir.FullName, "./assets/Stride/SDSL"));

        Assert.False(mixer.MergeSDSL(new ShaderClassSource("Child"), new ShaderMixer.Options(false), log, out _, out _, out _, out _));

        var message = Assert.Single(log.Messages);
        Assert.Equal($"{Path.Combine(sourceDir.FullName, "Broken.sdsl")}(2,2): #error bad thing", message.Text);
        Assert.Null(((LogMessage)message).Exception);
    }

    [Fact]
    public void WarningIsLoggedAndTheShaderCompiles()
    {
        File.WriteAllText(Path.Combine(sourceDir.FullName, "Warned.sdsl"), "#warning look here\nshader Warned { };\n");
        var log = new LoggerResult();
        var loader = new ShaderLoader(sourceDir.FullName, "./assets/Stride/SDSL") { Log = log };

        Assert.True(loader.LoadExternalBuffer("Warned", [], out _, out _, out _));

        var message = Assert.Single(log.Messages);
        Assert.Equal(LogMessageType.Warning, message.Type);
        Assert.Equal($"{Path.Combine(sourceDir.FullName, "Warned.sdsl")}(1,2): #warning look here", message.Text);
    }
}
