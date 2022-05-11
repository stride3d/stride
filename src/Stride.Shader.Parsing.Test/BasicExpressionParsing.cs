using Xunit;
using System.Linq;
using Stride.Shader.Parsing;
using Eto.Parse;
using Eto.Parse.Parsers;
using System.Collections.Generic;

namespace Stride.Shader.Parsing.Test;

public class BasicExpressionParsing
{
    SDSLParser parser;
    public BasicExpressionParsing()
    {
        parser = new();
    }
    [Fact]
    public void TestTerms()
    {
        parser.Grammar.Using(parser.Grammar.TermExpression);
        List<GrammarMatch> matches = new(){
            parser.Parse("5"),
            parser.Parse("5l"),
            parser.Parse("5u"),
            parser.Parse("5f"),
            parser.Parse(".5"),
            parser.Parse("5f"),
            parser.Parse(".5d"),
            parser.Parse("my_var"),
            parser.Parse("\"Hello World\"")
        };
        Assert.True(matches.TrueForAll(x => !x.Errors.Any()));

    }
    [Fact]
    public void TestPostfix()
    {
        parser.Grammar.Using(parser.Grammar.PostFixExpression.Then(";"));
        List<GrammarMatch> matches = new(){
            parser.Parse("my_var++;"),
            parser.Parse("my_var.a;"),
            parser.Parse("my_var[0];"),
            parser.Parse("my_var[a];"),
            parser.Parse("my_var.a[0];"),
            parser.Parse("my_var.a[b];"),
            parser.Parse("my_var.a[b]++;"),
            parser.Parse("my_var.a[0].c;"),
            parser.Parse("my_var.a[b].c;"),
            parser.Parse("my_var.a[b].c++;"),
            parser.Parse("my_var.a[b].c[5].b.e[7][5]++;"),
            
        };
        Assert.True(matches.TrueForAll(x => !x.Errors.Any()));

    }

    [Fact]
    public void TestUnary()
    {
        parser.Grammar.Using(parser.Grammar.UnaryExpression.Then(";"));
        List<GrammarMatch> matches = new()
        {
            parser.Parse("++b;"),
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