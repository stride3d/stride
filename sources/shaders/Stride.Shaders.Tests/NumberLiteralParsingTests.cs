using Stride.Shaders.Parsing;
using Stride.Shaders.Parsing.SDSL;
using Stride.Shaders.Parsing.SDSL.AST;

namespace Stride.Shaders.Parsers.Tests;

// Direct, literal-level tests for NumberParser.Float.
//
// These tests intentionally bypass full shader parsing (unlike ParsingTests.ParseFile) so that
// float literal grammar behavior can be validated in isolation: the error message and location
// produced when parsing a shader depend on the surrounding expression/statement context (e.g.
// "1.q" errors differently depending on where it is placed in a shader), which makes it hard to
// assert precise pass/fail semantics for the literal grammar itself.
//
// Grammar reference (HLSL floating-point numbers):
// https://learn.microsoft.com/en-us/windows/win32/direct3dhlsl/dx-graphics-hlsl-appendix-grammar#floating-point-numbers
//
//   Float-Literal ::= Digits '.' Digits? Exponent? Float-Suffix?
//                    | Digits Exponent Float-Suffix?
//                    | '.' Digits Exponent? Float-Suffix?
//                    | Digits Float-Suffix
//   Exponent ::= ('e' | 'E') ('+' | '-')? Digits
//   Float-Suffix ::= 'f' | 'F' | 'h' | 'H' | 'l' | 'L'
public class NumberLiteralParsingTests
{
    // Runs NumberParser.Float directly against the given text and returns the parsed literal
    // (or null on failure) along with whether the scanner consumed the whole input.
    static (FloatLiteral? Literal, bool ConsumedAll, bool Matched) ParseFloat(string text)
    {
        var scanner = new Scanner(text);
        var result = new ParseResult();
        var matched = NumberParser.Float(ref scanner, result, out var parsed);
        return (parsed as FloatLiteral, scanner.IsEof, matched);
    }

    public static IEnumerable<object[]> ValidFloatLiterals()
    {
        // digit-sequence '.' digit-sequence(opt)
        yield return ["0.", 0.0];
        yield return ["1.", 1.0];
        yield return ["1.0", 1.0];
        yield return ["123.456", 123.456];
        // '.' digit-sequence
        yield return [".5", 0.5];
        yield return [".0", 0.0];
        // digit-sequence exponent-part (no dot required)
        yield return ["1e2", 1e2];
        yield return ["1E2", 1e2];
        yield return ["1e+2", 1e2];
        yield return ["1e-2", 1e-2];
        // fractional-constant exponent-part
        yield return ["1.5e2", 1.5e2];
        yield return ["1.e2", 1.0e2];
        yield return [".5e2", 0.5e2];
        // floating-suffix
        // NOTE: the official HLSL grammar treats the floating-suffix as case-insensitive
        // ('f' | 'F' | 'h' | 'H' | 'l' | 'L'), but the SDSL parser (Tokens.FloatSuffix) currently
        // only recognizes the lowercase forms below (plus explicit sizes "f16"/"f32"/"f64"/"d").
        // Uppercase suffixes are covered as a negative case in UppercaseFloatSuffixIsNotRecognized.
        yield return ["1.f", 1.0];
        yield return ["1.h", 1.0];
        yield return ["1.0f", 1.0];
        yield return ["1e2f", 1e2];
        // digit-sequence floating-suffix (no dot, no exponent)
        yield return ["1f", 1.0];
        yield return ["1h", 1.0];
    }

    [Theory]
    [MemberData(nameof(ValidFloatLiterals))]
    public void ValidFloatLiteralParsesAndConsumesWholeInput(string text, double expected)
    {
        var (literal, consumedAll, matched) = ParseFloat(text);
        Assert.True(matched, $"Expected '{text}' to be parsed as a valid float literal, but it was rejected.");
        Assert.NotNull(literal);
        Assert.Equal(expected, literal!.Value, precision: 10);
        Assert.True(consumedAll, $"Expected '{text}' to be fully consumed by the float literal parser, but position stopped before the end.");
    }

