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
            if (Parsers.FollowedByAny(ref scanner, ["[", ".", "++", "--"], out _, withSpaces: true))
            {
                parsed = new AccessorChainExpression(parsed, parsed.Info);
                while (!scanner.IsEof && Parsers.FollowedByAny(ref scanner, ["[", ".", "++", "--"], out var matched, withSpaces: true, advance: true))
                {
                    if (
                        matched == "["
                        && Parsers.FollowedByDel(ref scanner, result, ExpressionParser.Expression, out Expression indexer, withSpaces: true, advance: true)
                        && Parsers.FollowedBy(ref scanner, Tokens.Char(']'), withSpaces: true, advance: true)
                    )
                    {
                        ((AccessorChainExpression)parsed).Accessors.Add(indexer);
                    }
                    else if (
                        matched == "."
                        && Parsers.FollowedByDel(ref scanner, result, PrimaryParsers.Method, out Expression call, withSpaces: true, advance: true)
                    )
                    {
                        ((AccessorChainExpression)parsed).Accessors.Add(call);
                    }
                    else if (
                        matched == "."
                        && Parsers.FollowedByDel(ref scanner, result, LiteralsParser.Identifier, out Identifier accessor, withSpaces: true, advance: true)
                    )
                    {
                        ((AccessorChainExpression)parsed).Accessors.Add(accessor);
                    }
                    else if (matched == "++" || matched == "--")
                    {
                        ((AccessorChainExpression)parsed).Accessors.Add(new PostfixIncrement(matched.ToOperator(), scanner[(scanner.Position - 2)..scanner.Position]));
                        break;
                    }
                }
                Parsers.Spaces0(ref scanner, result, out _);
            }
            parsed.Info = scanner[position..scanner.Position];
            return true;
        }
        return Parsers.Exit(ref scanner, result, out parsed, position, orError);
    }

    // public static bool Increment<TScanner>(ref TScanner scanner, ParseResult result, out Expression parsed, in ParseError? orError = null)
    //     where TScanner : struct, IScanner
    // {
    //     var position = scanner.Position;
    //     if (Accessor(ref scanner, result, out parsed))
    //     {
    //         var pos2 = scanner.Position;
    //         CommonParsers.Spaces0(ref scanner, result, out _);
    //         if (Tokens.Literal("++", ref scanner, advance: true))
    //         {
    //             parsed = new PostfixExpression(parsed, Operator.Inc, scanner[position..scanner.Position]);
    //             return true;
    //         }
    //         else
    //         {
    //             scanner.Position = pos2;
    //             return true;
    //         }
    //     }
    //     return CommonParsers.Exit(ref scanner, result, out parsed, position, orError);
    // }

    // public static bool Accessor<TScanner>(ref TScanner scanner, ParseResult result, out Expression parsed, in ParseError? orError = null)
    //     where TScanner : struct, IScanner
    // {
    //     var position = scanner.Position;
    //     if (Indexer(ref scanner, result, out var expression))
    //     {
    //         var pos2 = scanner.Position;
    //         CommonParsers.Spaces0(ref scanner, result, out _);
    //         if (
    //             Tokens.Char('.', ref scanner, advance: true)
    //             && CommonParsers.Spaces0(ref scanner, result, out _)
    //             && Accessor(ref scanner, result, out var accessed))
    //         {
    //             parsed = new AccessorExpression(expression, accessed, scanner[position..scanner.Position]);
    //             return true;
    //         }
    //         else
    //         {
    //             scanner.Position = pos2;
    //             parsed = expression;
    //             return true;
    //         }
    //     }
    //     return CommonParsers.Exit(ref scanner, result, out parsed, position, orError);
    // }

    // internal static bool Indexer<TScanner>(ref TScanner scanner, ParseResult result, out Expression parsed, in ParseError? orError = null)
    //     where TScanner : struct, IScanner
    // {
    //     var position = scanner.Position;

    //     if (PrimaryParsers.Primary(ref scanner, result, out var expression))
    //     {
    //         var pos2 = scanner.Position;
    //         CommonParsers.Spaces0(ref scanner, result, out _);
    //         if (Tokens.Char('[', ref scanner, advance: true))
    //         {
    //             if (
    //                 CommonParsers.Spaces0(ref scanner, result, out _)
    //                 && ExpressionParser.Expression(ref scanner, result, out var index)
    //                 && CommonParsers.Spaces0(ref scanner, result, out _)
    //                 && Tokens.Char(']', ref scanner, advance: true)
    //             )
    //             {
    //                 parsed = new IndexerExpression(expression, index, scanner[position..scanner.Position]);
    //                 return true;
    //             }
    //             else return CommonParsers.Exit(ref scanner, result, out parsed, position, new(SDSLErrorMessages.SDSL0021, scanner[position], scanner.Memory));

    //         }
    //         else
    //         {
    //             scanner.Position = pos2;
    //             parsed = expression;
    //             return true;
    //         }
    //     }
    //     return CommonParsers.Exit(ref scanner, result, out parsed, position, orError);
    // }
}


