// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Shaders.Parsers.Tests;

/// <summary>
/// Wrong code that used to fail with an internal error gets a message that says what is wrong: an override with another
/// return type or other parameters than the inherited method (SDSL0175, SDSL0176), a method where not all code paths return
/// a value (SDSL0177, as DXC), and an undeclared identifier (SDSL0110) with no internal error after it.
/// </summary>
public class CompileMessageTests : IDisposable
{
    private readonly DirectoryInfo sourceDir = Directory.CreateTempSubdirectory("stride-shader-compile-messages");

    public void Dispose()
    {
        sourceDir.Delete(recursive: true);
        GC.SuppressFinalize(this);
    }

    private const string Parent = """
        shader ComputeValue3
        {
            abstract float3 Compute();
            float Scale(float x) { return x; }
        };
        """;

    [Fact]
    public void OverrideWithAnotherReturnTypeSaysSo()
    {
        // VL.StandardLibs' DivideFloat3
        Write("ComputeValue3", Parent);
        var message = CompileError("Divide3", """
            shader Divide3 : ComputeValue3
            {
                override float2 Compute() { return 1; }
            };
            """);
        Assert.Contains("SDSL0175: Divide3.Compute() returns float2, but the method it overrides, ComputeValue3.Compute(), returns float3. Use the same return type.", message);
        Assert.DoesNotContain("Sequence contains no elements", message);
    }

    [Fact]
    public void OverrideWithOtherParametersSaysSo()
    {
        Write("ComputeValue3", Parent);
        var message = CompileError("OverrideOtherParameters", """
            shader OverrideOtherParameters : ComputeValue3
            {
                override float3 Compute() { return 1; }
                override float Scale(int x) { return x; }
            };
            """);
        Assert.Contains("SDSL0176: OverrideOtherParameters.Scale(int) is marked 'override', but no inherited method has this name and these parameters. Inherited: ComputeValue3.Scale(float).", message);
    }

    [Theory]
    // VL.StandardLibs' ChannelKeying: FXC accepts a switch whose cases cover every value, DXC doesn't
    [InlineData("float4 Pick(uint t) { switch (t % 3) { case 0: return 1; case 1: return 2; case 2: return 3; } }", "Pick")]
    [InlineData("float4 Pick(bool b) { if (b) return 1; }", "Pick")]
    [InlineData("float Pick(int t) { switch (t) { case 0: return 1; default: break; } }", "Pick")]
    public void MissingReturnSaysSo(string method, string name)
    {
        var message = CompileError("MissingReturn", $$"""
            shader MissingReturn
            {
                {{method}}
            };
            """);
        Assert.Contains($"SDSL0177: Not all code paths of MissingReturn.{name} return a value. Add a return at the end of the method (a switch needs a default that returns).", message);
    }

    [Theory]
    [InlineData("float4 Pick(uint t) { switch (t % 3) { case 0: return 1; case 1: return 2; default: return 3; } }")]
    [InlineData("float4 Pick(uint t) { switch (t) { case 0: case 1: return 1; default: { return 3; } } }")]
    [InlineData("float4 Pick(bool b) { if (b) return 1; else return 2; }")]
    [InlineData("float4 Pick(bool b) { if (b) return 1; else if (!b) return 2; else return 3; }")]
    public void EveryPathReturnsCompiles(string method)
    {
        Compile("EveryPathReturns", $$"""
            shader EveryPathReturns
            {
                {{method}}
            };
            """);
    }

    [Theory]
    // VL.OCIO's ColorGradeStage: a struct field used as a bare name
    [InlineData("float4 Grade(float4 c) { if (GradingSpace == 0) return c; return c * 2; }")]
    [InlineData("float4 Grade(float4 c) { return c * Missing.x + 1; }")]
    public void UndeclaredIdentifierIsTheOnlyError(string method)
    {
        var message = CompileError("UndeclaredName", $$"""
            shader UndeclaredName
            {
                {{method}}
            };
            """);
        Assert.Contains("SDSL0110: Use of undeclared identifier", message);
        Assert.DoesNotContain("GetElementType", message);
        Assert.Single(message.Split('\n'), line => line.Contains("SDSL"));
    }

    private void Write(string name, string code)
        => File.WriteAllText(Path.Combine(sourceDir.FullName, $"{name}.sdsl"), code);

    private void Compile(string name, string code)
    {
        Write(name, code);
        new ShaderLoader(sourceDir.FullName, "./assets/Stride/SDSL").LoadExternalBuffer(name, [], out _, out _, out _);
    }

    private string CompileError(string name, string code)
        => Assert.Throws<InvalidOperationException>(() => Compile(name, code)).Message;
}