    // Cases that must NOT be accepted as a complete, self-contained float literal: either the
    // parser rejects them outright, or it only consumes a valid prefix and leaves trailing
    // garbage unconsumed (which the surrounding parser is responsible for reporting as an error).
    public static IEnumerable<object[]> IncompleteOrInvalidFloatLiterals()
    {
        yield return ["."];       // lone dot, no digits at all
        yield return ["1e"];      // exponent with no digits
        yield return ["1e+"];     // exponent sign with no digits
        yield return ["1e-"];     // exponent sign with no digits
        yield return ["1.q"];     // trailing dot followed by an invalid suffix character
        yield return ["1.0q"];    // valid mantissa followed by an invalid suffix character
        yield return ["1eq"];     // 'e' not followed by a valid exponent
        yield return ["1.0fx"];   // valid float literal followed by garbage after the suffix
    }

    [Theory]
    [MemberData(nameof(IncompleteOrInvalidFloatLiterals))]
    public void InvalidOrIncompleteFloatLiteralIsNotFullyConsumed(string text)
    {
        var (_, consumedAll, matched) = ParseFloat(text);
        // Either the parser fails to match entirely, or it matches only a partial prefix and
        // therefore does not consume the whole (invalid) input. Either way, the literal grammar
        // must not silently swallow the invalid trailing text as part of a valid float literal.
        Assert.False(matched && consumedAll,
            $"Expected '{text}' to either fail to parse as a float literal, or to leave trailing characters unconsumed, but the whole input was accepted as a single float literal.");
    }

    [Fact]
    public void LoneDotIsNotAValidFloatLiteral()
    {
        var (literal, _, matched) = ParseFloat(".");
        Assert.False(matched);
        Assert.Null(literal);
    }

    [Theory]
    [InlineData("1e")]
    [InlineData("1e+")]
    [InlineData("1e-")]
    public void ExponentWithoutDigitsFailsToParse(string text)
    {
        var (literal, _, matched) = ParseFloat(text);
        Assert.False(matched, $"Expected '{text}' to fail because the exponent has no digits.");
        Assert.Null(literal);
    }

    [Theory]
    [InlineData("1.q", "1.")]
    [InlineData("1.0q", "1.0")]
    [InlineData("1.0fx", "1.0f")]
    public void TrailingGarbageAfterValidFloatPrefixIsLeftUnconsumed(string text, string validPrefix)
    {
        var (literal, _, matched) = ParseFloat(text);
        Assert.True(matched, $"Expected the valid prefix of '{text}' to still be parsed as a float literal.");
        Assert.NotNull(literal);
        var scanner = new Scanner(text);
        var result = new ParseResult();
        NumberParser.Float(ref scanner, result, out _);
        Assert.Equal(validPrefix.Length, scanner.Position);
    }

    // Regression test: a trailing-dot float literal (e.g. "1.") is valid per the HLSL grammar,
    // but a garbage character directly following it (e.g. "1.q") should still be reported as a
    // parse error once the invalid shader as a whole is parsed. See ParsingTests for the
    // full-shader version of this check.
    [Fact]
    public void TrailingDotFloatLiteralParsesAsExpectedValue()
    {
        var (literal, consumedAll, matched) = ParseFloat("1.");
        Assert.True(matched);
        Assert.NotNull(literal);
        Assert.Equal(1.0, literal!.Value, precision: 10);
        Assert.True(consumedAll);
    }

    // Known gap vs. the official HLSL grammar: floating-suffix is defined case-insensitively
    // ('f' | 'F' | 'h' | 'H' | 'l' | 'L'), but Tokens.FloatSuffix only recognizes lowercase 'f'
    // and 'h' (plus explicit-size suffixes "f16"/"f32"/"f64"/"d"). As a result "1.F" and "1.H"
    // do not parse as a complete float literal with a suffix: only the "1." prefix is consumed
    // and the trailing 'F'/'H' is left for the surrounding parser to (mis)interpret.
    [Theory]
    [InlineData("1.F")]
    [InlineData("1.H")]
    public void UppercaseFloatSuffixIsNotRecognizedByFloatParser(string text)
    {
        var (literal, consumedAll, matched) = ParseFloat(text);
        Assert.True(matched, $"Expected the '{text[..^1]}' prefix of '{text}' to still parse as a float literal.");
        Assert.NotNull(literal);
        Assert.False(consumedAll, $"Expected the uppercase suffix in '{text}' to be left unconsumed, but the whole input was consumed.");
    }
}
