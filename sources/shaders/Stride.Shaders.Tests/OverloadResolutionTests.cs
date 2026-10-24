// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Shaders.Parsers.Tests;

/// <summary>
/// Errors of calls to shader methods: argument count and types (SDSL0160, SDSL0161, SDSL0164), no matching overload (SDSL0162)
/// and ambiguous overloads (SDSL0163). The overloads chosen are checked by RenderTest <c>OverloadRanking</c>, the calls
/// that must keep compiling by ComputeTest <c>CSCallArgumentConversions</c>.
/// </summary>
public class OverloadResolutionTests
{
    private static string CompileErrors(string shaderName)
    {
        // Compile errors are thrown by the loader
        var loader = new ShaderLoader("./assets/SDSL/CompilerTests", "./assets/Stride/SDSL");
        var exception = Assert.Throws<InvalidOperationException>(() => loader.LoadExternalBuffer(shaderName, [], out _, out _, out _));

        // The error says what is wrong with the call: compilation stops before it can fail on the arguments
        Assert.DoesNotContain("Cast only work", exception.Message);
        Assert.DoesNotContain("ProcessSymbol was not called", exception.Message);
        return exception.Message;
    }

    [Fact]
    public void OverloadsByBufferElementType()
    {
        // Comparing two buffer types used to throw "Cast only work between numeral types"
        var loader = new ShaderLoader("./assets/SDSL/CompilerTests", "./assets/Stride/SDSL");
        loader.LoadExternalBuffer("BufferElementOverload", [], out _, out _, out _);
    }

    [Fact]
    public void CallThatDoesNotFitTheSignature()
    {
        var message = CompileErrors("CallArgumentErrors");

        Assert.Contains("SDSL0160: Score(float4, StructuredBuffer<float4>, uint) takes 3 arguments, but the call gives 2.", message);
        Assert.Contains("SDSL0160: Blend(float, float, float) takes 2 to 3 arguments, but the call gives 4.", message);
        Assert.Contains("SDSL0161: Argument 2 of the call to Score(float4, StructuredBuffer<float4>, uint) has type float4, which can't be converted to StructuredBuffer<float4>.", message);
        Assert.Contains("SDSL0164: Argument 1 of the call to Fill(float2) is written back (out or inout parameter), but corner.xx can't be written to.", message);
    }

    [Fact]
    public void NoOverloadOrAmbiguousOverloads()
    {
        var message = CompileErrors("OverloadErrors");

        // One element conversion each
        Assert.Contains("SDSL0163: The call Mix(float, float) is ambiguous: these overloads match equally well:\n  Mix(float, int)\n  Mix(int, float)\n", message);
        // One truncation each, of different sizes
        Assert.Contains("SDSL0163: The call Shrink(float3) is ambiguous: these overloads match equally well:\n  Shrink(float)\n  Shrink(float2)\n", message);
        Assert.Contains("SDSL0162: No overload of Pick(float3, float, float) can take these arguments:\n  Pick(float2, float): takes 2 arguments\n  Pick(float3): takes 1 argument", message);
    }
}
