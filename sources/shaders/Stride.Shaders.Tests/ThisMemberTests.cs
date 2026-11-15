// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Shaders.Parsers.Tests;

/// <summary>
/// <c>this.X</c> on a stage member and <c>base.X</c> on a variable are errors (SDSL0192), and so is <c>this.X</c> when the shader
/// has no member X (SDSL0112). The members that <c>this.X</c> reads are checked by RenderTest <c>ThisMember</c>.
/// </summary>
public class ThisMemberTests : IDisposable
{
    private readonly DirectoryInfo sourceDir = Directory.CreateTempSubdirectory("stride-shader-this-member");

    public void Dispose()
    {
        sourceDir.Delete(recursive: true);
        GC.SuppressFinalize(this);
    }

    [Theory]
    [InlineData("return this.Shared;", "(9,16): SDSL0192: this.Shared: Shared is a stage member, shared by every instance, so 'this.' doesn't apply to it. Write Shared.")]
    [InlineData("return this.Exposure;", "(9,16): SDSL0192: this.Exposure: Exposure is a stage member, shared by every instance, so 'this.' doesn't apply to it. Write Exposure.")]
    [InlineData("return base.Scale;", "(9,16): SDSL0192: base.Scale: Scale is a variable, which is not overridden, so 'base.' doesn't apply to it. Write Scale.")]
    [InlineData("float Missing = 1; return this.Missing;", "(9,40): SDSL0112: Could not resolve member Missing in expression this of type ThisMemberError")]
    [InlineData("return this.Twice;", "(9,21): SDSL0112: Could not resolve member Twice in expression this of type ThisMemberError")]
    public void QualifierThatDoesNotApplyIsAnError(string body, string expected)
    {
        File.WriteAllText(Path.Combine(sourceDir.FullName, "ThisMemberError.sdsl"), $$"""
            shader ThisMemberError
            {
                stage float Shared;
                float Scale;
                float Twice() { return Scale * 2; }

                float Compute()
                {
                    {{body}}
                }

                cbuffer Globals { stage float Exposure; }
            };
            """);
        var exception = Assert.Throws<InvalidOperationException>(() => new ShaderLoader(sourceDir.FullName, "./assets/Stride/SDSL").LoadExternalBuffer("ThisMemberError", [], out _, out _, out _));
        Assert.Contains($"ThisMemberError.sdsl{expected}", exception.Message);
    }
}
