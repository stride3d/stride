using System.Diagnostics.CodeAnalysis;
using Stride.Shaders.Parsing.SDSL.AST;
using System.Globalization;

namespace Stride.Shaders.Parsing.SDSL;


public struct NumberParser : IParser<Literal>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, [MaybeNullWhen(false)] out Literal parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        return Parsers.Alternatives(
            ref scanner,
            result,
            out parsed,
            orError,
            Hex,
            Float,
            Integer

        );
    }

    public static bool Integer<TScanner>(ref TScanner scanner, ParseResult result, [MaybeNullWhen(false)] out Literal parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (Tokens.Digit(ref scanner, advance: true))
        {
            while (Tokens.Digit(ref scanner, advance: true)) ;

            // A leading 0 makes an octal literal, as in C
            var digits = scanner.Span[position..scanner.Position];
            var octal = digits.Length > 1 && digits[0] == '0';
            ulong value = 0;
            for (int i = 0; i < digits.Length; i++)
            {
                var digit = (uint)(digits[i] - '0');
                if (octal && digit > 7)
                    return Parsers.Exit(ref scanner, result, out parsed, position, new(SDSLErrorMessages.SDSL0001, scanner[position + i], scanner.Memory));
                var radix = octal ? 8UL : 10UL;
                if (value > (ulong.MaxValue - digit) / radix)
                    return Parsers.Exit(ref scanner, result, out parsed, position, new("Integer literal is too large to be represented in any integer type.", scanner[position], scanner.Memory));
                value = value * radix + digit;
            }

            return IntegerWithSuffix(ref scanner, result, out parsed, position, value, octal, hex: false);
        }
        else return Parsers.Exit(ref scanner, result, out parsed, position, orError);
    }

    // Reads the suffix of an integer literal and types its value as DXC does, with HLSL's long being 32 bits:
    // u and ul wrap to uint, l to int (or uint for a hexadecimal or octal value above int), ll to int64 and ull to uint64.
    static bool IntegerWithSuffix<TScanner>(ref TScanner scanner, ParseResult result, [MaybeNullWhen(false)] out Literal parsed, int position, ulong value, bool octal, bool hex)
        where TScanner : struct, IScanner
    {
        var suffixPosition = scanner.Position;
        if (new IntegerSuffixParser().Match(ref scanner, result, out Suffix suffix))
        {
            var isLong = scanner.Span[suffixPosition..scanner.Position] is "l" or "L";
            if (isLong && (hex || octal) && value > int.MaxValue)
                suffix = suffix with { Signed = false };
            parsed = new IntegerLiteral(suffix, unchecked((long)value), scanner[position..scanner.Position]);
            return true;
        }

        // Letters right after the digits are a suffix this parser does not know
        while (Tokens.LetterOrDigit(ref scanner, advance: true) || Tokens.Char('_', ref scanner, advance: true)) ;
        if (scanner.Position > suffixPosition)
        {
            var text = scanner.Span[suffixPosition..scanner.Position];
            var message = text is "u8" or "u16" or "i8" or "i16"
                ? $"8-bit and 16-bit integer literals ('{text}' suffix) are not supported."
                : $"Invalid suffix '{text}' on integer literal.";
            return Parsers.Exit(ref scanner, result, out parsed, position, new(message, scanner[suffixPosition], scanner.Memory));
        }

        parsed = hex
            ? new HexLiteral(value, scanner[position..scanner.Position])
            : IntegerLiteral.FromUnsuffixed(value, scanner[position..scanner.Position]);
        return true;
    }
    public static bool Float<TScanner>(ref TScanner scanner, ParseResult result, [MaybeNullWhen(false)] out Literal parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (Tokens.Char('.', ref scanner, advance: true))
        {
            if (!Tokens.Digit(ref scanner))
                return Parsers.Exit(ref scanner, result, out parsed, position);
            while (Tokens.Digit(ref scanner, advance: true)) ;
        }
        else if (Tokens.Digit(ref scanner, advance: true))
        {
            while (Tokens.Digit(ref scanner, advance: true)) ;
            if (Tokens.Char('.', ref scanner))
            {
                scanner.Advance(1);
                while (Tokens.Digit(ref scanner, advance: true)) ;
            }
            else if (Tokens.FloatSuffix(ref scanner, out _) || Tokens.Char('e', ref scanner) || Tokens.Char('E', ref scanner)) { }
            else return Parsers.Exit(ref scanner, result, out parsed, position);
        }
        else return Parsers.Exit(ref scanner, result, out parsed, position);


        var value = double.Parse(scanner.Span[position..scanner.Position], CultureInfo.InvariantCulture);
        if (Tokens.Char('e', ref scanner, advance: true) || Tokens.Char('E', ref scanner, advance: true))
        {
            Tokens.AnyOf(["+", "-"], ref scanner, out _, advance: true);
            if (Tokens.Digit(ref scanner, advance: true))
            {
                while (Tokens.Digit(ref scanner, advance: true)) ;
                // Re-parse the whole mantissa+exponent span so the exponent is folded into the value.
                value = double.Parse(scanner.Span[position..scanner.Position], CultureInfo.InvariantCulture);
            }
            else return Parsers.Exit(ref scanner, result, out parsed, position, new(SDSLErrorMessages.SDSL0001, scanner[scanner.Position], scanner.Memory));
        }
        if (Tokens.FloatSuffix(ref scanner, out var suffix, advance: true) && suffix is not null)
            parsed = new FloatLiteral(suffix.Value, value, scanner[position..scanner.Position]);
        else
            parsed = new FloatLiteral(new(32, true, true), value, scanner[position..scanner.Position]);
        return true;
    }
    public static bool Hex<TScanner>(ref TScanner scanner, ParseResult result, [MaybeNullWhen(false)] out Literal parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        parsed = null!;
        var position = scanner.Position;
        if (Tokens.Literal("0x", ref scanner, advance: true) || Tokens.Literal("0X", ref scanner, advance: true))
        {
            while (Tokens.Set("abcdefABCDEF", ref scanner, advance: true) || Tokens.Digit(ref scanner, advance: true)) ;
            if (scanner.Position == position + 2)
                return Parsers.Exit(ref scanner, result, out parsed, position, new(SDSLErrorMessages.SDSL0001, scanner[scanner.Position], scanner.Memory));

            ulong sum = 0;

            for (int i = position + 2; i < scanner.Position; i++)
            {
                // Check if multiplying by 16 would not overflow ulong
                if ((sum >> 60) != 0)
                    return Parsers.Exit(ref scanner, result, out parsed, position, new("Hex value bigger than ulong.", scanner[i], scanner.Memory));

                sum <<= 4;
                var v = Hex2int(scanner.Span[i]);
                sum += (uint)v;
            }
            return IntegerWithSuffix(ref scanner, result, out parsed, position, sum, octal: false, hex: true);
        }
        else return Parsers.Exit(ref scanner, result, out parsed, position, orError);
    }
    static int Hex2int(char ch)
    {
        if (ch >= '0' && ch <= '9')
            return ch - '0';
        if (ch >= 'A' && ch <= 'F')
            return ch - 'A' + 10;
        if (ch >= 'a' && ch <= 'f')
            return ch - 'a' + 10;
        return -1;
    }
}

