using Stride.Shaders.Parsing.SDSL.AST;

namespace Stride.Shaders.Parsing.SDSL;

public record struct PostfixParser : IParser<Expression>
{
    public static bool Postfix<TScanner>(ref TScanner scanner, ParseResult result, out Expression parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
        => new PostfixParser().Match(ref scanner, result, out parsed, in orError);

    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out Expression parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (PrimaryParsers.Primary(ref scanner, result, out parsed))
        {
            while (!scanner.IsEof && CommonParsers.FollowedByAny(ref scanner, ["[", ".", "++", "--"], out var matched, withSpaces: true, advance: true))
            {
                if (
                    matched == "["
                    && CommonParsers.FollowedByDel(ref scanner, result, ExpressionParser.Expression, out Expression indexer, withSpaces: true, advance: true)
                    && CommonParsers.FollowedBy(ref scanner, Terminals.Char(']'), withSpaces: true, advance: true)
                )
                {
                    parsed = new IndexerExpression(parsed, indexer, scanner.GetLocation(position..scanner.Position));
                }
                else if (
                    matched == "."
                    && CommonParsers.FollowedByDel(ref scanner, result, PrimaryParsers.Method, out Expression call, withSpaces: true, advance: true)
                )
                {
                    parsed = new AccessorExpression(parsed, call, scanner.GetLocation(position..scanner.Position));
                }
                else if (
                    matched == "."
                    && CommonParsers.FollowedByDel(ref scanner, result, LiteralsParser.Literal, out Literal accessor, withSpaces: true, advance: true)
                )
                {
                    parsed = new AccessorExpression(parsed, accessor, scanner.GetLocation(position..scanner.Position));
                }
                else if (matched == "++" || matched == "--")
                {
                    parsed = new PostfixExpression(parsed, matched.ToOperator(), scanner.GetLocation(position..scanner.Position));
                    break;
                }
            }
            CommonParsers.Spaces0(ref scanner, result, out _);
            return true;
        }
        return CommonParsers.Exit(ref scanner, result, out parsed, position, orError);
    }
    
    public static bool Increment<TScanner>(ref TScanner scanner, ParseResult result, out Expression parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (Accessor(ref scanner, result, out parsed))
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

    public static bool Accessor<TScanner>(ref TScanner scanner, ParseResult result, out Expression parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (Indexer(ref scanner, result, out var expression))
        {
            var pos2 = scanner.Position;
            CommonParsers.Spaces0(ref scanner, result, out _);
            if (
                Terminals.Char('.', ref scanner, advance: true)
                && CommonParsers.Spaces0(ref scanner, result, out _)
                && Accessor(ref scanner, result, out var accessed))
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

    internal static bool Indexer<TScanner>(ref TScanner scanner, ParseResult result, out Expression parsed, in ParseError? orError = null)
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
                    && ExpressionParser.Expression(ref scanner, result, out var index)
                    && CommonParsers.Spaces0(ref scanner, result, out _)
                    && Terminals.Char(']', ref scanner, advance: true)
                )
                {
                    parsed = new IndexerExpression(expression, index, scanner.GetLocation(position, scanner.Position - position));
                    return true;
                }
                else return CommonParsers.Exit(ref scanner, result, out parsed, position, new(SDSLParsingMessages.SDSL0021, scanner.GetErrorLocation(position), scanner.Memory));

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


