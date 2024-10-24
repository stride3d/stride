using Stride.Shaders.Parsing.SDSL.AST;

namespace Stride.Shaders.Parsing.SDSL;


public record struct PrefixParser : IParser<Expression>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out Expression parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (UnaryParsers.PrefixIncrement(ref scanner, result, out parsed))
            return true;
        else if (UnaryParsers.Signed(ref scanner, result, out parsed))
            return true;
        // prefix not
        else if (UnaryParsers.Not(ref scanner, result, out parsed))
            return true;
        // prefix cast 
        else if (UnaryParsers.Cast(ref scanner, result, out parsed))
            return true;
        else if (UnaryParsers.Postfix(ref scanner, result, out var p))
        {
            parsed = p;
            return true;
        }
        return CommonParsers.Exit(ref scanner, result, out parsed, position, orError);
    }
}

public record struct PrefixIncrementParser : IParser<Expression>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out Expression parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (Terminals.Literal("++", ref scanner, advance: true))
        {
            CommonParsers.Spaces0(ref scanner, result, out _);
            if (UnaryParsers.Postfix(ref scanner, result, out var lit))
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
            if (UnaryParsers.Postfix(ref scanner, result, out var lit))
            {
                parsed = new PrefixExpression(Operator.Inc, lit, scanner.GetLocation(position, scanner.Position - position));
                return true;
            }
            else return CommonParsers.Exit(ref scanner, result, out parsed, position, new(SDSLParsingMessages.SDSL0020, scanner.GetErrorLocation(position), scanner.Memory));
   
        }
        else return CommonParsers.Exit(ref scanner, result, out parsed, position, orError);
    }
}

public record struct NotExpressionParser : IParser<Expression>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out Expression parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        parsed = null!;
        var position = scanner.Position;
        if (Terminals.Set("!~", ref scanner))
        {
            var op = ((char)scanner.Peek()).ToOperator();
            scanner.Advance(1);
            CommonParsers.Spaces0(ref scanner, result, out _);
            if (UnaryParsers.Postfix(ref scanner, result, out var lit))
            {
                parsed = new PrefixExpression(op, lit, scanner.GetLocation(position, scanner.Position - position));
                return true;
            }
            else return CommonParsers.Exit(ref scanner, result, out parsed, position, new(SDSLParsingMessages.SDSL0020, scanner.GetErrorLocation(position), scanner.Memory));
                
        }
        else return CommonParsers.Exit(ref scanner, result, out parsed, position, orError);
    }
}

public record struct SignExpressionParser : IParser<Expression>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out Expression parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (Terminals.Set("+-", ref scanner))
        {
            var op = ((char)scanner.Peek()).ToOperator();
            scanner.Advance(1);
            CommonParsers.Spaces0(ref scanner, result, out _);
            if (UnaryParsers.Prefix(ref scanner, result, out var lit))
            {
                parsed = new PrefixExpression(op, lit, scanner.GetLocation(position, scanner.Position - position));
                return true;
            }
            else return CommonParsers.Exit(ref scanner, result, out parsed, position, orError);
        }
        return CommonParsers.Exit(ref scanner, result, out parsed, position, orError);
    }
}

public record struct CastExpressionParser : IParser<Expression>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out Expression parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (
                Terminals.Char('(', ref scanner, advance: true)
                && CommonParsers.Spaces0(ref scanner, result, out _)
                && LiteralsParser.TypeName(ref scanner, result, out var typeName)
                && CommonParsers.Spaces0(ref scanner, result, out _)
                && Terminals.Char(')', ref scanner, true)
                && UnaryParsers.Postfix(ref scanner, result, out var lit)
        )
        {
            parsed = new CastExpression(typeName.Name, Operator.Cast, lit, scanner.GetLocation(position, scanner.Position - position));
            return true;
        }
        else return CommonParsers.Exit(ref scanner, result, out parsed, position, orError);
    }
}