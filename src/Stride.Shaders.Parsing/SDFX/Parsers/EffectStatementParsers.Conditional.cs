using Stride.Shaders.Parsing.SDFX.AST;
using Stride.Shaders.Parsing.SDFX.Parsers;
using Stride.Shaders.Parsing.SDSL;

namespace Stride.Shaders.Parsing.SDFX;



public record struct EffectControlsParser : IParser<EffectControl>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out EffectControl parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (If(ref scanner, result, out var ifstatement, orError) && CommonParsers.Spaces0(ref scanner, result, out _))
        {
            parsed = new(ifstatement, scanner.GetLocation(..));
            while(ElseIf(ref scanner, result, out var elseif, orError))
                parsed.ElseIfs.Add(elseif);
            if (Else(ref scanner, result, out var elseStatement, orError))
                parsed.Else = elseStatement;
            parsed.Info = scanner.GetLocation(position..scanner.Position);
            return true;
        }
        else if(Terminals.Literal("else ", ref scanner))
            return CommonParsers.Exit(ref scanner, result, out parsed, position, new("Else block should be preceeded by If statement", scanner.GetErrorLocation(scanner.Position), scanner.Memory));
        return CommonParsers.Exit(ref scanner, result, out parsed, position, orError);
    }

    public static bool Control<TScanner>(ref TScanner scanner, ParseResult result, out EffectControl parsed, ParseError? orError = null)
        where TScanner : struct, IScanner
        => new EffectControlsParser().Match(ref scanner, result, out parsed, orError);

    public static bool If<TScanner>(ref TScanner scanner, ParseResult result, out If parsed, ParseError? orError = null)
        where TScanner : struct, IScanner
        => new IfStatementParser().Match(ref scanner, result, out parsed, orError);
    public static bool ElseIf<TScanner>(ref TScanner scanner, ParseResult result, out ElseIf parsed, ParseError? orError = null)
        where TScanner : struct, IScanner
        => new ElseIfStatementParser().Match(ref scanner, result, out parsed, orError);
    public static bool Else<TScanner>(ref TScanner scanner, ParseResult result, out Else parsed, ParseError? orError = null)
        where TScanner : struct, IScanner
        => new ElseStatementParser().Match(ref scanner, result, out parsed, orError);
}



public record struct IfStatementParser : IParser<If>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out If parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (
            Terminals.Literal("if", ref scanner, advance: true)
            && CommonParsers.FollowedBy(ref scanner, Terminals.Char('('), withSpaces: true, advance: true)
            && CommonParsers.Spaces0(ref scanner, result, out _)
            && ExpressionParser.Expression(ref scanner, result, out var condition, new(SDSLParsingMessages.SDSL0015, scanner.GetErrorLocation(scanner.Position), scanner.Memory))
            && CommonParsers.Spaces0(ref scanner, result, out _)
        )
        {
            if (Terminals.Char(')', ref scanner, advance: true) && CommonParsers.Spaces0(ref scanner, result, out _))
            {
                if (EffectStatementParsers.Statement(ref scanner, result, out var statement, new(SDSLParsingMessages.SDSL0010, scanner.GetErrorLocation(scanner.Position), scanner.Memory)))
                {
                    parsed = new(condition, statement, scanner.GetLocation(position..scanner.Position));
                    return true;
                }
            }
            else return CommonParsers.Exit(ref scanner, result, out parsed, position, new(SDSLParsingMessages.SDSL0018, scanner.GetErrorLocation(scanner.Position), scanner.Memory));
        }
        return CommonParsers.Exit(ref scanner, result, out parsed, position, orError);
    }
}

public record struct ElseIfStatementParser : IParser<ElseIf>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out ElseIf parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (
            Terminals.Literal("else", ref scanner, advance: true)
            && CommonParsers.Spaces1(ref scanner, result, out _)
            && Terminals.Literal("if", ref scanner, advance: true)
            && CommonParsers.Spaces0(ref scanner, result, out _)
            && Terminals.Char('(', ref scanner, advance: true)
            && CommonParsers.Spaces0(ref scanner, result, out _)
            && ExpressionParser.Expression(ref scanner, result, out var condition, new(SDSLParsingMessages.SDSL0015, scanner.GetErrorLocation(scanner.Position), scanner.Memory))
            && CommonParsers.Spaces0(ref scanner, result, out _)
        )
        {
            if (Terminals.Char(')', ref scanner, advance: true) && CommonParsers.Spaces0(ref scanner, result, out _))
            {
                if (EffectStatementParsers.Statement(ref scanner, result, out var statement, new(SDSLParsingMessages.SDSL0010, scanner.GetErrorLocation(scanner.Position), scanner.Memory)))
                {
                    parsed = new(condition, statement, scanner.GetLocation(position..scanner.Position));
                    return true;
                }
            }
            else return CommonParsers.Exit(ref scanner, result, out parsed, position, new(SDSLParsingMessages.SDSL0018, scanner.GetErrorLocation(scanner.Position), scanner.Memory));
        }
        return CommonParsers.Exit(ref scanner, result, out parsed, position, orError);
    }
}

public record struct ElseStatementParser : IParser<Else>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out Else parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (
            Terminals.Literal("else", ref scanner, advance: true)
            && CommonParsers.Spaces0(ref scanner, result, out _)
            && EffectStatementParsers.Statement(ref scanner, result, out var statement, new(SDSLParsingMessages.SDSL0010, scanner.GetErrorLocation(scanner.Position), scanner.Memory))
        )
        {
            parsed = new(statement, scanner.GetLocation(position..scanner.Position));
            return true;
        }
        return CommonParsers.Exit(ref scanner, result, out parsed, position, orError);
    }
}

