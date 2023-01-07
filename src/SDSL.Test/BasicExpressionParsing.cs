using Xunit;
using System.Linq;
using SDSL.Parsing;
using Eto.Parse;
using Eto.Parse.Parsers;
using System.Collections.Generic;

namespace SDSL.Parsing.Test;

public class BasicExpressionParsing
{
    ShaderMixinParser parser;
    public BasicExpressionParsing()
    {
        parser = new();
    }
    [Fact]
    public void TestTerms()
    {
        parser.Grammar.Using(parser.Grammar.TermExpression);
        List<GrammarMatch> matches = new(){
            parser.TestParse("5"),
            parser.TestParse("5l"),
            parser.TestParse("5u"),
            parser.TestParse("5f"),
            parser.TestParse(".5"),
            parser.TestParse("5f"),
            parser.TestParse(".5d"),
            parser.TestParse("my_var"),
            parser.TestParse("\"Hello World\"")
        };
        Assert.True(matches.TrueForAll(x => !x.Errors.Any()));

    }
    [Fact]
    public void TestPostfix()
    {
        parser.Grammar.Using(parser.Grammar.PostfixExpression.Then(";"));
        List<GrammarMatch> matches = new(){
            parser.TestParse("my_var++;"),
            parser.TestParse("my_var.a;"),
            parser.TestParse("my_var[0];"),
            parser.TestParse("my_var[a];"),
            parser.TestParse("my_var.a[0];"),
            parser.TestParse("my_var.a[b];"),
            parser.TestParse("my_var.a[b]++;"),
            parser.TestParse("my_var.a[0].c;"),
            parser.TestParse("my_var.a[b].c;"),
            parser.TestParse("my_var.a[b].c++;"),
            parser.TestParse("my_var.a[b].c[5].b.e[7][5]++;"),
            
        };
        Assert.True(matches.TrueForAll(x => !x.Errors.Any()));

    }

    [Fact]
    public void TestUnary()
    {
        parser.Grammar.Using(parser.Grammar.UnaryExpression.Then(";"));
        List<GrammarMatch> matches = new()
        {
            parser.TestParse("++b;"),
            parser.TestParse("++my_var.a;"),
            parser.TestParse("++my_var[0];"),
            parser.TestParse("++my_var[a];"),
            parser.TestParse("++my_var.a[0];"),
            parser.TestParse("++my_var.a[b];"),
            parser.TestParse("++my_var.a[0].c;"),
            parser.TestParse("++my_var.a[b].c;"),
            parser.TestParse("++my_var.a[b].c[5].b.e[7][5];"),
        };
        Assert.True(matches.TrueForAll(x => !x.Errors.Any()));
    }

    [Fact]
    public void TestCast()
    {
        parser.Grammar.Using(parser.Grammar.CastExpression.Then(";"));
        List<GrammarMatch> matches = new()
        {
            parser.TestParse("(float)++my_var;"),
            parser.TestParse("(float)++my_var.a;"),
            parser.TestParse("(float4)my_var[0]++;"),
            parser.TestParse("(float4x4)++my_var[a];"),
            parser.TestParse("(MyStruct)++my_var.a[0];"),
            parser.TestParse("(MyStruct)my_var.a[b]++;"),
            parser.TestParse("(MyStruct)++my_var.a[0].c;"),
            parser.TestParse("(MyStruct)my_var.a[b].c++;"),
            parser.TestParse("(MyStruct)++my_var.a[b].c[5].b.e[7][5];"),
        };
        Assert.True(matches.TrueForAll(x => !x.Errors.Any()));
    }
}