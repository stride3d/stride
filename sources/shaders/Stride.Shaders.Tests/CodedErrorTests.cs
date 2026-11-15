// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Diagnostics;
using Stride.Shaders.Compilers.SDSL;

namespace Stride.Shaders.Parsers.Tests;

/// <summary>
/// Errors that give a code and the file, line and column: an unknown type (SDSL0190), a missing parent shader (SDSL0191), an
/// assignment to a value (SDSL0103), a call that matches no method or intrinsic (SDSL0110, SDSL0109, SDSL0162), and a stream
/// read before any stage writes it (SDSL0194, found when the effect is mixed).
/// </summary>
public class CodedErrorTests : IDisposable
{
    private readonly DirectoryInfo sourceDir = Directory.CreateTempSubdirectory("stride-shader-coded-errors");

    public void Dispose()
    {
        sourceDir.Delete(recursive: true);
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void UnknownTypeHasCodeAndLocation()
    {
        // VL.Fuse's FuseCommonDraw: Sampler instead of SamplerState, in a parameter
        var message = CompileError("UnknownType", """
            shader UnknownType
            {
                float4 Fetch(Texture2D t, Sampler s, float2 uv) { return t.Sample(s, uv); }
            };
            """);
        Assert.Contains("UnknownType.sdsl(3,31): SDSL0190: Unknown type Sampler.", message);
    }

    [Fact]
    public void MissingParentShaderHasCodeAndLocation()
    {
        // VL.StandardLibs' BoxSDF inherits from a shader that isn't in the library
        Write("ShapeBase", "shader ShapeBase { float Size; };");
        var message = CompileError("MissingParent", """
            shader MissingParent : ShapeBase, HappyShape
            {
                float Twice() { return Size * 2; }
            };
            """);
        Assert.Contains("MissingParent.sdsl(1,35): SDSL0191: MissingParent inherits from HappyShape, but shader HappyShape can't be found.", message);
        Assert.Single(message.Split('\n'), line => line.Contains("SDSL"));
    }

    [Fact]
    public void AssignmentToAValueHasCodeAndLocation()
    {
        // StrideTerrainEditor's Terrain_Blend_Slope
        var message = CompileError("AssignToValue", """
            shader AssignToValue
            {
                float Blend()
                {
                    float amount = 0;
                    + amount = 1.0;
                    return amount;
                }
            };
            """);
        Assert.Contains("AssignToValue.sdsl(6,9): SDSL0103: + amount can't be assigned to: it is a value, not a variable.", message);
    }

    [Theory]
    [InlineData("return Missing(x);", "(6,16): SDSL0110: Use of undeclared identifier 'Missing'")]
    [InlineData("return lerp(x, 2);", "(6,16): SDSL0162: No overload of lerp(float, int) can take these arguments: lerp is an intrinsic: check the number and types of its arguments.")]
    [InlineData("return Values.Fetch(0).x;", "(6,23): SDSL0109: Could not resolve method Fetch in type Texture2D<float4>")]
    [InlineData("float y = 1; return y(x);", "(6,29): SDSL0162: No overload of y(float) can take these arguments: y is not a method.")]
    public void CallThatMatchesNothingHasCode(string body, string expected)
    {
        var message = CompileError("CallMatchesNothing", $$"""
            shader CallMatchesNothing
            {
                Texture2D Values;
                float Compute(float x)
                {
                    {{body}}
                }
            };
            """);
        Assert.Contains($"CallMatchesNothing.sdsl{expected}", message);
    }

    [Fact]
    public void StreamReadButNeverWrittenHasCodeAndLocation()
    {
        Write("StreamNeverWritten", """
            shader StreamNeverWritten
            {
                stream float4 Position : POSITION;
                stream float4 ShadingPosition : SV_Position;
                stream float4 ColorTarget : SV_Target0;
                stream float3 Normal;

                void VSMain()
                {
                    streams.ShadingPosition = streams.Position;
                }

                void PSMain()
                {
                    streams.ColorTarget = float4(streams.Normal, 1);
                }
            };
            """);
        var mixer = new ShaderMixer(new ShaderLoader(sourceDir.FullName, "./assets/Stride/SDSL"));
        var log = new LoggerResult();
        var exception = Assert.Throws<InvalidOperationException>(() => mixer.MergeSDSL(new ShaderClassSource("StreamNeverWritten"), new ShaderMixer.Options(true), log, out _, out _, out _, out _));
        Assert.Contains("StreamNeverWritten.sdsl(15,9): SDSL0194: Stream Normal is read by PSMain before any stage writes it, and it has no semantic", exception.Message);
    }

    private void Write(string name, string code)
        => File.WriteAllText(Path.Combine(sourceDir.FullName, $"{name}.sdsl"), code);

    private string CompileError(string name, string code)
    {
        Write(name, code);
        return Assert.Throws<InvalidOperationException>(() => new ShaderLoader(sourceDir.FullName, "./assets/Stride/SDSL").LoadExternalBuffer(name, [], out _, out _, out _)).Message;
    }
}
