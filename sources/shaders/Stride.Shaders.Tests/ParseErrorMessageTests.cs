// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Shaders.Parsing;

namespace Stride.Shaders.Parsers.Tests;

/// <summary>
/// Code that FXC and Stride 4.3 also reject, but where the parse error now says why:
/// a reserved word as a name (SDSL0048) and code after the end of a shader (SDSL0049).
/// </summary>
public class ParseErrorMessageTests
{
    [Theory]
    // VL.Fuse's FuseMeshLibrarySampling: `triangle` is a geometry shader primitive type
    [InlineData("shader S { void F() { int triangle=1<2?3:4; } };", "triangle")]
    [InlineData("shader S { void F() { float4 texture = 0; } };", "texture")]
    [InlineData("shader S { void F() { int a, line[2]; } };", "line")]
    [InlineData("shader S { float point; };", "point")]
    public void ReservedWordAsANameSaysSo(string code, string word)
    {
        // The enclosing parsers add their general errors after it, as for any parse error
        var errors = SDSLParser.Parse(code).Errors;
        Assert.StartsWith($"SDSL0048: '{word}' is a reserved word and can't be a name.", errors[0].Message);
        Assert.DoesNotContain(errors, x => x.Message.StartsWith("SDSL0049"));
    }

    [Fact]
    public void NamesThatStartWithAReservedWordAreFine()
    {
        var result = SDSLParser.Parse("shader S { void F() { int triangleCount = 1; float pointSize; } float lineWidth; };");
        Assert.True(result.Errors.Count == 0, string.Join("\n", result.Errors.Select(x => x.ToString())));
    }

    // Only a declaration shape gets SDSL0048: a method named `point` keeps the general error
    [Fact]
    public void AReservedMethodNameKeepsTheGeneralError()
    {
        var errors = SDSLParser.Parse("shader S { float4 point(float4 a) { return a; } };").Errors;
        Assert.StartsWith("SDSL0001", errors[0].Message);
    }

    [Theory]
    // VL.Addons' AutoColor_Pass2: DX9 effect globals after the shader
    [InlineData("shader S { };\nfloat PW <float uimin=1.0;> = 15.0;")]
    [InlineData("namespace N;\nshader S { };\nfloat PW;")]
    [InlineData("namespace N { shader S { }; float PW; }")]
    public void CodeAfterTheShaderSaysSo(string code)
    {
        var error = SDSLParser.Parse(code).Errors[0];
        Assert.StartsWith("SDSL0049: Code after the end of shader S.", error.Message);
        Assert.EndsWith(": float", error.ToString());
    }

    [Fact]
    public void CodeAfterParamsKeepsTheGeneralError()
    {
        var errors = SDSLParser.Parse("params P { bool A; }\nfloat PW;").Errors;
        Assert.StartsWith("SDSL0001", errors[0].Message);
    }
}
