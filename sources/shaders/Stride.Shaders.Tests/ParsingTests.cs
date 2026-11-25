
using Stride.Shaders.Parsing;
using Stride.Shaders.Parsing.Analysis;
using Stride.Shaders.Parsing.SDSL;
using Stride.Shaders.Parsing.SDSL.AST;
namespace Stride.Shaders.Parsers.Tests;

public class ParsingTests1
{
    public static IEnumerable<object[]> GetShaderFilePaths()
    {
        var files = Directory.GetFiles("assets/Stride/SDSL", "*.sdsl");
        foreach (var file in files)
        {
            yield return new object[] { file };
        }
        files = Directory.GetFiles("assets/Stride/SDFX", "*.sdfx");
        foreach (var file in files)
        {
            yield return new object[] { file };
        }
        files = Directory.GetFiles("assets/SDSL", "*.sdsl", SearchOption.AllDirectories);
        foreach (var file in files)
        {
            yield return new object[] { file };
        }
    }

    [Theory]
    [MemberData(nameof(GetShaderFilePaths))]
    public void ParseFile(string path)
    {
        var text = MonoGamePreProcessor.OpenAndRun(path, []);
        var result = SDSLParser.Parse(text);
        Assert.True(result.Errors.Count == 0, path + string.Join("\n", result.Errors.Select(x => x.ToString())));
    }

    // A struct member can be an array, like a shader member
    [Fact]
    public void StructMemberCanBeAnArray()
    {
        var result = SDSLParser.Parse("shader S { struct T { float2 uv[4]; float x; }; };");
        Assert.True(result.Errors.Count == 0, string.Join("\n", result.Errors.Select(x => x.ToString())));
        var members = ((ShaderFile)result.AST!).RootDeclarations.OfType<ShaderClass>().Single().Elements.OfType<ShaderStruct>().Single().Members;
        Assert.Equal(["uv", "x"], members.Select(x => x.Name.Name));
        Assert.Single(members[0].TypeName.ArraySize!);
        Assert.Null(members[1].TypeName.ArraySize);
    }

    // `module` marks a member of the shader's per-effect part; `stage` is the old spelling, still accepted
    [Theory]
    [InlineData("module")]
    [InlineData("stage")]
    public void ModuleModifierMarksEveryKindOfMember(string keyword)
    {
        var text = $$"""
            shader S
            {
                {{keyword}} float A;
                {{keyword}} stream float B;
                {{keyword}} compose ComputeColor C;
                {{keyword}} SamplerState D { Filter = MIN_MAG_MIP_LINEAR; };
                cbuffer PerDraw { {{keyword}} float E; };
                rgroup PerMaterial { {{keyword}} Texture2D F; };
                {{keyword}} void G() { }
                float H;
            };
            """;
        var result = SDSLParser.Parse(text);
        Assert.True(result.Errors.Count == 0, string.Join("\n", result.Errors.Select(x => x.ToString())));

        var shader = Assert.IsType<ShaderClass>(Assert.Single(((ShaderFile)result.AST!).RootDeclarations));
        var staged = new Dictionary<string, bool>();
        foreach (var element in shader.Elements)
        {
            switch (element)
            {
                case ShaderMember member: staged[member.Name.Name] = member.IsStaged; break;
                case ShaderCompose compose: staged[compose.Name.Name] = compose.IsStaged; break;
                case ShaderSamplerState sampler: staged[sampler.Name.Name] = sampler.IsStaged; break;
                case ShaderMethod method: staged[method.Name.Name] = method.IsStaged; break;
                case ShaderBuffer buffer:
                    foreach (var member in buffer.Members)
                        staged[member.Name.Name] = member.IsStaged;
                    break;
            }
        }
        Assert.Equal(new Dictionary<string, bool> { ["A"] = true, ["B"] = true, ["C"] = true, ["D"] = true, ["E"] = true, ["F"] = true, ["G"] = true, ["H"] = false }, staged);
    }

    // Regression test: a trailing-dot float literal (e.g. "1.") is valid per the HLSL grammar
    // (https://learn.microsoft.com/en-us/windows/win32/direct3dhlsl/dx-graphics-hlsl-appendix-grammar#floating-point-numbers),
    // but a garbage character directly following it (e.g. "1.q") should still be reported as a parse error.
    [Fact]
    public void InvalidCharacterAfterTrailingDotFloatLiteralStillErrors()
    {
        var text = "shader Foo { void Bar() { float i = 1.q; } };";
        var result = SDSLParser.Parse(text);
        Assert.True(result.Errors.Count > 0, "Expected a parse error for invalid float suffix '1.q' but got none. Errors: " + string.Join("\n", result.Errors.Select(x => x.ToString())));
    }

