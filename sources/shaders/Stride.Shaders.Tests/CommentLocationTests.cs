// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Shaders.Parsing;
using Stride.Shaders.Parsing.SDSL.AST;

namespace Stride.Shaders.Parsers.Tests;

/// <summary>
/// Comments don't move the lines and columns that errors report: the parser reads the code with each comment replaced by spaces.
/// </summary>
public class CommentLocationTests
{
    const string Code = """
        // A line comment
        /* A block comment
           over three lines */
        shader S
        {
            // Another line comment
            void F() { /* before */ int x = 1 }
        };
        """;

    [Fact]
    public void ParseErrorHasTheLineAndColumnOfTheFile()
    {
        var error = SDSLParser.Parse(Code).Errors[0];
        // The missing ';' after `int x = 1`, on line 7
        Assert.Equal((7, 39), (error.Location.Line, error.Location.Column));
    }

    // Columns count characters, a tab is one (as csc and the VS Code Problems panel count them)
    [Fact]
    public void TabsCountAsOneColumn()
    {
        var error = SDSLParser.Parse("shader S\n{\n\tvoid F() {\t/*\tx\t*/\tint y = 1 }\n};").Errors[0];
        Assert.Equal((3, 31), (error.Location.Line, error.Location.Column));
    }

    [Fact]
    public void NodeLocationHasTheLineAndColumnOfTheFile()
    {
        var result = SDSLParser.Parse(Code.Replace("int x = 1 }", "int x = 1; }"));
        Assert.True(result.Errors.Count == 0, string.Join("\n", result.Errors.Select(x => x.ToString())));

        var shader = Assert.IsType<ShaderClass>(Assert.Single(((ShaderFile)result.AST!).RootDeclarations));
        Assert.Equal((4, 1), (shader.Info.Line, shader.Info.Column));
        var declare = shader.Elements.OfType<ShaderMethod>().Single().Body!.Statements.Single();
        Assert.Equal((7, 29), (declare.Info.Line, declare.Info.Column));
    }
}
