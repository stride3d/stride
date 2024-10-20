using Stride.Shaders.Parsing.SDSL.AST;

namespace Stride.Shaders.Parsing.SDSL;

public record struct PostfixParser : IParser<Expression>
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
                    else return CommonParsers.Exit(ref scanner, result, out parsed, position, orError);
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
                else return CommonParsers.Exit(ref scanner, result, out parsed, position, new(SDSLErrors.SDSL0020, scanner.GetErrorLocation(position), scanner.Memory));
                    
            }
            else return true;
        }
        return CommonParsers.Exit(ref scanner, result, out parsed, position, orError);
    }
    public static bool Postfix<TScanner>(ref TScanner scanner, ParseResult result, out Expression parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
        => new PostfixParser().Match(ref scanner, result, out parsed, in orError);
    internal static bool Increment<TScanner>(ref TScanner scanner, ParseResult result, out Expression parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
        => new PostfixIncrementParser().Match(ref scanner, result, out parsed, in orError);
    internal static bool Accessor<TScanner>(ref TScanner scanner, ParseResult result, out Expression parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
        => new PostfixAccessorParser().Match(ref scanner, result, out parsed, in orError);
    internal static bool Indexer<TScanner>(ref TScanner scanner, ParseResult result, out Expression parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
        => new PostfixIndexerParser().Match(ref scanner, result, out parsed, in orError);
}


public record struct PostfixAccessorParser : IParser<Expression>
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
                && PostfixParser.Accessor(ref scanner, result, out var accessed, new(SDSLErrors.SDSL0024, scanner.GetErrorLocation(scanner.Position), scanner.Memory)))
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
        return CommonParsers.Exit(ref scanner, result, out parsed, position, orError);
    }
}

public record struct PostfixIndexerParser : IParser<Expression>
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
                    && ExpressionParser.Expression(ref scanner, result, out var index, new(SDSLErrors.SDSL0015, scanner.GetErrorLocation(scanner.Position), scanner.Memory))
                    && CommonParsers.Spaces0(ref scanner, result, out _)
                    && Terminals.Char(']', ref scanner, advance: true)
                )
                {
                    parsed = new IndexerExpression(expression, index, scanner.GetLocation(position, scanner.Position - position));
                    return true;
                }
                else return CommonParsers.Exit(ref scanner, result, out parsed, position, new(SDSLErrors.SDSL0021, scanner.GetErrorLocation(position), scanner.Memory));
                    
            }
            else
            {
                scanner.Position = pos2;
                parsed = expression;
                return true;
            }
        }
        return CommonParsers.Exit(ref scanner, result, out parsed, position, orError);
    }
}

public record struct PostfixIncrementParser : IParser<Expression>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out Expression parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (PostfixParser.Accessor(ref scanner, result, out parsed))
        {
            var pos2 = scanner.Position;
            CommonParsers.Spaces0(ref scanner, result, out _);
            if (Terminals.Literal("++", ref scanner, advance: true))
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
        return CommonParsers.Exit(ref scanner, result, out parsed, position, orError);
    }
}


