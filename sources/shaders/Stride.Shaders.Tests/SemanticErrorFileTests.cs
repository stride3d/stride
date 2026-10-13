// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Diagnostics;
using Stride.Core.Storage;
using Stride.Shaders.Compilers.SDSL;

namespace Stride.Shaders.Parsers.Tests;

/// <summary>
/// Compile errors and warnings of a shader file start with file(line,col), the form build tools and editors link to.
/// </summary>
public class SemanticErrorFileTests : IDisposable
{
    private readonly DirectoryInfo sourceDir = Directory.CreateTempSubdirectory("stride-shader-error-file");

    public void Dispose()
    {
        sourceDir.Delete(recursive: true);
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void ErrorsAndWarningsNameTheFile()
    {
        File.WriteAllText(Path.Combine(sourceDir.FullName, "ErrorInFile.sdsl"), """
            shader ErrorInFile
            {
                float Scale : register(c1);
                void Pick(int t) { switch (t) { case 1: break; case 1: break; } }
            };
            """);
        var log = new LoggerResult();
        var loader = new ShaderLoader(sourceDir.FullName, "./assets/Stride/SDSL") { Log = log };

        Assert.ThrowsAny<InvalidOperationException>(() => loader.LoadExternalBuffer("ErrorInFile", [], out _, out _, out _));

        var file = Path.Combine(sourceDir.FullName, "ErrorInFile.sdsl");
        var error = Assert.Single(log.Messages, m => m.Type == LogMessageType.Error).Text;
        Assert.Equal($"{file}(4,52): case label has the same value (1) as a previous label of this switch (in `case 1:`)", error);
        var warning = Assert.Single(log.Messages, m => m.Type == LogMessageType.Warning).Text;
        Assert.StartsWith($"{file}(3,19): SDSL0220: register(c1) on Scale is ignored", warning);
    }

    private const string DuplicateCase = """
        shader ErrorInCode
        {
            void Pick(int t) { switch (t) { case 1: break; case 1: break; } }
        };
        """;

    // Code without a file, as an effect compiled from memory: the message stays on one line
    [Fact]
    public void MessagesWithoutAFileStartWithLineAndColumn()
    {
        var log = new LoggerResult();
        var loader = new ShaderLoader(sourceDir.FullName, "./assets/Stride/SDSL");

        Assert.False(new SDSLC(loader).Compile(null, DuplicateCase, ObjectId.Empty, [], log, out _, new() { RegisterInCache = false }));

        var error = Assert.Single(log.Messages, m => m.Type == LogMessageType.Error).Text;
        Assert.StartsWith("(3,52): case label has the same value (1) as a previous label of this switch", error);
        Assert.DoesNotContain('\n', error);
    }

    // A virtual path, as one of the asset database, is not a file on disk: it keeps its separators
    [Fact]
    public void VirtualPathsStayAsTheyAre()
    {
        var log = new LoggerResult();
        var loader = new ShaderLoader(sourceDir.FullName, "./assets/Stride/SDSL");

        Assert.False(new SDSLC(loader).Compile("/shaders/ErrorInCode.sdsl", DuplicateCase, ObjectId.Empty, [], log, out _, new() { RegisterInCache = false }));

        var error = Assert.Single(log.Messages, m => m.Type == LogMessageType.Error).Text;
        Assert.StartsWith("/shaders/ErrorInCode.sdsl(3,52): case label", error);
    }

    private const string BadStatement = """
        shader BadStatement
        {
            float4 F()
            {
                return float4(1, 0, 0, 1) + ;
            }
        };
        """;

    [Fact]
    public void ParseErrorNamesTheFileOnce()
    {
        File.WriteAllText(Path.Combine(sourceDir.FullName, "BadStatement.sdsl"), BadStatement);
        var log = new LoggerResult();
        var loader = new ShaderLoader(sourceDir.FullName, "./assets/Stride/SDSL") { Log = log };

        Assert.ThrowsAny<InvalidOperationException>(() => loader.LoadExternalBuffer("BadStatement", [], out _, out _, out _));

        var message = Assert.Single(log.Messages);
        Assert.Equal($"{Path.Combine(sourceDir.FullName, "BadStatement.sdsl")}(5,37): SDSL0041: Expected expression or semi colon (at `;`)", message.Text);
    }

    // Directives before the error must not move its line
    [Fact]
    public void ParseErrorLineCountsPreprocessorLines()
    {
        File.WriteAllText(Path.Combine(sourceDir.FullName, "BadAfterDirectives.sdsl"), """
            #define VALUE 1
            #ifdef VALUE
            // A comment
            shader BadAfterDirectives
            {
                float F() { return VALUE + ; }
            };
            #endif
            """);
        var log = new LoggerResult();
        var loader = new ShaderLoader(sourceDir.FullName, "./assets/Stride/SDSL") { Log = log };

        Assert.ThrowsAny<InvalidOperationException>(() => loader.LoadExternalBuffer("BadAfterDirectives", [], out _, out _, out _));

        Assert.StartsWith($"{Path.Combine(sourceDir.FullName, "BadAfterDirectives.sdsl")}(6,", Assert.Single(log.Messages).Text);
    }

    [Fact]
    public void ParseErrorInAParentIsLoggedOnce()
    {
        File.WriteAllText(Path.Combine(sourceDir.FullName, "BadStatement.sdsl"), BadStatement);
        File.WriteAllText(Path.Combine(sourceDir.FullName, "Child.sdsl"), "shader Child : BadStatement { };");
        var log = new LoggerResult();
        var loader = new ShaderLoader(sourceDir.FullName, "./assets/Stride/SDSL") { Log = log };

        Assert.ThrowsAny<InvalidOperationException>(() => loader.LoadExternalBuffer("Child", [], out _, out _, out _));

        Assert.StartsWith($"{Path.Combine(sourceDir.FullName, "BadStatement.sdsl")}(5,37): SDSL0041", Assert.Single(log.Messages).Text);
    }

    [Fact]
    public void ParseErrorInAMixinIsLoggedOnce()
    {
        File.WriteAllText(Path.Combine(sourceDir.FullName, "BadStatement.sdsl"), BadStatement);
        var log = new LoggerResult();
        var mixer = new ShaderMixer(new ShaderLoader(sourceDir.FullName, "./assets/Stride/SDSL"));

        Assert.False(mixer.MergeSDSL(new ShaderClassSource("BadStatement"), new ShaderMixer.Options(false), log, out _, out _, out _, out _));

        var message = Assert.Single(log.Messages);
        Assert.StartsWith($"{Path.Combine(sourceDir.FullName, "BadStatement.sdsl")}(5,37): SDSL0041", message.Text);
        Assert.Null(((LogMessage)message).Exception);
    }
}
