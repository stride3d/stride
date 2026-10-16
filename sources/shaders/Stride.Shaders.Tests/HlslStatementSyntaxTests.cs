// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

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
}
