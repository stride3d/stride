using Stride.Shaders.Parsing.SDSL.AST;

namespace Stride.Shaders.Parsing.SDSL;


public interface ILiteralParser<TResult>
{
    public bool Match<TScanner>(ref TScanner scanner, ParseResult result, out TResult literal, in ParseError? error = null)
        where TScanner : struct, IScanner;
}

public record struct LiteralsParser : IParser<Literal>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out Literal literal, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (Vector(ref scanner, result, out var v, orError))
        {
            literal = v;
            return true;
        }
        else if (Matrix(ref scanner, result, out var m, orError))
        {
            literal = m;
            return true;
        }
        else if (Identifier(ref scanner, result, out var i, orError))
        {
            literal = i;
            return true;
        }
        else if (Number(ref scanner, result, out var n, orError))
        {
            literal = n;
            return true;
        }
        else return CommonParsers.Exit(ref scanner, result, out literal, position, orError);
    }
    public static bool Literal<TScanner>(ref TScanner scanner, ParseResult result, out Literal literal, in ParseError? orError = null)
        where TScanner : struct, IScanner
        => new LiteralsParser().Match(ref scanner, result, out literal, in orError);
    public static bool Identifier<TScanner>(ref TScanner scanner, ParseResult result, out Identifier identifier, in ParseError? orError = null)
        where TScanner : struct, IScanner
        => new IdentifierParser().Match(ref scanner, result, out identifier, orError);

    public static bool TypeName<TScanner>(ref TScanner scanner, ParseResult result, out TypeName typeName, in ParseError? orError = null)
        where TScanner : struct, IScanner
        => new TypeNameParser().Match(ref scanner, result, out typeName);

    public static bool Number<TScanner>(ref TScanner scanner, ParseResult result, out NumberLiteral number, in ParseError? orError = null)
        where TScanner : struct, IScanner
        => new NumberParser().Match(ref scanner, result, out number, in orError);
    public static bool Vector<TScanner>(ref TScanner scanner, ParseResult result, out VectorLiteral parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
        => new VectorParser().Match(ref scanner, result, out parsed, in orError);
    public static bool Matrix<TScanner>(ref TScanner scanner, ParseResult result, out MatrixLiteral parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
        => new MatrixParser().Match(ref scanner, result, out parsed, in orError);
    public static bool Integer<TScanner>(ref TScanner scanner, ParseResult result, out IntegerLiteral number, in ParseError? orError = null)
        where TScanner : struct, IScanner
        => new IntegerParser().Match(ref scanner, result, out number, in orError);

    public static bool AssignOperators<TScanner>(ref TScanner scanner, ParseResult result, out AssignOperator op, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        op = AssignOperator.NOp;
        if (
            Terminals.AnyOf(
                ["=", "+=", "-=", "*=", "/=", "%=", "&=", "|=", "^=", "<<=", ">>="],
                ref scanner,
                out var matched,
                advance: true
            )
        )
        {
            op = matched.ToAssignOperator();
            return true;
        }
        else return false;
    }

}


public record struct Suffix(int Size, bool IsFloatingPoint, bool Signed)
{
    public readonly override string ToString()
    {
        return (IsFloatingPoint, Signed) switch
        {
            (true, _) => $"f{Size}",
            (false, false) => $"u{Size}",
            (false, true) => $"i{Size}",
        };
    }
}

public readonly record struct FloatSuffixParser() : ILiteralParser<Suffix>
{
    public static bool TryMatchAndAdvance<TScanner>(ref TScanner scanner, string match)
        where TScanner : struct, IScanner
    {
        if (Terminals.Literal<TScanner>(match, ref scanner))
        {
            scanner.Advance(match.Length);
            return true;
        }
        return false;
    }

    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out Suffix suffix, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        suffix = new(32, false, false);
        if (Terminals.AnyOf(["f16", "f32", "f64", "d", "h"], ref scanner, out var matched, advance: true))
        {
            suffix = matched switch
            {
                "f16" or "h" => new(16, true, true),
                "f32" => new(32, true, true),
                "f64" or "d" => new(64, true, true),

                _ => throw new NotImplementedException()
            };
            return true;
        }
        else return false;
    }
}

