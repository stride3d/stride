using System.Diagnostics.CodeAnalysis;
using Stride.Shaders.Core;
using Stride.Shaders.Parsing.SDSL.AST;

namespace Stride.Shaders.Parsing.SDSL;

public record struct PostfixParser : IParser<Expression>
{
    public static bool Postfix<TScanner>(ref TScanner scanner, ParseResult result, [MaybeNullWhen(false)] out Expression parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
        => new PostfixParser().Match(ref scanner, result, out parsed, in orError);

    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, [MaybeNullWhen(false)] out Expression parsed, in ParseError? orError = null)
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
                        && Parsers.FollowedByDel(ref scanner, result, ExpressionParser.Expression, out Expression? indexer, withSpaces: true, advance: true)
                        && Parsers.FollowedBy(ref scanner, Tokens.Char(']'), withSpaces: true, advance: true)
                    )
                    {
                        ((AccessorChainExpression)parsed).Accessors.Add(new IndexerExpression(indexer!, indexer!.Info));
                    }
                    else if (
                        matched == "."
                        && Parsers.FollowedByDel(ref scanner, result, PrimaryParsers.Method, out Expression? call, withSpaces: true, advance: true)
                    )
                    {
                        ((AccessorChainExpression)parsed).Accessors.Add(call!);
                    }
                    else if (
                        matched == "."
                        && Parsers.FollowedByDel(ref scanner, result, LiteralsParser.IdentifierBase, out IdentifierBase? accessor, withSpaces: true, advance: true)
                    )
                    {
                        ((AccessorChainExpression)parsed).Accessors.Add(accessor!);
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
}
