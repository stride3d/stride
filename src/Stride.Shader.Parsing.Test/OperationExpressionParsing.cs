using Xunit;
using System.Linq;
using Stride.Shader.Parsing;
using Eto.Parse;
using Eto.Parse.Parsers;
using System.Collections.Generic;

namespace Stride.Shader.Parsing.Test;

public class OperationExpressionParsing
{
    SDSLParser parser;
    public OperationExpressionParsing()
    {
        parser = new();
    }
    [Fact]
    public void TestMul()
    {
        parser.Grammar.Using(parser.Grammar.MulExpression.Then(";"));
        List<GrammarMatch> matches = new()
        {
            parser.Parse("5*3;"),
            parser.Parse("5*3*4;"),
            parser.Parse("5 * (float)++my_var;"),
            parser.Parse("5* (float)++my_var.a;"),
            parser.Parse("(float4)my_var[0]++ * 2;"),
            parser.Parse("(float4x4)++my_var[a]* 2;"),
            parser.Parse("(MyStruct)++my_var.a[0] * 2;"),
            parser.Parse("2 * 3 * (MyStruct)my_var.a[b]++;"),
            parser.Parse("(MyStruct)++my_var.a[0].c *4* 5;"),
            parser.Parse("(float)my_value * (MyStruct)my_var.a[b].c++ *(float)my_value2;"),
            parser.Parse("(float)my_value * (MyStruct)++my_var.a[b].c[5].b.e[7][5];"),
        };

        Assert.True(matches.TrueForAll(x => !x.Errors.Any()));

    }
    [Fact]
    public void TestSum()
    {
        parser.Grammar.Using(parser.Grammar.SumExpression.Then(";"));
        List<GrammarMatch> matches = new()
        {
            parser.Parse("5+3;"),
            parser.Parse("a + b++ * 3 + 4;"),
            parser.Parse("5+3+4;"),
            parser.Parse("3 + 5 * (float)++my_var;"),
            parser.Parse("3 + 5* (float)++my_var.a;"),
            parser.Parse("a + (float4)my_var[0]++ * 2 + 4;"),
            parser.Parse("my_otherVar + (float4x4)++my_var[a]* 2 - 2;"),
            parser.Parse("(float)1 + (MyStruct)++my_var.a[0] * 2;"),
            parser.Parse("2 * 3 + (MyStruct)my_var.a[b]++;"),
            parser.Parse("(MyStruct)++my_var.a[0].c + 6 + 4 * 5;"),
            parser.Parse("(float)my_value + (MyStruct)my_var.a[b].c++ *(float)my_value2;"),
            parser.Parse("2 + (float)my_value * (MyStruct)++my_var.a[b].c[5].b.e[7][5] + ++b;"),
        };

        Assert.True(matches.TrueForAll(x => !x.Errors.Any()));

    }

    [Fact]
    public void TestShift()
    {
        parser.Grammar.Using(parser.Grammar.ShiftExpression.Then(";"));
        List<GrammarMatch> matches = new()
        {
            parser.Parse("5 << 5+3;"),
            parser.Parse("a + b++ * 3 << 4;"),
            parser.Parse("5 << 3 >> 4;"),
            parser.Parse("3 + 5 * (float)++my_var;"),
            parser.Parse("3 + 5* (float)++my_var.a;"),
            parser.Parse("a >> (float4)my_var[0]++ * 2 + 4;"),
            parser.Parse("my_otherVar << (float4x4)++my_var[a]* 2 - 2;"),
            parser.Parse("(float)1 + (MyStruct)++my_var.a[0] << 2;"),
            parser.Parse("2 * 3 + (MyStruct)my_var.a[b]++;"),
            parser.Parse("(MyStruct)++my_var.a[0].c + 6 + 4 * 5 >> 2;"),
            parser.Parse("(float)my_value + (MyStruct)my_var.a[b].c++ *(float)my_value2;"),
            parser.Parse("2 + (float)my_value << (MyStruct)++my_var.a[b].c[5].b.e[7][5] >> ++b;"),
        };

        Assert.True(matches.TrueForAll(x => !x.Errors.Any()));

    }

    [Fact]
    public void TestTestExpr()
    {
        parser.Grammar.Using(parser.Grammar.TestExpression.Then(";"));
        List<GrammarMatch> matches = new()
        {
            parser.Parse("5 < 5+3;"),
            parser.Parse("a > b++ * 3 < 4;"),
            parser.Parse("5 < 3 > 4;"),
            parser.Parse("3 + 5 * (float)++my_var;"),
            parser.Parse("3 + 5 > (float)++my_var.a*2;"),
            parser.Parse("a >> (float4)my_var[0]++ * 2 + 4;"),
            parser.Parse("my_otherVar <a.b<< (float4x4)++my_var[a]* 2 - 2;"),
            parser.Parse("(float)1 + (MyStruct)++my_var.a[0] < 2;"),
            parser.Parse("2 * 3 < (MyStruct)my_var.a[b]++;"),
            parser.Parse("(MyStruct)++my_var.a[0].c + 6 + 4 * 5 >> 2;"),
            parser.Parse("(float)my_value + (MyStruct)my_var.a[b].c++ >(float)my_value2;"),
            parser.Parse("2 + (float)my_value < (MyStruct)++my_var.a[b].c[5].b.e[7][5] > ++b;"),
        };

        Assert.True(matches.TrueForAll(x => !x.Errors.Any()));

    }

    [Fact]
    public void TestUnary()
    {
        parser.Grammar.Using(parser.Grammar.UnaryExpression.Then(";"));
        List<GrammarMatch> matches = new()
        {
            parser.Parse("++my_var;"),
            parser.Parse("++my_var.a;"),
            parser.Parse("++my_var[0];"),
            parser.Parse("++my_var[a];"),
            parser.Parse("++my_var.a[0];"),
            parser.Parse("++my_var.a[b];"),
            parser.Parse("++my_var.a[0].c;"),
            parser.Parse("++my_var.a[b].c;"),
            parser.Parse("++my_var.a[b].c[5].b.e[7][5];"),
        };
        Assert.True(matches.TrueForAll(x => !x.Errors.Any()));
    }

    [Fact]
    public void TestCast()
    {
        parser.Grammar.Using(parser.Grammar.CastExpression.Then(";"));
        List<GrammarMatch> matches = new()
        {
            parser.Parse("(float)++my_var;"),
            parser.Parse("(float)++my_var.a;"),
            parser.Parse("(float4)my_var[0]++;"),
            parser.Parse("(float4x4)++my_var[a];"),
            parser.Parse("(MyStruct)++my_var.a[0];"),
            parser.Parse("(MyStruct)my_var.a[b]++;"),
            parser.Parse("(MyStruct)++my_var.a[0].c;"),
            parser.Parse("(MyStruct)my_var.a[b].c++;"),
            parser.Parse("(MyStruct)++my_var.a[b].c[5].b.e[7][5];"),
        };
        Assert.True(matches.TrueForAll(x => !x.Errors.Any()));
    }



}