// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Linq;
using Stride.Shaders.Parsing;
using Stride.Shaders.Parsing.SDFX.AST;
using Stride.Shaders.Parsing.SDSL.AST;
using Xunit;

namespace Stride.Shaders.Parsers.Tests;

/// <summary>
/// The .sdfx files of Stride 4.3 and older could declare an effect with <c>shader</c>: an .sdfx reads it as an effect and keeps
/// where the keyword is, for the error and the upgrade; an .sdsl reads it as a shader class.
/// </summary>
public class EffectShaderKeywordTests
{
    private const string OldEffects = """
        namespace Game
        {
            shader Main
            {
                mixin Base;
                mixin child Child;
            };

            partial shader Child
            {
                mixin Other;
            }
        }
        """;

    [Fact]
    public void AnEffectFileReadsShaderAsAnEffectAndKeepsTheKeyword()
    {
        var result = SDSLParser.Parse(OldEffects, isEffectFile: true);

        Assert.Empty(result.Errors);
        var effects = ((ShaderFile)result.AST!).Namespaces.Single().Declarations.Cast<ShaderEffect>().ToList();
        Assert.Equal(["Main", "Child"], effects.Select(x => x.Name.Name));
        Assert.Equal([false, true], effects.Select(x => x.IsPartial));
        Assert.Equal([(3, 5), (9, 13)], effects.Select(x => (x.ShaderKeyword!.Value.Line, x.ShaderKeyword.Value.Column)));
        Assert.All(effects, x => Assert.Equal("shader", x.ShaderKeyword!.Value.TextSpan.ToString()));
    }

    [Fact]
    public void AShaderFileStillReadsShaderAsAClass()
    {
        Assert.NotEmpty(SDSLParser.Parse(OldEffects).Errors);
        Assert.IsType<ShaderClass>(Assert.Single(((ShaderFile)SDSLParser.Parse("shader S { };").AST!).RootDeclarations));
    }

    [Fact]
    public void AnEffectDeclaredWithEffectHasNoShaderKeyword()
    {
        var result = SDSLParser.Parse("effect Main { mixin Base; }", isEffectFile: true);

        Assert.Null(Assert.IsType<ShaderEffect>(Assert.Single(((ShaderFile)result.AST!).RootDeclarations)).ShaderKeyword);
    }

    // A shader class in an .sdfx is not an effect: it keeps the errors and the declaration of a class
    [Fact]
    public void AShaderClassInAnEffectFileStaysAClass()
    {
        var result = SDSLParser.Parse("shader S { float4 F() { return 0; } };", isEffectFile: true);
        Assert.IsType<ShaderClass>(Assert.Single(((ShaderFile)result.AST!).RootDeclarations));

        var broken = SDSLParser.Parse("shader S { float4 F() { return 0 } };", isEffectFile: true);
        Assert.Equal(SDSLParser.Parse("shader S { float4 F() { return 0 } };").Errors.Select(x => x.ToString()), broken.Errors.Select(x => x.ToString()));
    }
}
