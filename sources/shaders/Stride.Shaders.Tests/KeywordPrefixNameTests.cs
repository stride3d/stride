// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Shaders.Parsing;

namespace Stride.Shaders.Parsers.Tests;

/// <summary>
/// A name that starts with a statement keyword (<c>whileCount</c>, <c>returnValue</c>) is a name, not the keyword.
/// </summary>
public class KeywordPrefixNameTests
{
    [Theory]
    [InlineData("while")]
    [InlineData("for")]
    [InlineData("do")]
    [InlineData("if")]
    [InlineData("switch")]
    [InlineData("return")]
    [InlineData("break")]
    [InlineData("continue")]
    [InlineData("discard")]
    [InlineData("foreach")]
    public void NameStartingWithAKeywordIsAName(string keyword)
    {
        var result = SDSLParser.Parse($$"""
            shader S
            {
                int F()
                {
                    int {{keyword}}Count = 0;
                    {{keyword}}Count++;
                    {{keyword}}Count = {{keyword}}Count + 1;
                    return {{keyword}}Count;
                }
            };
            """);
        Assert.True(result.Errors.Count == 0, string.Join("\n", result.Errors.Select(x => x.ToString())));
    }
}
