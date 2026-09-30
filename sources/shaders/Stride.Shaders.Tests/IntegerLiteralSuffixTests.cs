// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Shaders.Core;
using Stride.Shaders.Parsing;
using Stride.Shaders.Parsing.SDSL;
using Stride.Shaders.Parsing.SDSL.AST;
using Stride.Shaders.Spirv.Building;

namespace Stride.Shaders.Parsers.Tests;

// Integer literals in every form (lone 0, decimal, octal, hexadecimal) with every suffix, typed and wrapped as DXC does:
// HLSL's long is 32 bits, so l and ul are int and uint, ll and ull are 64 bits, and a value too large for its suffix wraps.
public class IntegerLiteralSuffixTests
{
    public static TheoryData<string, object> ValidLiterals => new()
    {
        // Lone 0
        { "0", 0 },
        { "0u", 0u },
        { "0U", 0u },
        { "0l", 0 },
        { "0ul", 0u },
        { "0ll", 0L },
        { "0ull", 0UL },
        // Decimal without suffix: the first of int, uint, long, ulong that holds the value
        { "1", 1 },
        { "2147483647", int.MaxValue },
        { "2147483648", 2147483648u },
        { "4294967295", uint.MaxValue },
        { "4294967296", 4294967296L },
        { "9223372036854775807", long.MaxValue },
        { "9223372036854775808", 9223372036854775808UL },
        { "18446744073709551615", ulong.MaxValue },
        // Decimal with a suffix
        { "1u", 1u },
        { "1U", 1u },
        { "1l", 1 },
        { "1L", 1 },
        { "1ul", 1u },
        { "1UL", 1u },
        { "1lu", 1u },
        { "1LU", 1u },
        { "1ll", 1L },
        { "1LL", 1L },
        { "1ull", 1UL },
        { "1ULL", 1UL },
        { "1llu", 1UL },
        { "1u32", 1u },
        { "1i32", 1 },
        { "1u64", 1UL },
        { "1i64", 1L },
        { "4294967295u", uint.MaxValue },
        { "4294967296u", 0u },
        { "5000000000u", 705032704u },
        { "5000000000ul", 705032704u },
        { "2147483648l", int.MinValue },
        { "5000000000l", 705032704 },
        { "5000000000ll", 5000000000L },
        { "18446744073709551615ull", ulong.MaxValue },
        // Hexadecimal without suffix: uint, or ulong above 32 bits
        { "0x10", 16u },
        { "0X10", 16u },
        { "0x80000000", 0x80000000u },
        { "0x100000000", 0x100000000UL },
        { "0xFFFFFFFFFFFFFFFF", ulong.MaxValue },
        // Hexadecimal with a suffix
        { "0x10u", 16u },
        { "0x1Ful", 31u },
        { "0xFFFFFFFFu", uint.MaxValue },
        { "0x100000000u", 0u },
        { "0x7FFFFFFFl", int.MaxValue },
        { "0x80000000L", 0x80000000u },
        { "0xFFFFFFFFl", uint.MaxValue },
        { "0x100000000l", 0u },
        { "0xFFFFFFFFFFFFFFFFll", -1L },
        { "0xFFFFFFFFFFFFFFFFull", ulong.MaxValue },
        { "0xFFFFFFFFi32", -1 },
        // Octal
        { "00", 0 },
        { "010", 8 },
        { "017u", 15u },
        { "037777777777", uint.MaxValue },
        { "020000000000l", 0x80000000u },
    };

    [Theory]
    [MemberData(nameof(ValidLiterals))]
    public void LiteralHasTheTypeAndValueOfItsSuffix(string text, object expected)
    {
        var scanner = new Scanner(text);
        var result = new ParseResult();
        Assert.True(new NumberParser().Match(ref scanner, result, out var parsed), string.Join("\n", result.Errors));
        Assert.Empty(result.Errors);
        Assert.True(scanner.IsEof, $"'{text}' was not fully consumed");

        var literal = Assert.IsAssignableFrom<IntegerLiteral>(parsed);
        AssertConstant(literal, expected);
    }