    // A postfix '.' or '[' with nothing valid after it used to be consumed and silently dropped.
    [Theory]
    [InlineData("float i = a.;")]
    [InlineData("float i = a . ;")]
    [InlineData("float i = 1..;")]
    [InlineData("float i = 1.0.;")]
    [InlineData("float i = a.b.;")]
    [InlineData("float i = a[;")]
    [InlineData("float i = a[];")]
    [InlineData("float i = a[1;")]
    [InlineData("float i = a[1].;")]
    public void DanglingPostfixAccessorIsAnError(string statement)
    {
        var text = "shader Foo { void Bar() { float a = 0; " + statement + " } };";
        var result = SDSLParser.Parse(text);
        Assert.True(result.Errors.Count > 0, "Expected a parse error for '" + statement + "' but got none.");
    }

    [Theory]
    [InlineData("float i = a.x;")]
    [InlineData("float i = a . x;")]
    [InlineData("float i = a.b.c;")]
    [InlineData("float i = a[1];")]
    [InlineData("float i = a[1].x;")]
    [InlineData("float i = a[1][2];")]
    [InlineData("float i = a.Foo(1).x;")]
    [InlineData("float i = a++;")]
    [InlineData("float i = a[1]++;")]
    [InlineData("float i = 1.;")]
    public void PostfixAccessorChainParses(string statement)
    {
        var text = "shader Foo { void Bar() { float a = 0; " + statement + " } };";
        var result = SDSLParser.Parse(text);
        Assert.True(result.Errors.Count == 0, statement + "\n" + string.Join("\n", result.Errors.Select(x => x.ToString())));
    }

    // [Theory]
    // [MemberData(nameof(GetShaderFilePaths))]
    // public void AnalyseFile(string path)
    // {
    //     var text = MonoGamePreProcessor.OpenAndRun(path, []);
    //     var result = SDSLParser.Parse(text);
    //     Assert.True(result.Errors.Count == 0, path + string.Join("\n", result.Errors.Select(x => x.ToString())));
    // }

    // A cast to a reserved type name takes any unary operand, as in HLSL. A cast to another name, which could be a variable,
    // takes a cast or a postfix operand, so (a) - b stays a subtraction
    [Theory]
    [InlineData("(float)(uint)x", "(float)(uint)x", typeof(CastExpression))]
    [InlineData("(float)(uint)-x", "(float)(uint)-x", typeof(CastExpression))]
    [InlineData("(float)(uint)(x)", "(float)(uint)(x)", typeof(CastExpression))]
    [InlineData("(float2)(uint2)v", "(float2)(uint2)v", typeof(CastExpression))]
    [InlineData("(float)(int)(uint)x", "(float)(int)(uint)x", typeof(CastExpression))]
    [InlineData("(float)-x * y", "( (float)-x * y )", typeof(BinaryExpression))]
    [InlineData("(float)(a) - b", "( (float)(a) - b )", typeof(BinaryExpression))]
    [InlineData("(a)(uint)x", "(a)(uint)x", typeof(CastExpression))]
    [InlineData("(a)(b)", "(a)(b)", typeof(CastExpression))]
    [InlineData("(a) - b", "( (a) - b )", typeof(BinaryExpression))]
    [InlineData("(a) + -b", "( (a) + -b )", typeof(BinaryExpression))]
    [InlineData("-(float)(uint)x", "-(float)(uint)x", typeof(PrefixExpression))]
    [InlineData("~(uint)x", "~(uint)x", typeof(PrefixExpression))]
    [InlineData("!(bool)(uint)x", "!(bool)(uint)x", typeof(PrefixExpression))]
    public void CastOperandParses(string text, string expected, Type expectedType)
    {
        var scanner = new Scanner(text);
        var result = new ParseResult();
        Assert.True(ExpressionParser.Expression(ref scanner, result, out var parsed), text + "\n" + string.Join("\n", result.Errors.Select(x => x.ToString())));
        Assert.True(scanner.IsEof, text);
        Assert.Equal(expected, parsed.ToString());
        Assert.IsType(expectedType, parsed);
    }
}
