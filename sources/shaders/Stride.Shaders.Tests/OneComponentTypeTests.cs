// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Shaders.Parsing;
using Stride.Shaders.Tests;

namespace Stride.Shaders.Parsers.Tests;

/// <summary>
/// float1 and float1x1 are float (checked by RenderTest <c>OneComponentTypes</c>); a matrix with one row or one column
/// (float1x3, float3x1) is error SDSL0155 for now.
/// </summary>
public class OneComponentTypeTests
{
    [Fact]
    public void OneRowMatrixDeclarationIsAnError()
    {
        // Compile errors are thrown by the loader
        var loader = new ShaderLoader("./assets/SDSL/CompilerTests", "./assets/Stride/SDSL");
        var exception = Assert.Throws<InvalidOperationException>(() => loader.LoadExternalBuffer("OneRowMatrix", [], out _, out _, out _));

        Assert.Contains("OneRowMatrix.sdsl(7,9): ", exception.Message);
        Assert.Contains("SDSL0155: float1x3 (a matrix with one row or one column) is not supported yet. Use float3.", exception.Message);
    }

    [Theory]
    [InlineData("float3x1", "float3")]
    [InlineData("int1x2", "int2")]
    public void OneColumnMatrixConstructorIsAnError(string type, string vectorType)
    {
        var errors = SDSLParser.Parse($"shader S {{ float4 F() {{ return {type}(1, 2); }} }};").Errors;
        Assert.StartsWith($"SDSL0155: {type} (a matrix with one row or one column) is not supported yet. Use {vectorType}.", errors[0].Message);
    }
}
