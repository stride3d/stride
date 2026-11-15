// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Diagnostics;
using Stride.Rendering;
using Stride.Shaders.Parsing;
using Stride.Shaders.Parsing.SDSL.AST;
using Stride.Shaders.Tests;

namespace Stride.Shaders.Parsers.Tests;

/// <summary>
/// HLSL statement syntax that the SDSL parser didn't read: the parts the RenderTests can't check.
/// </summary>
public class HlslStatementSyntaxTests
{
    // `do` starts `double` and `done`: only the whole word starts a do-while
    [Fact]
    public void NamesStartingWithDoAreNotLoops()
    {
        var result = SDSLParser.Parse("shader S { void F() { double d = 1; int done = 0; done++; do done++; while (done < 3); } };");
        Assert.True(result.Errors.Count == 0, string.Join("\n", result.Errors.Select(x => x.ToString())));

        var shader = Assert.IsType<ShaderClass>(Assert.Single(((ShaderFile)result.AST!).RootDeclarations));
        var body = shader.Elements.OfType<ShaderMethod>().Single().Body!;
        Assert.Collection(body.Statements,
            x => Assert.IsType<Declare>(x),
            x => Assert.IsType<Declare>(x),
            x => Assert.IsType<ExpressionStatement>(x),
            x => Assert.IsType<DoWhile>(x));
    }

    // An effect's do-while is written back as a loop in the generated C# (asset CompilerTests/EffectDoWhile.sdfx)
    [Theory]
    [InlineData(0, "OneShader")]
    [InlineData(3, "ThreeShader")]
    public void DoWhileInAnEffectLoops(int count, string expectedMixin)
    {
        var parameters = new ParameterCollection();
        parameters.Set(EffectDoWhileParameters.Count, count);

        var mixin = ShaderMixinManager.Generate("EffectDoWhile", parameters);

        Assert.Equal([expectedMixin], mixin.Mixins.Select(x => x.ClassName));
    }

    // `sample` is a name now, but stays an interpolation modifier where modifiers are read
    [Fact]
    public void SampleIsStillAnInterpolationModifier()
    {
        var result = SDSLParser.Parse("shader S { sample stream float4 A; float sample; };");
        Assert.True(result.Errors.Count == 0, string.Join("\n", result.Errors.Select(x => x.ToString())));

        var shader = Assert.IsType<ShaderClass>(Assert.Single(((ShaderFile)result.AST!).RootDeclarations));
        var members = shader.Elements.OfType<ShaderMember>().ToList();
        Assert.Equal(InterpolationModifier.Sample, members[0].Interpolation);
        Assert.Equal("sample", members[1].Name.Name);
    }

    // A local struct goes to the class under a unique name; the method's type names follow it
    [Fact]
    public void LocalStructsAreHoistedUnderUniqueNames()
    {
        var result = SDSLParser.Parse("shader S { struct P { float a; }; float F() { struct P { int b; }; P p; p.b = 1; return p.b; } float G() { struct P { int c; }; P p; p.c = 2; return p.c; } };");
        Assert.True(result.Errors.Count == 0, string.Join("\n", result.Errors.Select(x => x.ToString())));

        var shader = Assert.IsType<ShaderClass>(Assert.Single(((ShaderFile)result.AST!).RootDeclarations));
        Assert.Equal(["P", "P_in_F", "P_in_G"], shader.Elements.OfType<ShaderStruct>().Select(x => x.TypeName.Name));
        var g = shader.Elements.OfType<ShaderMethod>().Single(x => x.Name.Name == "G");
        Assert.Equal("P_in_G", g.Body!.Statements.OfType<Declare>().Single().TypeName.Name);
    }

    // fxc's X3081: a dropped value without side effect usually means a vector constructor was intended
    [Fact]
    public void CommaExpressionDroppingAValueIsAWarning()
    {
        var loader = new ShaderLoader("./assets/SDSL/RenderTests") { Log = new LoggerResult() };
        loader.LoadExternalBuffer("CommaExpression", [], out _, out _, out _);

        var warnings = ((LoggerResult)loader.Log).Messages.Where(m => m.Type == LogMessageType.Warning).Select(m => m.Text).ToList();
        var warning = Assert.Single(warnings);
        Assert.Contains("SDSL0219: comma expression (0.0f, 0.0f, 0.0f, 0.6f): the values before the last are dropped, the result is 0.6f.", warning);
    }
}
