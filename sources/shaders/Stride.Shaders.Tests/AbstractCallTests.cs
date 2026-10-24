// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Diagnostics;
using Stride.Shaders.Compilers.SDSL;

namespace Stride.Shaders.Parsers.Tests;

/// <summary>
/// A call to an abstract method that no shader of the effect implements is an error only when the calling method runs: a method
/// replaced by an override never does (render test <c>AbstractCallInReplacedMethod</c>). When it runs, the error names both (SDSL0166).
/// </summary>
public class AbstractCallTests : IDisposable
{
    private readonly DirectoryInfo sourceDir = Directory.CreateTempSubdirectory("stride-shader-abstract-call");

    public void Dispose()
    {
        sourceDir.Delete(recursive: true);
        GC.SuppressFinalize(this);
    }

    private const string Base = """
        namespace Stride.Shaders.Tests;

        shader AbstractCallBase
        {
            stream float4 ColorTarget : SV_Target0;

            abstract float4 Filter(float4 color);

            stage float4 Shading()
            {
                return Filter(float4(1.0, 1.0, 1.0, 1.0));
            }

            void PSMain()
            {
                streams.ColorTarget = Shading();
            }
        }
        """;

    [Theory]
    // Shading is not replaced
    [InlineData("")]
    // The override calls the replaced Shading
    [InlineData("stage override float4 Shading() { return base.Shading() * 0.5; }")]
    public void CalledAbstractMethodIsAnError(string members)
    {
        var (ok, log) = Mix(members);
        Assert.False(ok);
        Assert.Contains("SDSL0166: AbstractCallBase.Shading calls abstract method AbstractCallBase.Filter, but no shader of the effect overrides it. Mix in a shader that implements AbstractCallBase.Filter.", log);
    }

    [Theory]
    [InlineData("stage override float4 Shading() { return 0.5; }")]
    [InlineData("override float4 Filter(float4 color) { return color * 0.5; }")]
    public void UncalledOrImplementedAbstractMethodIsFine(string members)
    {
        var (ok, log) = Mix(members);
        Assert.True(ok, log);
    }

    private (bool ok, string log) Mix(string members)
    {
        File.WriteAllText(Path.Combine(sourceDir.FullName, "AbstractCallBase.sdsl"), Base);
        File.WriteAllText(Path.Combine(sourceDir.FullName, "AbstractCall.sdsl"), $$"""
            namespace Stride.Shaders.Tests;

            shader AbstractCall : AbstractCallBase
            {
                {{members}}
            }
            """);

        var log = new LoggerResult();
        var ok = new ShaderMixer(new ShaderLoader(sourceDir.FullName, "./assets/Stride/SDSL"))
            .MergeSDSL(new ShaderClassSource("AbstractCall"), new ShaderMixer.Options(true), log, out _, out _, out _, out _);

        return (ok, string.Join(Environment.NewLine, log.Messages.Select(m => m.Text)));
    }
}
