using Stride.Shaders.Parsing.SDSL.AST;

namespace Stride.Shaders.Parsing.SDSL;



public record struct ControlsParser : IParser<ConditionalFlow>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out ConditionalFlow parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
        => Control(ref scanner, result, out parsed, StatementParsers.Statement, orError);

    public static bool Control<TScanner>(ref TScanner scanner, ParseResult result, out ConditionalFlow parsed, ParserDelegate<TScanner, Statement> statementParser, ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (ShaderAttributeListParser.AttributeList(ref scanner, result, out var attributeList))
            Parsers.Spaces0(ref scanner, result, out _);
        if (If(ref scanner, result, out var ifstatement, statementParser, orError) && Parsers.Spaces0(ref scanner, result, out _))
        {
            parsed = new(ifstatement, scanner[..])
            {
                Attributes = attributeList
            };
            while (ElseIf(ref scanner, result, out var elseif, statementParser, orError) && Parsers.Spaces0(ref scanner, result, out _))
                parsed.ElseIfs.Add(elseif);
            if (Else(ref scanner, result, out var elseStatement, statementParser, orError))
                parsed.Else = elseStatement;
            parsed.Info = scanner[position..scanner.Position];
            return true;
        }
        else if (Tokens.Literal("else ", ref scanner))
            return Parsers.Exit(ref scanner, result, out parsed, position, new("Else block should be preceeded by If statement", scanner[scanner.Position], scanner.Memory));
        return Parsers.Exit(ref scanner, result, out parsed, position, orError);
    }

    public static bool Control<TScanner>(ref TScanner scanner, ParseResult result, out ConditionalFlow parsed, ParseError? orError = null)
        where TScanner : struct, IScanner
        => Control(ref scanner, result, out parsed, StatementParsers.Statement, orError);

    public static bool If<TScanner>(ref TScanner scanner, ParseResult result, out If parsed, ParserDelegate<TScanner, Statement> statementParser, ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (
            Tokens.Literal("if", ref scanner, advance: true)
            && Parsers.FollowedBy(ref scanner, Tokens.Char('('), withSpaces: true, advance: true)
            && Parsers.Spaces0(ref scanner, result, out _)
            && ExpressionParser.Expression(ref scanner, result, out var condition, new(SDSLErrorMessages.SDSL0015, scanner[scanner.Position], scanner.Memory))
            && Parsers.Spaces0(ref scanner, result, out _)
        )
        {
            if (Tokens.Char(')', ref scanner, advance: true) && Parsers.Spaces0(ref scanner, result, out _))
            {
                if (statementParser(ref scanner, result, out var statement, new(SDSLErrorMessages.SDSL0010, scanner[scanner.Position], scanner.Memory)))
                {
                    parsed = new(condition, statement, scanner[position..scanner.Position]);
                    return true;
                }
            }
            else return Parsers.Exit(ref scanner, result, out parsed, position, new(SDSLErrorMessages.SDSL0018, scanner[scanner.Position], scanner.Memory));
        }
        return Parsers.Exit(ref scanner, result, out parsed, position, orError);
    }

    public static bool If<TScanner>(ref TScanner scanner, ParseResult result, out If parsed, ParseError? orError = null)
        where TScanner : struct, IScanner
        => If(ref scanner, result, out parsed, StatementParsers.Statement, orError);

    public static bool ElseIf<TScanner>(ref TScanner scanner, ParseResult result, out ElseIf parsed, ParserDelegate<TScanner, Statement> statementParser, ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (
            Tokens.Literal("else", ref scanner, advance: true)
            && Parsers.Spaces1(ref scanner, result, out _)
            && Tokens.Literal("if", ref scanner, advance: true)
            && Parsers.Spaces0(ref scanner, result, out _)
            && Tokens.Char('(', ref scanner, advance: true)
            && Parsers.Spaces0(ref scanner, result, out _)
            && ExpressionParser.Expression(ref scanner, result, out var condition, new(SDSLErrorMessages.SDSL0015, scanner[scanner.Position], scanner.Memory))
            && Parsers.Spaces0(ref scanner, result, out _)
        )
        {
            if (Tokens.Char(')', ref scanner, advance: true) && Parsers.Spaces0(ref scanner, result, out _))
            {
                if (statementParser(ref scanner, result, out var statement, new(SDSLErrorMessages.SDSL0010, scanner[scanner.Position], scanner.Memory)))
                {
                    parsed = new(condition, statement, scanner[position..scanner.Position]);
                    return true;
                }
            }
            else return Parsers.Exit(ref scanner, result, out parsed, position, new(SDSLErrorMessages.SDSL0018, scanner[scanner.Position], scanner.Memory));
        }
        return Parsers.Exit(ref scanner, result, out parsed, position, orError);
    }

    public static bool ElseIf<TScanner>(ref TScanner scanner, ParseResult result, out ElseIf parsed, ParseError? orError = null)
        where TScanner : struct, IScanner
        => ElseIf(ref scanner, result, out parsed, StatementParsers.Statement, orError);

    public static bool Else<TScanner>(ref TScanner scanner, ParseResult result, out Else parsed, ParserDelegate<TScanner, Statement> statementParser, ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (
            Tokens.Literal("else", ref scanner, advance: true)
            && Parsers.Spaces0(ref scanner, result, out _)
            && statementParser(ref scanner, result, out var statement, new(SDSLErrorMessages.SDSL0010, scanner[scanner.Position], scanner.Memory))
        )
        {
            parsed = new(statement, scanner[position..scanner.Position]);
            return true;
        }
        return Parsers.Exit(ref scanner, result, out parsed, position, orError);
    }

    public static bool Else<TScanner>(ref TScanner scanner, ParseResult result, out Else parsed, ParseError? orError = null)
        where TScanner : struct, IScanner
        => Else(ref scanner, result, out parsed, StatementParsers.Statement, orError);
}
