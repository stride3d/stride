// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Diagnostics;
using Stride.Shaders.Parsing;
using Stride.Shaders.Parsing.SDSL.AST;

namespace Stride.Shaders.Parsers.Tests;

/// <summary>
/// A composition written with a value (<c>compose ComputeFloat X = 0.1f;</c>) compiles: the value is kept in the tree for tools
/// but never compiled, and the class gets warning SDSL0236. The render test <c>ComposeWithValue</c> checks the output.
/// </summary>
public class ComposeWithValueTests
{
    [Fact]
    public void ValueIsKeptInTheTree()
    {
        var result = SDSLParser.Parse("shader S { compose ComputeFloat Distance = 0.1f; };");
        Assert.True(result.Errors.Count == 0, string.Join("\n", result.Errors.Select(x => x.ToString())));

        var shader = Assert.IsType<ShaderClass>(Assert.Single(((ShaderFile)result.AST!).RootDeclarations));
        var member = shader.Elements.OfType<ShaderMember>().Single();
        Assert.True(member.IsCompose);
        Assert.Equal("0.1f", member.Value?.Info.Text.ToString());
    }

    [Fact]
    public void ValueIsAWarning()
    {
        var log = new LoggerResult();
        var loader = new ShaderLoader("./assets/SDSL/RenderTests", "./assets/Stride/SDSL") { Log = log };
        loader.LoadExternalBuffer("ComposeWithValue", [], out _, out _, out _);
        Assert.False(log.HasErrors, string.Join("\n", log.Messages));

        var warnings = log.Messages.Where(m => m.Type == LogMessageType.Warning).Select(m => m.Text).ToList();
        Assert.Equal(3, warnings.Count);
        foreach (var (name, value) in new[] { ("Color", "float4(1.0, 0.0, 0.0, 1.0)"), ("Scale", "0.5f"), ("Count", "1") })
            Assert.Contains(warnings, w => w.Contains($"SDSL0236: the value of composition {name} is ignored: a composition gets its shader from the effect, not from a value. Remove '= {value}'."));
    }
}
