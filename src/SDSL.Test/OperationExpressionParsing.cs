using Xunit;
using System.Linq;
using SDSL.Parsing;
using Eto.Parse;
using Eto.Parse.Parsers;
using System.Collections.Generic;

namespace SDSL.Parsing.Test;

public class OperationExpressionParsing
{
    ShaderMixinParser parser;
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
            parser.TestParse("5*3;"),
            parser.TestParse("5*3*4;"),
            parser.TestParse("5 * (float)++my_var;"),
            parser.TestParse("5* (float)++my_var.a;"),
            parser.TestParse("(float4)my_var[0]++ * 2;"),
            parser.TestParse("(float4x4)++my_var[a]* 2;"),
            parser.TestParse("(MyStruct)++my_var.a[0] * 2;"),
            parser.TestParse("2 * 3 * (MyStruct)my_var.a[b]++;"),
            parser.TestParse("(MyStruct)++my_var.a[0].c *4* 5;"),
            parser.TestParse("(float)my_value * (MyStruct)my_var.a[b].c++ *(float)my_value2;"),
            parser.TestParse("(float)my_value * (MyStruct)++my_var.a[b].c[5].b.e[7][5];"),
        };

        Assert.True(matches.TrueForAll(x => !x.Errors.Any()));

    }
    [Fact]
    public void TestSum()
    {
        parser.Grammar.Using(parser.Grammar.SumExpression.Then(";"));
        List<GrammarMatch> matches = new()
        {
            parser.TestParse("5+3;"),
            parser.TestParse("a + b++ * 3 + 4;"),
            parser.TestParse("5+3+4;"),
            parser.TestParse("3 + 5 * (float)++my_var;"),
            parser.TestParse("3 + 5* (float)++my_var.a;"),
            parser.TestParse("a + (float4)my_var[0]++ * 2 + 4;"),
            parser.TestParse("my_otherVar + (float4x4)++my_var[a]* 2 - 2;"),
            parser.TestParse("(float)1 + (MyStruct)++my_var.a[0] * 2;"),
            parser.TestParse("2 * 3 + (MyStruct)my_var.a[b]++;"),
            parser.TestParse("(MyStruct)++my_var.a[0].c + 6 + 4 * 5;"),
            parser.TestParse("(float)my_value + (MyStruct)my_var.a[b].c++ *(float)my_value2;"),
            parser.TestParse("2 + (float)my_value * (MyStruct)++my_var.a[b].c[5].b.e[7][5] + ++b;"),
        };

        Assert.True(matches.TrueForAll(x => !x.Errors.Any()));

    }

    [Fact]
    public void TestShift()
    {
        parser.Grammar.Using(parser.Grammar.ShiftExpression.Then(";"));
        List<GrammarMatch> matches = new()
        {
            parser.TestParse("5 << 5+3;"),
            parser.TestParse("a + b++ * 3 << 4;"),
            parser.TestParse("5 << 3 >> 4;"),
            parser.TestParse("3 + 5 * (float)++my_var;"),
            parser.TestParse("3 + 5* (float)++my_var.a;"),
            parser.TestParse("a >> (float4)my_var[0]++ * 2 + 4;"),
            parser.TestParse("my_otherVar << (float4x4)++my_var[a]* 2 - 2;"),
            parser.TestParse("(float)1 + (MyStruct)++my_var.a[0] << 2;"),
            parser.TestParse("2 * 3 + (MyStruct)my_var.a[b]++;"),
            parser.TestParse("(MyStruct)++my_var.a[0].c + 6 + 4 * 5 >> 2;"),
            parser.TestParse("(float)my_value + (MyStruct)my_var.a[b].c++ *(float)my_value2;"),
            parser.TestParse("2 + (float)my_value << (MyStruct)++my_var.a[b].c[5].b.e[7][5] >> ++b;"),
        };

        Assert.True(matches.TrueForAll(x => !x.Errors.Any()));

    }

    [Fact]
    public void TestTestExpr()
    {
        parser.Grammar.Using(parser.Grammar.TestExpression.Then(";"));
        List<GrammarMatch> matches = new()
        {
            parser.TestParse("5 < 5+3;"),
            parser.TestParse("a > b++ * 3 < 4;"),
            parser.TestParse("5 < 3 > 4;"),
            parser.TestParse("3 + 5 * (float)++my_var;"),
            parser.TestParse("3 + 5 > (float)++my_var.a*2;"),
            parser.TestParse("a >> (float4)my_var[0]++ * 2 + 4;"),
            parser.TestParse("my_otherVar <a.b<< (float4x4)++my_var[a]* 2 - 2;"),
            parser.TestParse("(float)1 + (MyStruct)++my_var.a[0] < 2;"),
            parser.TestParse("2 * 3 < (MyStruct)my_var.a[b]++;"),
            parser.TestParse("(MyStruct)++my_var.a[0].c + 6 + 4 * 5 >> 2;"),
            parser.TestParse("(float)my_value + (MyStruct)my_var.a[b].c++ >(float)my_value2;"),
            parser.TestParse("2 + (float)my_value < (MyStruct)++my_var.a[b].c[5].b.e[7][5] > ++b;"),
        };

        Assert.True(matches.TrueForAll(x => !x.Errors.Any()));

    }
    [Fact]
    public void TestEqualsExpr()
    {
        parser.Grammar.Using(parser.Grammar.EqualsExpression.Then(";"));
        List<GrammarMatch> matches = new()
        {
            parser.TestParse("true == false;"),
            parser.TestParse("true != false;"),
            parser.TestParse("a > b++ == 3 < 4;"),
            parser.TestParse("true == 3 != 4;"),
            parser.TestParse("3 == 5 * (float)++my_var;"),
            parser.TestParse("3 + 5 == (float)++my_var.a*2;"),
            parser.TestParse("5 == a >> (float4)my_var[0]++ * 2 + 4;"),
            parser.TestParse("my_otherVar <a.b<< (float4x4)++my_var[a]* 2 == 2;"),
            parser.TestParse("true == (float)1 + (MyStruct)++my_var.a[0] < 2;"),
            parser.TestParse("2 * 3 == 3 < (MyStruct)my_var.a[b]++;"),
            parser.TestParse("(MyStruct)++my_var.a[0].c + 6 == 4 * 5 >> 2;"),
            parser.TestParse("(float)my_value + (MyStruct)my_var.a[b].c++ !=(float)my_value2;"),
            parser.TestParse("2 + (float)my_value == (float)my_value * (MyStruct)++my_var.a[b].c[5].b.e[7][5] > ++b;"),
        };

        Assert.True(matches.TrueForAll(x => !x.Errors.Any()));

    }

    [Fact]
    public void TestBinary()
    {
        parser.Grammar.Using(parser.Grammar.OrExpression.Then(";"));
        List<GrammarMatch> matches = new()
        {
            parser.TestParse("5 & 4;"),
            parser.TestParse("1 ^ 6;"),
            parser.TestParse("1 | 6;"),
            parser.TestParse("a >> b++ | 3 << 4;"),
            parser.TestParse("5 ^ 3 ^ 4;"),
            parser.TestParse("3 & 5 * (float)++my_var;"),
            parser.TestParse("3 + 5 | (float)++my_var.a*2;"),
            parser.TestParse("5 & a >> (float4)my_var[0]++ * 2 & 4;"),
            parser.TestParse("my_otherVar <<a.b<< (float4x4)++my_var[a]* 2 |2;"),
            parser.TestParse("true & (float)1 + (MyStruct)++my_var.a[0] << 2;"),
            parser.TestParse("2 & 3 && 3 << (MyStruct)my_var.a[b]++;"),
            parser.TestParse("(MyStruct)++my_var.a[0].c + 6 & 4 * 5 >> 2;"),
            parser.TestParse("(float)my_value + (MyStruct)my_var.a[b].c+++(float)my_value2;"),
            parser.TestParse("2 ^ (float)my_value | (float)my_value * (MyStruct)++my_var.a[b].c[5].b.e[7][5] >> ++b;"),
        };

        Assert.True(matches.TrueForAll(x => !x.Errors.Any()));

    }
    [Fact]
    public void TestConditional()
    {
        parser.Grammar.Using(parser.Grammar.ConditionalExpression.Then(";"));
        List<GrammarMatch> matches = new()
        {
            parser.TestParse("true && true;"),
            parser.TestParse("true || false;"),
            parser.TestParse("1 || 6;"),
            parser.TestParse("a > b++ && 3 < 4;"),
            parser.TestParse("true == true || 3 != 4;"),
            parser.TestParse("3 || 5 * (float)++my_var;"),
            parser.TestParse("3 + 5 && (float)++my_var.a*2;"),
            parser.TestParse("5 == a && (float4)my_var[0]++ * 2 & 4;"),
            parser.TestParse("my_otherVar <a.b<< (float4x4)++my_var[a]* 2 |2;"),
            parser.TestParse("true == (float)1  && (MyStruct)++my_var.a[0] < 2;"),
            parser.TestParse("2 & 3 && 3 < (MyStruct)my_var.a[b]++;"),
            parser.TestParse("(MyStruct)++my_var.a[0].c || 6 == 4 * 5 >> 2 &&false;"),
            parser.TestParse("(float)my_value && (MyStruct)my_var.a[b].c++ !=(float)my_value2;"),
            parser.TestParse("2 ^ (float)my_value | (float)my_value | (MyStruct)++my_var.a[b].c[5].b.e[7][5] && 4 == ++b;"),
            parser.TestParse("true ? 5 : 8;"),

        };

        Assert.True(matches.TrueForAll(x => !x.Errors.Any()));

    }

}