    public static TheoryData<string, object> NegatedLiterals => new()
    {
        // A suffixed literal keeps its type and wraps
        { "-1u", uint.MaxValue },
        { "-1l", -1 },
        { "-1ll", -1L },
        { "-1ull", ulong.MaxValue },
        { "-0x10u", 0xFFFFFFF0u },
        // An unsuffixed literal stays signed
        { "-1", -1 },
        { "-2147483648", int.MinValue },
        { "-2147483649", -2147483649L },
        { "-9223372036854775808", long.MinValue },
        { "-0x10", -16 },
        { "-0x80000000", int.MinValue },
        { "-010", -8 },
    };

    [Theory]
    [MemberData(nameof(NegatedLiterals))]
    public void NegatedLiteralIsFolded(string text, object expected)
    {
        var scanner = new Scanner(text);
        var result = new ParseResult();
        Assert.True(ExpressionParser.Expression(ref scanner, result, out var parsed), string.Join("\n", result.Errors));
        Assert.Empty(result.Errors);

        var literal = Assert.IsAssignableFrom<IntegerLiteral>(parsed);
        AssertConstant(literal, expected);
    }

    // Parse errors rather than exceptions: HLSL has no 8 or 16-bit integer literal suffix, and a value must fit in 64 bits
    [Theory]
    [InlineData("0u8", "8-bit and 16-bit")]
    [InlineData("0u16", "8-bit and 16-bit")]
    [InlineData("0xFFi16", "8-bit and 16-bit")]
    [InlineData("1i8", "8-bit and 16-bit")]
    [InlineData("1ux", "Invalid suffix 'ux'")]
    [InlineData("1uu", "Invalid suffix 'uu'")]
    [InlineData("1lL", "Invalid suffix 'lL'")]
    [InlineData("0x10q", "Invalid suffix 'q'")]
    [InlineData("18446744073709551616", "too large")]
    [InlineData("0x10000000000000000", "bigger than ulong")]
    [InlineData("09", null)]
    [InlineData("0x", null)]
    public void InvalidLiteralIsAParseError(string text, string? message)
    {
        var scanner = new Scanner(text);
        var result = new ParseResult();
        Assert.False(new NumberParser().Match(ref scanner, result, out _));
        var error = Assert.Single(result.Errors);
        if (message is not null)
            Assert.Contains(message, error.Message);
    }

    [Fact]
    public void LiteralsParseInAShader()
    {
        var result = SDSLParser.Parse("shader S { stage uint Output; void M(int c) { Output = c != 0 ? 1u : 0u; Output = 0x10u + 0x80000000L + 017 + 0ul; } };");
        Assert.True(result.Errors.Count == 0, string.Join("\n", result.Errors.Select(x => x.ToString())));
    }

    [Fact]
    public void UnsupportedSuffixIsReportedInAShader()
    {
        var result = SDSLParser.Parse("shader S { stage uint Output; void M() { Output = 0u16; } };");
        Assert.Contains(result.Errors, e => e.Message.Contains("8-bit and 16-bit"));
    }

    // The type of the literal, its value as that type, and the SPIR-V constant it compiles to
    static void AssertConstant(IntegerLiteral literal, object expected)
    {
        var type = SpirvContext.ComputeLiteralType(literal);
        object value = type.Type switch
        {
            Scalar.Int => literal.IntValue,
            Scalar.UInt => literal.UIntValue,
            Scalar.Int64 => literal.LongValue,
            Scalar.UInt64 => literal.ULongValue,
            _ => throw new InvalidOperationException($"Unexpected literal type {type}"),
        };
        // Assert.Equal on object compares the type too
        Assert.Equal(expected, value);
        Assert.Equal(Convert.ToDouble(expected), literal.DoubleValue);

        var context = new SpirvContext();
        context.CompileConstantLiteral(literal);
        var constant = Assert.Single(context.LiteralConstants).Key;
        Assert.Equal<SymbolType>(type, constant.Type);
        Assert.Equal(expected, constant.Value);
    }
}
