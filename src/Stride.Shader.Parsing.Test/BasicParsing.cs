using Xunit;
using System.Linq;
using Stride.Shader.Parsing;
using Eto.Parse;
using Eto.Parse.Parsers;
using System.Collections.Generic;

namespace Stride.Shader.Parsing.Test;

public class BasicParsing
{
    SDSLGrammar Grammar;
    public BasicParsing()
    {
        Grammar = new();
    }
    [Fact]
    public void TestIdentifier()
    {
        var matches = new List<(string Name,GrammarMatch Matching)>{
            ("myVar",Grammar.Match("myVar")),
            ("my_Var",Grammar.Match("my_Var")),
            ("my_Var2",Grammar.Match("my_Var2")),
            ("my2Var",Grammar.Match("my2Var")),
            ("myVar",Grammar.Match("myVar"))
        };
        
        foreach(var (Name, Matching) in matches)
        {
            Assert.True(Matching.HasMatches);
            // Assert.True(Matching.Matches.Exists(x => x.Name == "Identifier"));
            Assert.True(Matching.Matches[0].StringValue == Name);
        }

    }   
    [Fact]
    public void TestConstant()
    {
        var values = new string[]{
            "5",
            "50",
            "51.5",
            "0",
            "-1"
        };
        var matches = 
            values.Select(x => (x,Grammar.Match(x)));
                
        foreach(var (Original, Matching) in matches)
        {
            Assert.True(Matching.HasMatches);
            Assert.True(Matching.Matches[0].StringValue == Original);
        }
    }
    [Fact]
    public void TestAdd()
    {

    }
    [Fact]
    public void TestMult()
    {

    }
    [Fact]
    public void TestAssign()
    {

    }
    [Fact]
    public void TestParenthesis()
    {

    }
    [Fact]
    public void TestOperations()
    {

    }
    
}