// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Diagnostics;

namespace Stride.Shaders.Parsers.Tests;

/// <summary>
/// Mistakes reported when a shader class compiles, before any effect mixes it.
/// </summary>
public class ShaderDefinitionCheckTests : IDisposable
{
    private readonly DirectoryInfo sourceDir = Directory.CreateTempSubdirectory("stride-shader-definition");

    public void Dispose()
    {
        sourceDir.Delete(recursive: true);
        GC.SuppressFinalize(this);
    }

    private const string Contract = """
        namespace Stride.Shaders.Tests;

        shader CheckContract
        {
            float4 Compute() { return 0; }
        }
        """;

    [Fact]
    public void UnknownMethodThroughACompositionNamesTheCompositionType()
    {
        Write("CheckContract", Contract);
        Write("CheckRoot", """
            namespace Stride.Shaders.Tests;

            shader CheckRoot
            {
                compose CheckContract contract;
                float4 Shading() { return contract.Missing(); }
            }
            """);

        var (ok, log) = Compile("CheckRoot");
        Assert.False(ok);
        Assert.Contains("SDSL0109: Could not resolve method Missing in type CheckContract", log);
    }

    // Compiles one class alone (and the classes it uses), like the first effect that needs it would
    private (bool Ok, string Log) Compile(string name)
    {
        var log = new LoggerResult();
        var loader = new ShaderLoader(sourceDir.FullName, "./assets/Stride/SDSL") { Log = log };
        bool ok;
        try
        {
            ok = loader.LoadExternalBuffer(name, [], out _, out _, out _);
        }
        catch (InvalidOperationException)
        {
            ok = false;
        }
        return (ok, string.Join(Environment.NewLine, log.Messages.Select(m => $"{m.Type}: {m.Text}")));
    }

    private void Write(string name, string code)
        => File.WriteAllText(Path.Combine(sourceDir.FullName, $"{name}.sdsl"), code);
}
