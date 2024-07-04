using Stride.Shaders.Parsing.SDSL.AST;

namespace Stride.Shaders.Parsing.SDSL;

public record struct DirectivePostfixParser : IParser<Expression>
{

    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out Expression parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        // If the following 
        if (
            Accessor(ref scanner, result, out parsed)
            && CommonParsers.Spaces0(ref scanner, result, out _)
        )
        {
            if (Terminals.Set("[.", ref scanner) || Terminals.Literal("++", ref scanner) || Terminals.Literal("--", ref scanner))
            {
                if (Terminals.Char('.', ref scanner, advance: true))
                {
                    if (Postfix(ref scanner, result, out var accessed))
                    {
                        parsed = new AccessorExpression(parsed, accessed, scanner.GetLocation(position, scanner.Position));
                        return true;
                    }
                    else
                    {
                        scanner.Position = position;
                        return false;
                    }
                }
                else if (Terminals.Char('[', ref scanner, advance: true))
                {
                    CommonParsers.Spaces0(ref scanner, result, out _);
                    if (
                        ExpressionParser.Expression(ref scanner, result, out var index)
                        && CommonParsers.Spaces0(ref scanner, result, out _)
                        && Terminals.Char(']', ref scanner, advance: true)
                    )
                    {
                        parsed = new IndexerExpression(parsed, index, scanner.GetLocation(position, scanner.Position - position));
                        return true;
                    }
                    else
                    {
                        scanner.Position = position;
                        return false;
                    }
                }
                else if (Terminals.Literal("++", ref scanner, advance: true))
                {
                    parsed = new PostfixExpression(parsed, Operator.Inc, scanner.GetLocation(position, scanner.Position - position));
                    return true;
                }
                else if (Terminals.Literal("--", ref scanner, advance: true))
                {
                    parsed = new PostfixExpression(parsed, Operator.Dec, scanner.GetLocation(position, scanner.Position - position));
                    return true;
                }
                else 
                {
                    result.Errors.Add(new("Expected Postfix expression", scanner.CreateError(position)));
                    return false;
                }
            }
            else return true;
        }
        else 
        {
            if (orError is not null)
                result.Errors.Add(orError.Value);
            scanner.Position = position;
            return false;
        }
    }
    public static bool Postfix<TScanner>(ref TScanner scanner, ParseResult result, out Expression parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
        => new DirectivePostfixParser().Match(ref scanner, result, out parsed, in orError);
    internal static bool Increment<TScanner>(ref TScanner scanner, ParseResult result, out Expression parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
        => new DirectivePostfixIncrementParser().Match(ref scanner, result, out parsed, in orError);
    internal static bool Accessor<TScanner>(ref TScanner scanner, ParseResult result, out Expression parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
        => new DirectivePostfixAccessorParser().Match(ref scanner, result, out parsed, in orError);
    internal static bool Indexer<TScanner>(ref TScanner scanner, ParseResult result, out Expression parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
        => new DirectivePostfixIndexerParser().Match(ref scanner, result, out parsed, in orError);
}


public record struct DirectivePostfixAccessorParser : IParser<Expression>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out Expression parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (PostfixParser.Indexer(ref scanner, result, out var expression))
        {
            var pos2 = scanner.Position;
            CommonParsers.Spaces0(ref scanner, result, out _);
            if (
                Terminals.Char('.', ref scanner, advance: true)
                && CommonParsers.Spaces0(ref scanner, result, out _)
                && PostfixParser.Accessor(ref scanner, result, out var accessed, new("Expected accessor expression", scanner.CreateError(scanner.Position))))
            {
                parsed = new AccessorExpression(expression, accessed, scanner.GetLocation(position, scanner.Position - position));
                return true;
            }
            else
            {
                scanner.Position = pos2;
                parsed = expression;
                return true;
            }
        }
        if (orError is not null)
                result.Errors.Add(orError.Value);
        parsed = null!;
        return false;
    }
}

public record struct DirectivePostfixIndexerParser : IParser<Expression>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out Expression parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;

        if (PrimaryParsers.Primary(ref scanner, result, out var expression))
        {
            var pos2 = scanner.Position;
            CommonParsers.Spaces0(ref scanner, result, out _);
            if (Terminals.Char('[', ref scanner, advance: true))
            {
                if (
                    CommonParsers.Spaces0(ref scanner, result, out _)
                    && ExpressionParser.Expression(ref scanner, result, out var index, new("Expected expression", scanner.CreateError(scanner.Position)))
                    && CommonParsers.Spaces0(ref scanner, result, out _)
                    && Terminals.Char(']', ref scanner, advance: true)
                )
                {
                    parsed = new IndexerExpression(expression, index, scanner.GetLocation(position, scanner.Position - position));
                    return true;
                }
                else 
                {
                    result.Errors.Add(new("Expected accessor parser", scanner.CreateError(position)));
                    parsed = null!;
                    return false;
                }
            }
            else
            {
                scanner.Position = pos2;
                parsed = expression;
                return true;
            }
        }
        if (orError is not null)
                result.Errors.Add(orError.Value);
        parsed = null!;
        return false;
    }
}

public record struct DirectivePostfixIncrementParser : IParser<Expression>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out Expression parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if(PostfixParser.Accessor(ref scanner, result, out parsed))
        {
            var pos2 = scanner.Position;
            CommonParsers.Spaces0(ref scanner, result, out _);
            if(Terminals.Literal("++", ref scanner, advance: true))
            {
                parsed = new PostfixExpression(parsed, Operator.Inc, scanner.GetLocation(position, scanner.Position - position));
                return true;
            }
            else
            {
                scanner.Position = pos2;
                return true;
            }
        }
        if (orError is not null)
                result.Errors.Add(orError.Value);
        parsed = null!;
        return false;
    }
}


