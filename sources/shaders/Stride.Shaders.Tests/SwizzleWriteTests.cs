// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Shaders.Parsers.Tests;

/// <summary>
/// A written swizzle can't name an element twice (FXC's X3025): error SDSL0145, for matrix and vector swizzles.
/// The values written through matrix swizzles are checked by RenderTest <c>MatrixSwizzleWrite</c>.
/// </summary>
public class SwizzleWriteTests
{
    [Fact]
    public void SameElementTwiceIsAnError()
    {
        // Compile errors are thrown by the loader
        var loader = new ShaderLoader("./assets/SDSL/CompilerTests", "./assets/Stride/SDSL");
        var exception = Assert.Throws<InvalidOperationException>(() => loader.LoadExternalBuffer("SwizzleWritesTwice", [], out _, out _, out _));

        Assert.Contains("SDSL0145: Swizzle _m00_m00 writes the same element twice.", exception.Message);
        Assert.Contains("SDSL0145: Swizzle xx writes the same element twice.", exception.Message);
    }
}
