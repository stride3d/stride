using Stride.Shaders.Parsing.SDSL.AST;

namespace Stride.Shaders.Parsing.SDSL;


public record struct PrefixParser : IParser<Expression>
{
    public static bool Prefix<TScanner>(ref TScanner scanner, ParseResult result, out Expression prefix, in ParseError? orError = null)
        where TScanner : struct, IScanner
        => new PrefixParser().Match(ref scanner, result, out prefix, in orError);


    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out Expression parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        return CommonParsers.Alternatives(
            ref scanner, 
            result, 
            out parsed, 
            orError,
            PrefixIncrement,
            Signed,
            Not,
            Cast,
            PostfixParser.Postfix
        );
    }

    public static bool Not<TScanner>(ref TScanner scanner, ParseResult result, out Expression parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (Terminals.Set("!~", ref scanner))
        {
            var op = ((char)scanner.Peek()).ToOperator();
            scanner.Advance(1);
            CommonParsers.Spaces0(ref scanner, result, out _);
            if (PostfixParser.Postfix(ref scanner, result, out var lit))
            {
                parsed = new PrefixExpression(op, lit, scanner.GetLocation(position, scanner.Position - position));
                return true;
            }
            else return CommonParsers.Exit(ref scanner, result, out parsed, position, new(SDSLParsingMessages.SDSL0020, scanner.GetErrorLocation(position), scanner.Memory));

        }
        else return CommonParsers.Exit(ref scanner, result, out parsed, position, orError);
    }

    public static bool Signed<TScanner>(ref TScanner scanner, ParseResult result, out Expression parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (Terminals.Set("+-", ref scanner))
        {
            var op = ((char)scanner.Peek()).ToOperator();
            scanner.Advance(1);
            CommonParsers.Spaces0(ref scanner, result, out _);
            if (Prefix(ref scanner, result, out var lit))
            {
                parsed = new PrefixExpression(op, lit, scanner.GetLocation(position, scanner.Position - position));
                return true;
            }
            else return CommonParsers.Exit(ref scanner, result, out parsed, position, orError);
        }
        return CommonParsers.Exit(ref scanner, result, out parsed, position, orError);
    }
    
    public static bool PrefixIncrement<TScanner>(ref TScanner scanner, ParseResult result, out Expression parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (Terminals.Literal("++", ref scanner, advance: true))
        {
            CommonParsers.Spaces0(ref scanner, result, out _);
            if (PostfixParser.Postfix(ref scanner, result, out var lit))
            {
                parsed = new PrefixExpression(Operator.Inc, lit, scanner.GetLocation(position, scanner.Position - position));
                return true;
            }
            else return CommonParsers.Exit(ref scanner, result, out parsed, position, new(SDSLParsingMessages.SDSL0020, scanner.GetErrorLocation(position), scanner.Memory));
        }
        // prefix decrememnt 
        else if (Terminals.Literal("--", ref scanner, advance: true))
        {
            CommonParsers.Spaces0(ref scanner, result, out _);
            if (PostfixParser.Postfix(ref scanner, result, out var lit))
            {
                parsed = new PrefixExpression(Operator.Inc, lit, scanner.GetLocation(position, scanner.Position - position));
                return true;
            }
            else return CommonParsers.Exit(ref scanner, result, out parsed, position, new(SDSLParsingMessages.SDSL0020, scanner.GetErrorLocation(position), scanner.Memory));

        }
        else return CommonParsers.Exit(ref scanner, result, out parsed, position, orError);
    }

    public static bool Cast<TScanner>(ref TScanner scanner, ParseResult result, out Expression parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (
                CommonParsers.FollowedBy(ref scanner, Terminals.Char('('), withSpaces: true, advance: true)
                && CommonParsers.FollowedBy(ref scanner, result, LiteralsParser.TypeName, out TypeName typeName, withSpaces: true, advance: true)
                && CommonParsers.FollowedBy(ref scanner, Terminals.Char(')'), withSpaces: true, advance: true)
                && CommonParsers.FollowedBy(ref scanner, result, PostfixParser.Postfix, out Expression expression, withSpaces: true, advance: true)
        )
        {
            parsed = new CastExpression(typeName.Name, Operator.Cast, expression, scanner.GetLocation(position, scanner.Position - position));
            return true;
        }
        else return CommonParsers.Exit(ref scanner, result, out parsed, position, orError);
    }
}
