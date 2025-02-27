using Stride.Shaders.Parsing.SDSL.AST;
using System.Globalization;

namespace Stride.Shaders.Parsing.SDSL;


public struct NumberParser : IParser<Literal>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out Literal parsed, in ParseError? orError = null)
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

    public static bool Integer<TScanner>(ref TScanner scanner, ParseResult result, out Literal parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        IntegerSuffixParser suffix = new();
        if (Tokens.Digit(ref scanner, 1.., advance: true))
        {
            while (Tokens.Digit(ref scanner, advance: true)) ;

            var numPos = scanner.Position;
            if (suffix.Match(ref scanner, null!, out Suffix suf))
            {
                parsed = new IntegerLiteral(suf, long.Parse(scanner.Span[position..numPos]), scanner[position..scanner.Position]);
                return true;
            }
            else
            {
                var memory = scanner.Memory[position..scanner.Position];
                parsed = new IntegerLiteral(new(32, false, true), long.Parse(memory.Span), new(scanner.Memory, position..scanner.Position));
                return true;
            }
        }
        else if (Tokens.Char('0', ref scanner, advance: true) && !Tokens.Digit(ref scanner, ..))
        {
            parsed = new IntegerLiteral(new(32, false, true), 0, new(scanner.Memory, position..scanner.Position));
            return true;
        }
        else return Parsers.Exit(ref scanner, result, out parsed, position, orError);
    }
    public static bool Float<TScanner>(ref TScanner scanner, ParseResult result, out Literal parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (Tokens.Char('.', ref scanner, advance: true))
        {
            if (!Tokens.Digit(ref scanner))
                return Parsers.Exit(ref scanner, result, out parsed, position);
            while (Tokens.Digit(ref scanner, advance: true)) ;
        }
        else if (Tokens.Digit(ref scanner, 1.., advance: true))
        {
            while (Tokens.Digit(ref scanner, advance: true)) ;
            if (Tokens.Char('.', ref scanner))
            {
                scanner.Advance(1);
                if (!Tokens.Digit(ref scanner) && !Tokens.FloatSuffix(ref scanner, out _))
                    return Parsers.Exit(ref scanner, result, out parsed, position, new(SDSLErrorMessages.SDSL0001, scanner[scanner.Position], scanner.Memory));
                while (Tokens.Digit(ref scanner, advance: true)) ;
            }
            else if (Tokens.FloatSuffix(ref scanner, out _) || Tokens.Char('e', ref scanner)) { }
            else return Parsers.Exit(ref scanner, result, out parsed, position);
        }
        else if (Tokens.Digit(ref scanner, 0, advance: true))
        {
            if (Tokens.Char('.', ref scanner, advance: true))
            {
                if (!Tokens.Digit(ref scanner) && !Tokens.FloatSuffix(ref scanner, out _))
                    return Parsers.Exit(ref scanner, result, out parsed, position, new(SDSLErrorMessages.SDSL0001, scanner[scanner.Position], scanner.Memory));
                while (Tokens.Digit(ref scanner, advance: true)) ;
            }
            else return Parsers.Exit(ref scanner, result, out parsed, position);
        }
        else return Parsers.Exit(ref scanner, result, out parsed, position);


        var value = double.Parse(scanner.Span[position..scanner.Position], CultureInfo.InvariantCulture);
        int? exponent = null;
        if (Tokens.Char('e', ref scanner, advance: true))
        {
            var signed = Tokens.AnyOf(["+", "-"], ref scanner, out var matched, advance: true);
            if (Integer(ref scanner, result, out var exp))
            {
                exponent = (int)((IntegerLiteral)exp).Value;
                if (signed && matched == "-")
                    exponent = -exponent;
            }
            else return Parsers.Exit(ref scanner, result, out parsed, position, new(SDSLErrorMessages.SDSL0001, scanner[scanner.Position], scanner.Memory));
        }
        if (Tokens.FloatSuffix(ref scanner, out var suffix, advance: true) && suffix is not null)
            parsed = new FloatLiteral(suffix.Value, value, exponent, scanner[position..scanner.Position]);
        else
            parsed = new FloatLiteral(new(32, true, true), value, exponent, scanner[position..scanner.Position]);
        return true;
    }
    public static bool Hex<TScanner>(ref TScanner scanner, ParseResult result, out Literal parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        parsed = null!;
        var position = scanner.Position;
        if (Tokens.Literal("0x", ref scanner, advance: true))
        {
            while (Tokens.Set("abcdefABCDEF", ref scanner, advance: true) || Tokens.Digit(ref scanner, advance: true)) ;

            ulong sum = 0;

            for (int i = 0; i < scanner.Position - position - 2; i += 1)
            {
                var v = Hex2int(scanner.Span[i]);
                var add = v * Math.Pow(16, i);
                if (ulong.MaxValue - sum < add)
                {
                    result.Errors.Add(new ParseError("Hex value bigger than ulong.", scanner[position], scanner.Memory));
                    return false;
                }
            }
            parsed = new HexLiteral(sum, scanner[position..scanner.Position]);
            return true;
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

