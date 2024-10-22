using Stride.Shaders.Parsing.SDSL.AST;

namespace Stride.Shaders.Parsing.SDSL;



public record struct FlowParsers : IParser<Flow>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out Flow parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (While(ref scanner, result, out var w, orError))
        {
            parsed = w;
            return true;
        }
        else if (ForEach(ref scanner, result, out var fe, orError))
        {
            parsed = fe;
            return true;
        }
        else if (For(ref scanner, result, out var f, orError))
        {
            parsed = f;
            return true;
        }
        else return CommonParsers.Exit(ref scanner, result, out parsed, position, orError);
    }

    public static bool While<TScanner>(ref TScanner scanner, ParseResult result, out While parsed, ParseError? orError = null)
        where TScanner : struct, IScanner
        => new WhileParser().Match(ref scanner, result, out parsed, orError);
    public static bool ForEach<TScanner>(ref TScanner scanner, ParseResult result, out ForEach parsed, ParseError? orError = null)
        where TScanner : struct, IScanner
        => new ForEachParser().Match(ref scanner, result, out parsed, orError);
    public static bool For<TScanner>(ref TScanner scanner, ParseResult result, out For parsed, ParseError? orError = null)
        where TScanner : struct, IScanner
        => new ForParser().Match(ref scanner, result, out parsed, orError);
}



public record struct ForParser : IParser<For>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out For parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if(
            Terminals.Literal("for", ref scanner, advance: true)
            && CommonParsers.FollowedBy(ref scanner, Terminals.Char('('), withSpaces: true, advance: true)
        )
        {
            Statement? init = null;
            Statement? condition = null;
            Statement? expression = null;
            CommonParsers.Spaces0(ref scanner, result, out _);

            // Parsing the initialization
            if(StatementParsers.Expression(ref scanner, result, out init)){}
            else if(StatementParsers.Assignments(ref scanner, result, out init)){}
            else if(StatementParsers.Declare(ref scanner, result, out init)){}
            else if(StatementParsers.Empty(ref scanner, result, out init)){}
            else return CommonParsers.Exit(ref scanner, result, out parsed, position, new(SDSLParsingMessages.SDSL0036, scanner.GetErrorLocation(scanner.Position), scanner.Memory));

            CommonParsers.Spaces0(ref scanner, result, out _);
            // Parsing the condition

            if (StatementParsers.Expression(ref scanner, result, out condition)){}
            else if (StatementParsers.Empty(ref scanner, result, out condition)){}
            else return CommonParsers.Exit(ref scanner, result, out parsed, position, new(SDSLParsingMessages.SDSL0037, scanner.GetErrorLocation(scanner.Position), scanner.Memory));
            
            CommonParsers.Spaces0(ref scanner, result, out _);
            // parsing the final expression
            
            var tmpPos = scanner.Position;
            if(CommonParsers.FollowedBy(ref scanner, Terminals.Char(')'), withSpaces: true, advance: true))
                expression = new EmptyStatement(scanner.GetLocation(tmpPos..scanner.Position));
            else if(ExpressionParser.Expression(ref scanner, result, out var exp) && CommonParsers.FollowedBy(ref scanner, Terminals.Char(')'), withSpaces: true, advance: true))
            {
                expression = new ExpressionStatement(exp, exp.Info);
            }
            else return CommonParsers.Exit(ref scanner, result, out parsed, position, new(SDSLParsingMessages.SDSL0038, scanner.GetErrorLocation(scanner.Position), scanner.Memory));

            CommonParsers.Spaces0(ref scanner, result, out _);

            // parsing the block or statement

            if(StatementParsers.Statement(ref scanner, result, out var body))
            {
                parsed = new For(init, condition, expression, body, scanner.GetLocation(position..scanner.Position));
                return true;
            }
            else return CommonParsers.Exit(ref scanner, result, out parsed, position, orError);
        }
        else return CommonParsers.Exit(ref scanner, result, out parsed, position, orError);
    }
}

