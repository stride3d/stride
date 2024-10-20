using Stride.Shaders.Parsing.SDSL.AST;

namespace Stride.Shaders.Parsing.SDSL;


public record struct DirectivePrefixParser : IParser<Expression>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out Expression parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (DirectiveUnaryParsers.PrefixIncrement(ref scanner, result, out parsed))
            return true;
        else if (DirectiveUnaryParsers.Signed(ref scanner, result, out parsed))
            return true;
        // prefix not
        else if (DirectiveUnaryParsers.Not(ref scanner, result, out parsed))
            return true;
        // prefix cast 
        else if (DirectiveUnaryParsers.Cast(ref scanner, result, out parsed))
            return true;
        else if (DirectiveUnaryParsers.Primary(ref scanner, result, out var p))
        {
            parsed = p;
            return true;
        }
        else
        {
            if (orError is not null)
                result.Errors.Add(orError.Value);
            parsed = null!;
            scanner.Position = position;
            return false;
        }
    }
}

public record struct DirectivePrefixIncrementParser : IParser<Expression>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out Expression parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (Terminals.Literal("++", ref scanner, advance: true))
        {
            CommonParsers.Spaces0(ref scanner, result, out _);
            if (DirectiveUnaryParsers.Primary(ref scanner, result, out var lit))
            {
                parsed = new PrefixExpression(Operator.Inc, lit, scanner.GetLocation(position, scanner.Position - position));
                return true;
            }
            else
            {
                parsed = null!;
                scanner.Position = position;
                result.Errors.Add(new("Expecting Postfix expression", scanner.GetErrorLocation(position)));
                return false;
            }
        }
        // prefix decrememnt 
        else if (Terminals.Literal("--", ref scanner, advance: true))
        {
            CommonParsers.Spaces0(ref scanner, result, out _);
            if (DirectiveUnaryParsers.Primary(ref scanner, result, out var lit))
            {
                parsed = new PrefixExpression(Operator.Inc, lit, scanner.GetLocation(position, scanner.Position - position));
                return true;
            }
            else
            {
                parsed = null!;
                scanner.Position = position;
                result.Errors.Add(new("Expecting Postfix expression", scanner.GetErrorLocation(position)));
                return false;
            }
        }
        else
        {
            if(orError is not null)
                result.Errors.Add(orError.Value);
            scanner.Position = position;
            parsed = null!;
            return false;
        }
    }
}

public record struct DirectiveNotExpressionParser : IParser<Expression>
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
            if (DirectiveUnaryParsers.Primary(ref scanner, result, out var lit))
            {
                parsed = new PrefixExpression(op, lit, scanner.GetLocation(position, scanner.Position - position));
                return true;
            }
            else
            {
                parsed = null!;
                scanner.Position = position;
                result.Errors.Add(new("Expecting Postfix expression", scanner.GetErrorLocation(position)));
                return false;
            }
        }
        else 
        {
            if (orError is not null)
                result.Errors.Add(orError.Value with { Location = scanner.GetErrorLocation(position) });
            return false;
        }
    }
}

public record struct DirectiveSignExpressionParser : IParser<Expression>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out Expression parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        parsed = null!;
        var position = scanner.Position;
        if (Terminals.Set("+-", ref scanner))
        {
            var op = ((char)scanner.Peek()).ToOperator();
            scanner.Advance(1);
            CommonParsers.Spaces0(ref scanner, result, out _);
            if (DirectiveUnaryParsers.Prefix(ref scanner, result, out var lit))
            {
                parsed = new PrefixExpression(op, lit, scanner.GetLocation(position, scanner.Position - position));
                return true;
            }
            else
            {
                // TODO: check if error can be added here
                if (orError is not null)
                    result.Errors.Add(orError.Value with { Location = scanner.GetErrorLocation(position) });
                parsed = null!;
                scanner.Position = position;
                return false;
            }
        }
        else 
        {
            if (orError is not null)
                result.Errors.Add(orError.Value with { Location = scanner.GetErrorLocation(position) });
            return false;
        }
    }
}

public record struct DirectiveCastExpressionParser : IParser<Expression>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out Expression parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (
                Terminals.Char('(', ref scanner, advance: true)
                && CommonParsers.Spaces0(ref scanner, result, out _)
                && LiteralsParser.Identifier(ref scanner, result, out var typeName, new("Expected identifier", scanner.GetErrorLocation(scanner.Position)))
                && CommonParsers.Spaces0(ref scanner, result, out _)
                && Terminals.Char(')', ref scanner, true)
                && DirectiveUnaryParsers.Primary(ref scanner, result, out var lit)
        )
        {
            parsed = new CastExpression(typeName.Name, Operator.Cast, lit, scanner.GetLocation(position, scanner.Position - position));
            return true;
        }
        else
        {
            if (orError is not null)
                result.Errors.Add(orError.Value with { Location = scanner.GetErrorLocation(position) });
            parsed = null!;
            scanner.Position = position;
            return false;
        }
    }
}