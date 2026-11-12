// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Diagnostics;
using Stride.Core.Storage;
using Stride.Shaders.Compilers.SDSL;

namespace Stride.Shaders.Parsers.Tests;

/// <summary>
/// Compile errors, warnings and infos of a shader file start with file(line,col), the form build tools and editors link to.
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

    private const string StageCallsNonStage = """
        shader InfoInFile
        {
            stage float Get() { return Scale(); }
            float Scale() { return 2; }
        };
        """;

    [Fact]
    public void InfosNameTheFile()
    {
        File.WriteAllText(Path.Combine(sourceDir.FullName, "InfoInFile.sdsl"), StageCallsNonStage);
        var log = new LoggerResult();
        var loader = new ShaderLoader(sourceDir.FullName, "./assets/Stride/SDSL") { Log = log };

        loader.LoadExternalBuffer("InfoInFile", [], out _, out _, out _);

        var info = Assert.Single(log.Messages, m => m.Type == LogMessageType.Info).Text;
        Assert.Equal($"{Path.Combine(sourceDir.FullName, "InfoInFile.sdsl")}(3,32): Stage method 'InfoInFile.Get' references non-stage method 'InfoInFile.Scale'. This will cause the shader to be fully imported at root level instead of stage-only when used in a composition. (in `Scale()`)", info);
    }

    // Code without a file, as an effect compiled from memory: the message stays on one line
    [Fact]
    public void MessagesWithoutAFileStartWithLineAndColumn()
    {
        var log = new LoggerResult();
        var loader = new ShaderLoader(sourceDir.FullName, "./assets/Stride/SDSL");

        Assert.True(new SDSLC(loader).Compile(null, StageCallsNonStage, ObjectId.Empty, [], log, out _, new() { RegisterInCache = false }));

        var info = Assert.Single(log.Messages, m => m.Type == LogMessageType.Info).Text;
        Assert.StartsWith("(3,32): Stage method 'InfoInFile.Get' references non-stage method 'InfoInFile.Scale'.", info);
        Assert.DoesNotContain('\n', info);
    }

    // A virtual path, as one of the asset database, is not a file on disk: it keeps its separators
    [Fact]
    public void VirtualPathsStayAsTheyAre()
    {
        var log = new LoggerResult();
        var loader = new ShaderLoader(sourceDir.FullName, "./assets/Stride/SDSL");

        Assert.True(new SDSLC(loader).Compile("/shaders/InfoInFile.sdsl", StageCallsNonStage, ObjectId.Empty, [], log, out _, new() { RegisterInCache = false }));

        var info = Assert.Single(log.Messages, m => m.Type == LogMessageType.Debug).Text;
        Assert.StartsWith("/shaders/InfoInFile.sdsl(3,32): Stage method 'InfoInFile.Get'", info);
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