public readonly record struct IntegerSuffixParser() : ILiteralParser<Suffix>
{
    public static bool TryMatchAndAdvance<TScanner>(ref TScanner scanner, string match)
        where TScanner : struct, IScanner
    {
        if (Terminals.Literal(match, ref scanner))
        {
            scanner.Advance(match.Length);
            return true;
        }
        return false;
    }

    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out Suffix suffix, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        suffix = new(32, false, false);
        if (Terminals.AnyOf(["u8", "u16", "u32", "u64", "i8", "i16", "i32", "i64", "U", "L"], ref scanner, out var matched, advance: true))
        {
            suffix = matched switch
            {
                "u8" => new(8, false, false),
                "u16" => new(16, false, false),
                "u32" => new(32, false, false),
                "u64" => new(64, false, false),
                "i8" => new(8, false, true),
                "i16" => new(16, false, true),
                "i32" => new(32, false, true),
                "i64" => new(64, false, true),
                "U" => new(32, false, false),
                "L" => new(32, false, true),
                _ => throw new NotImplementedException()
            };
            return true;
        }
        else return false;
    }
}


public record struct IdentifierParser() : ILiteralParser<Identifier>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out Identifier literal, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        literal = null!;
        var position = scanner.Position;
        if (Terminals.Char('_', ref scanner) || Terminals.Letter(ref scanner))
        {
            scanner.Advance(1);
            while (Terminals.LetterOrDigit(ref scanner) || Terminals.Char('_', ref scanner))
                scanner.Advance(1);
            var id = scanner.Memory[position..scanner.Position].ToString();
            if (Reserved.Keywords.Contains(id))
                return CommonParsers.Exit(ref scanner, result, out literal, position, orError);
            literal = new(id, scanner.GetLocation(position, scanner.Position - position));
            return true;
        }
        else return false;
    }
}

public record struct TypeNameParser() : ILiteralParser<TypeName>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out TypeName name, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        name = null!;
        var position = scanner.Position;
        if (Terminals.Char('_', ref scanner) || Terminals.Letter(ref scanner))
        {
            scanner.Advance(1);
            while (Terminals.LetterOrDigit(ref scanner) || Terminals.Char('_', ref scanner))
                scanner.Advance(1);
            var identifier = new Identifier(scanner.Memory[position..scanner.Position].ToString(), scanner.GetLocation(position, scanner.Position - position));

            var intermediate = scanner.Position;
            if (
                CommonParsers.Spaces0(ref scanner, result, out _)
                && Terminals.Char('[', ref scanner, advance: true)
                && CommonParsers.Spaces0(ref scanner, result, out _)
                && CommonParsers.Optional(ref scanner, new ExpressionParser(), result, out _)
                && CommonParsers.Spaces0(ref scanner, result, out _)
                && Terminals.Char(']', ref scanner, advance: true)
            )
            {
                name = new TypeName(scanner.Memory[position..scanner.Position].ToString().Trim(), scanner.GetLocation(position..scanner.Position), isArray: true);
                return true;
            }
            else
            {
                scanner.Position = intermediate;
                name = new(identifier.Name, scanner.GetLocation(position..scanner.Position), isArray : false);
                return true;
            }
        }
        else return false;
    }
}




