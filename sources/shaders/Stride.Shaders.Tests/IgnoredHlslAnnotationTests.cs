// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Diagnostics;
using Stride.Shaders.Parsing;
using Stride.Shaders.Parsing.SDSL.AST;

namespace Stride.Shaders.Parsers.Tests;

/// <summary>
/// HLSL qualifiers that Stride reads and drops: <c>register(...)</c> / <c>packoffset(...)</c> (warning SDSL0220),
/// a return semantic on a helper and a semantic on a sampler (silently, as with FXC).
/// </summary>
public class IgnoredHlslAnnotationTests
{
    [Fact]
    public void QualifiersAreReadAndNothingIsKept()
    {
        var result = SDSLParser.Parse("shader S { float2 p : register(c1) = 1; SamplerState s : IMMUTABLE { Filter = MIN_MAG_MIP_POINT; }; SamplerState t : register(s2); float4 F() : COLOR { return 0; } };");
        Assert.True(result.Errors.Count == 0, string.Join("\n", result.Errors.Select(x => x.ToString())));

        var shader = Assert.IsType<ShaderClass>(Assert.Single(((ShaderFile)result.AST!).RootDeclarations));
        var member = shader.Elements.OfType<ShaderMember>().Single();
        Assert.Null(member.Semantic);
        Assert.NotNull(member.Value);
        Assert.Equal("register(c1)", member.IgnoredRegister?.Text.ToString());

        var samplers = shader.Elements.OfType<ShaderSamplerState>().ToList();
        Assert.Null(samplers[0].IgnoredRegister);
        Assert.Single(samplers[0].Parameters);
        Assert.Equal("register(s2)", samplers[1].IgnoredRegister?.Text.ToString());

        Assert.Equal("COLOR", shader.Elements.OfType<ShaderMethod>().Single().ReturnSemantic?.Name);
    }

    [Theory]
    [InlineData("./assets/SDSL/RenderTests", "IgnoredHlslAnnotations", new[] { "packoffset(c0.y) on Red", "register(c3) on Blue" })]
    [InlineData("./assets/SDSL/CompilerTests", "IgnoredRegisterMembers", new[] { "register(c1) on p1", "register(c2) on Scale", "register(s3) on PointSampler" })]
    public void RegisterAndPackoffsetAreWarnings(string path, string shaderName, string[] expected)
    {
        var log = new LoggerResult();
        var loader = new ShaderLoader(path, "./assets/Stride/SDSL") { Log = log };
        loader.LoadExternalBuffer(shaderName, [], out _, out _, out _);
        Assert.False(log.HasErrors, string.Join("\n", log.Messages));

        var warnings = log.Messages.Where(m => m.Type == LogMessageType.Warning).Select(m => m.Text).ToList();
        Assert.Equal(expected.Length, warnings.Count);
        foreach (var text in expected)
            Assert.Contains(warnings, w => w.Contains($"SDSL0220: {text} is ignored: Stride lays out cbuffers and binds resources itself."));
    }
}
