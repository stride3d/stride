// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Shaders.Tests;

namespace Stride.Shaders.Parsers.Tests;

/// <summary>
/// Initializer lists (<c>{ 1, 2, 3 }</c>) with the wrong number of values (SDSL0156, as DXC's "too few / too many elements")
/// or a value that isn't made of numbers (SDSL0157). The values they build are checked by RenderTest <c>BraceInitializers</c>.
/// </summary>
public class InitializerListTests
{
    [Fact]
    public void WrongValueCountIsAnError()
    {
        // Compile errors are thrown by the loader
        var loader = new ShaderLoader("./assets/SDSL/CompilerTests", "./assets/Stride/SDSL");
        var exception = Assert.Throws<InvalidOperationException>(() => loader.LoadExternalBuffer("InitializerListErrors", [], out _, out _, out _));

        Assert.Contains("SDSL0156: float3x3 needs 9 values, but the initializer list has 8.", exception.Message);
        Assert.Contains("SDSL0156: float2[2] needs 4 values, but the initializer list has 5.", exception.Message);
        Assert.Contains("SDSL0156: float3[] needs a multiple of 3 values, but the initializer list has 4.", exception.Message);
        Assert.Contains("SDSL0157: a Texture2D<float4> can't be in an initializer list.", exception.Message);
    }
}
