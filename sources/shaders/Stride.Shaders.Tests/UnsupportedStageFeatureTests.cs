// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Shaders.Parsers.Tests;

/// <summary>
/// Stage features that Stride shaders don't have get a coded error when the class compiles: geometry shader stream output
/// (SDSL0170), other unknown method attributes (SDSL0171) and a stream with an initial value (SDSL0172).
/// </summary>
public class UnsupportedStageFeatureTests : IDisposable
{
    private readonly DirectoryInfo sourceDir = Directory.CreateTempSubdirectory("stride-shader-stage-features");

    public void Dispose()
    {
        sourceDir.Delete(recursive: true);
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void StreamOutputIsAnError()
    {
        var message = CompileError("GSWithStreamOutput", """
            shader GSWithStreamOutput
            {
                stream float4 ShadingPosition : SV_Position;
                stream float3 Normal : NORMAL;

                [StreamOutput("SV_Position.xyz;NORMAL.xyz")]
                [maxvertexcount(3)]
                void GSMain(triangle Input input[3], inout TriangleStream<Output> triangleStream)
                {
                    streams = input[0];
                    triangleStream.Append(streams);
                }
            };
            """);
        Assert.Contains("SDSL0170: [StreamOutput] on GSMain is not supported", message);
    }

    [Fact]
    public void UnknownMethodAttributeIsAnError()
    {
        var message = CompileError("UnknownMethodAttribute", """
            shader UnknownMethodAttribute
            {
                [SomethingElse(2)]
                float Value() { return 1; }
            };
            """);
        Assert.Contains("SDSL0171: Attribute [SomethingElse] on method Value is not supported. Remove it.", message);
    }

    [Theory]
    [InlineData("stage stream int TessFactor = 16;", "TessFactor", "16")]
    [InlineData("stream float Size = 1.5f;", "Size", "1.5f")]
    [InlineData("patchstream float4 Edges = float4(1, 1, 1, 1);", "Edges", "float4(1, 1, 1, 1)")]
    public void StreamInitialValueIsAnError(string declaration, string name, string value)
    {
        // A class that declares the stream: the error comes before any effect uses it
        var message = CompileError("StreamWithInitialValue", $$"""
            shader StreamWithInitialValue
            {
                {{declaration}}
            };
            """);
        Assert.Contains($"SDSL0172: Stream {name} has an initial value ({value}), but a stream has none. Remove it, and assign the value in the stage that writes the stream first (streams.{name} = {value};).", message);
    }

    private string CompileError(string name, string code)
    {
        File.WriteAllText(Path.Combine(sourceDir.FullName, $"{name}.sdsl"), code);
        var loader = new ShaderLoader(sourceDir.FullName, "./assets/Stride/SDSL");
        return Assert.Throws<InvalidOperationException>(() => loader.LoadExternalBuffer(name, [], out _, out _, out _)).Message;
    }
}