public record struct VectorParser : IParser<VectorLiteral>
{

    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out VectorLiteral parsed, in ParseError? orError = null) where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (
            Terminals.AnyOf(["bool", "half", "float", "double", "short", "ushort", "int", "uint", "long", "ulong"], ref scanner, out var baseType, advance: true)
            && Terminals.Digit(ref scanner, 2..4, advance: true)
        )
        {
            var tnPos = scanner.Position;
            int size = scanner.Span[scanner.Position - 1] - '0';
            if (size < 2 || size > 4)
                return CommonParsers.Exit(ref scanner, result, out parsed, position, new(SDSLParsingMessages.SDSL0002, scanner.GetErrorLocation(scanner.Position - 1), scanner.Memory));
            CommonParsers.Spaces0(ref scanner, result, out _);
            if (Terminals.Char('(', ref scanner, advance: true))
            {
                var p = new VectorLiteral(new TypeName(scanner.Memory[position..tnPos].ToString(), scanner.GetLocation(position..tnPos), isArray: false), scanner.GetLocation(..))
                {
                    TypeName = new(baseType, scanner.GetLocation((tnPos - baseType.Length)..(tnPos - 1)), isArray: false)
                };
                while (!scanner.IsEof)
                {
                    CommonParsers.Spaces0(ref scanner, result, out _);
                    if (LiteralsParser.Number(ref scanner, result, out var number))
                        p.Values.Add(number);
                    else if (LiteralsParser.Vector(ref scanner, result, out var vec))
                        p.Values.Add(vec);
                    else if (ExpressionParser.Expression(ref scanner, result, out var exp))
                        p.Values.Add(exp);
                    else return CommonParsers.Exit(ref scanner, result, out parsed, position, new(SDSLParsingMessages.SDSL0001, scanner.GetErrorLocation(scanner.Position), scanner.Memory));
                    CommonParsers.Spaces0(ref scanner, result, out _);
                    if (Terminals.Char(',', ref scanner, advance: true))
                        CommonParsers.Spaces0(ref scanner, result, out _);
                    else if (Terminals.Char(')', ref scanner, advance: true))
                        break;
                }
                if (scanner.IsEof)
                    return CommonParsers.Exit(ref scanner, result, out parsed, position, new(SDSLParsingMessages.SDSL0004, scanner.GetErrorLocation(scanner.Position), scanner.Memory));
                if (p.Values.Count != size && p.Values.Count > size)
                    return CommonParsers.Exit(ref scanner, result, out parsed, position, new(SDSLParsingMessages.SDSL0005, scanner.GetErrorLocation(scanner.Position), scanner.Memory));
                parsed = p;
                return true;
            }

        }
        return CommonParsers.Exit(ref scanner, result, out parsed, position, orError);
    }
}

public record struct MatrixParser : IParser<MatrixLiteral>
{

    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out MatrixLiteral parsed, in ParseError? orError = null) where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (
            Terminals.AnyOf(["bool", "half", "float", "double", "short", "ushort", "int", "uint", "long", "ulong"], ref scanner, out var baseType, advance: true)
            && Terminals.Digit(ref scanner, 2..4, advance: true)
            && Terminals.Char('x', ref scanner, advance: true)
            && Terminals.Digit(ref scanner, 2..4, advance: true)
        )
        {
            var tnPos = scanner.Position;
            int rows = scanner.Span[scanner.Position - 3] - '0';
            int cols = scanner.Span[scanner.Position - 1] - '0';
            if (cols < 2 || cols > 4 || rows < 2 || rows > 4)
                return CommonParsers.Exit(ref scanner, result, out parsed, position, new(SDSLParsingMessages.SDSL0006, scanner.GetErrorLocation(scanner.Position - 1), scanner.Memory));
            CommonParsers.Spaces0(ref scanner, result, out _);
            if (Terminals.Char('(', ref scanner, advance: true))
            {
                var p = new MatrixLiteral<ValueLiteral>(new TypeName(scanner.Memory[position..tnPos].ToString(), scanner.GetLocation(position..tnPos), isArray: false), rows, cols, scanner.GetLocation(..))
                {
                    TypeName = new(baseType, scanner.GetLocation((tnPos - baseType.Length)..(tnPos - 1)), isArray: false)
                };
                while (!scanner.IsEof)
                {
                    CommonParsers.Spaces0(ref scanner, result, out _);
                    if (LiteralsParser.Number(ref scanner, result, out var number))
                        p.Values.Add(number);
                    else if (LiteralsParser.Vector(ref scanner, result, out var vector, new(SDSLParsingMessages.SDSL0007, scanner.GetErrorLocation(scanner.Position), scanner.Memory)))
                        p.Values.Add(vector);
                    else return CommonParsers.Exit(ref scanner, result, out parsed, position, orError);
                    CommonParsers.Spaces0(ref scanner, result, out _);
                    if (Terminals.Char(',', ref scanner, advance: true))
                        CommonParsers.Spaces0(ref scanner, result, out _);
                    else if (Terminals.Char(')', ref scanner, advance: true))
                        break;
                }
                if (scanner.IsEof)
                    return CommonParsers.Exit(ref scanner, result, out parsed, position, new(SDSLParsingMessages.SDSL0008, scanner.GetErrorLocation(scanner.Position), scanner.Memory));
                if (p.Values.Count != rows * cols && p.Values.Count > rows * cols)
                    return CommonParsers.Exit(ref scanner, result, out parsed, position, new(SDSLParsingMessages.SDSL0002, scanner.GetErrorLocation(scanner.Position), scanner.Memory));
                parsed = p;
                return true;
            }

        }
        return CommonParsers.Exit(ref scanner, result, out parsed, position, orError);
    }
}