public record struct ForEachParser : IParser<ForEach>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out ForEach parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (Terminals.Literal("foreach", ref scanner, advance: true) && CommonParsers.Spaces0(ref scanner, result, out _))
        {
            if (Terminals.Char('(', ref scanner, advance: true) && CommonParsers.Spaces0(ref scanner, result, out _))
            {
                if (
                    LiteralsParser.TypeName(ref scanner, result, out var typeName, new(SDSLParsingMessages.SDSL0017, scanner.GetErrorLocation(scanner.Position), scanner.Memory))
                    && CommonParsers.Spaces1(ref scanner, result, out _)
                    && LiteralsParser.Identifier(ref scanner, result, out var identifier, new(SDSLParsingMessages.SDSL0032, scanner.GetErrorLocation(scanner.Position), scanner.Memory))
                    && CommonParsers.Spaces1(ref scanner, result, out _)
                )
                {
                    if (Terminals.Literal("in", ref scanner, advance: true) && CommonParsers.Spaces1(ref scanner, result, out _))
                    {
                        if (
                            ExpressionParser.Expression(ref scanner, result, out var collection, new(SDSLParsingMessages.SDSL0032, scanner.GetErrorLocation(scanner.Position), scanner.Memory))
                            && CommonParsers.Spaces0(ref scanner, result, out _)
                        )
                        {
                            if (Terminals.Char(')', ref scanner, advance: true) && CommonParsers.Spaces0(ref scanner, result, out _))
                            {
                                if (StatementParsers.Statement(ref scanner, result, out var statement, new(SDSLParsingMessages.SDSL0010, scanner.GetErrorLocation(scanner.Position), scanner.Memory)))
                                {
                                    parsed = new(typeName, identifier, collection, statement, scanner.GetLocation(position..scanner.Position));
                                    return true;
                                }
                            }
                            else return CommonParsers.Exit(ref scanner, result, out parsed, position, new(SDSLParsingMessages.SDSL0018, scanner.GetErrorLocation(scanner.Position), scanner.Memory));
                        }
                    }
                    else return CommonParsers.Exit(ref scanner, result, out parsed, position, new(SDSLParsingMessages.SDSL0001, scanner.GetErrorLocation(scanner.Position), scanner.Memory));
                }
            }
            else return CommonParsers.Exit(ref scanner, result, out parsed, position, new(SDSLParsingMessages.SDSL0035, scanner.GetErrorLocation(scanner.Position), scanner.Memory));
        }
        return CommonParsers.Exit(ref scanner, result, out parsed, position, orError);
    }
}

public record struct WhileParser : IParser<While>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out While parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (Terminals.Literal("while", ref scanner, advance: true) && CommonParsers.Spaces0(ref scanner, result, out _))
        {
            if (Terminals.Char('(', ref scanner, advance: true) && CommonParsers.Spaces0(ref scanner, result, out _))
            {
                if (ExpressionParser.Expression(ref scanner, result, out var expression, new(SDSLParsingMessages.SDSL0015, scanner.GetErrorLocation(scanner.Position), scanner.Memory)))
                {
                    if (Terminals.Char(')', ref scanner, advance: true) && CommonParsers.Spaces0(ref scanner, result, out _))
                    {
                        if (StatementParsers.Statement(ref scanner, result, out var statement, new(SDSLParsingMessages.SDSL0010, scanner.GetErrorLocation(scanner.Position), scanner.Memory)))
                        {
                            parsed = new(expression, statement, scanner.GetLocation(position..scanner.Position));
                            return true;
                        }
                    }
                    else return CommonParsers.Exit(ref scanner, result, out parsed, position, new(SDSLParsingMessages.SDSL0018, scanner.GetErrorLocation(scanner.Position), scanner.Memory));
                }
            }
            else return CommonParsers.Exit(ref scanner, result, out parsed, position, new(SDSLParsingMessages.SDSL0035, scanner.GetErrorLocation(scanner.Position), scanner.Memory));
        }
        return CommonParsers.Exit(ref scanner, result, out parsed, position, orError);
    }
}

