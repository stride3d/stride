// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Shaders.Parsing;
using Stride.Shaders.Parsing.SDSL;
using Stride.Shaders.Parsing.SDSL.AST;

namespace Stride.Shaders.Parsers.Tests;

// Integer suffixes on every form of integer literal: a lone 0, decimal, hexadecimal.
public class IntegerLiteralSuffixTests
{
    static (IntegerLiteral? Literal, bool ConsumedAll) Parse(string text)
    {
        var scanner = new Scanner(text);
        var matched = new NumberParser().Match(ref scanner, new ParseResult(), out var parsed);
        return (matched ? parsed as IntegerLiteral : null, scanner.IsEof);
    }

    [Theory]
    [InlineData("0u", 0L)]
    [InlineData("0U", 0L)]
    [InlineData("1u", 1L)]
    [InlineData("0x10u", 16L)]
    [InlineData("0xFFFFFFFFu", 0xFFFFFFFFL)]
    public void UnsignedSuffixIsConsumed(string text, long expected)
    {
        var (literal, consumedAll) = Parse(text);
        Assert.NotNull(literal);
        Assert.Equal(expected, literal!.Value);
        Assert.False(literal.Suffix.Signed);
        Assert.True(consumedAll);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("0x10")]
    public void UnsuffixedLiteralIsUnchanged(string text)
    {
        var (literal, consumedAll) = Parse(text);
        Assert.NotNull(literal);
        Assert.True(consumedAll);
        Assert.Equal(text == "0", literal!.Suffix.Signed);
    }

    [Fact]
    public void ZeroWithSuffixParsesInAShader()
    {
        var result = SDSLParser.Parse("shader S { stage uint Output; void M(int c) { Output = c != 0 ? 1u : 0u; Output = 0x10u; } };");
        Assert.True(result.Errors.Count == 0, string.Join("\n", result.Errors.Select(x => x.ToString())));
    }
}
