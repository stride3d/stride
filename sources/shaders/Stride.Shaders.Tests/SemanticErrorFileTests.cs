// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Diagnostics;

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

        Assert.Throws<InvalidOperationException>(() => loader.LoadExternalBuffer("ErrorInFile", [], out _, out _, out _));

        var file = $"{sourceDir.FullName}/ErrorInFile.sdsl";
        var error = Assert.Single(log.Messages, m => m.Type == LogMessageType.Error).Text;
        Assert.Equal($"{file}(4,52): case label has the same value (1) as a previous label of this switch (in `case 1:`)", error);
        var warning = Assert.Single(log.Messages, m => m.Type == LogMessageType.Warning).Text;
        Assert.StartsWith($"{file}(3,19): SDSL0220: register(c1) on Scale is ignored", warning);
    }
}
