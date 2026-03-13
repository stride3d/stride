using Stride.Shaders.Core;
using Stride.Shaders.Parsing.SDSL.AST;

namespace Stride.Shaders.Parsing.SDSL;



public record struct FlowParsers : IParser<Flow>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out Flow parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
        => Flow(ref scanner, result, out parsed, StatementParsers.Statement, orError);

    public static bool Flow<TScanner>(ref TScanner scanner, ParseResult result, out Flow parsed, ParserDelegate<TScanner, Statement> statementParser, ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        var hasAttributes = ShaderAttributeListParser.Attribute(ref scanner, result, out var attribute) && Parsers.Spaces0(ref scanner, result, out _);
        if (!hasAttributes)
            scanner.Position = position;
        if (While(ref scanner, result, out var w, statementParser, orError))
        {
            if (hasAttributes)
                w.Attribute = attribute;
            parsed = w;
            return true;
        }
        else if (ForEach(ref scanner, result, out var fe, statementParser, orError))
        {
            parsed = fe;
            return true;
        }
        else if (For(ref scanner, result, out var f, statementParser, orError))
        {
            if (hasAttributes)
                f.Attribute = attribute;
            parsed = f;
            return true;
        }
        else return Parsers.Exit(ref scanner, result, out parsed, position, orError);
    }

    public static bool Flow<TScanner>(ref TScanner scanner, ParseResult result, out Flow parsed, ParseError? orError = null)
        where TScanner : struct, IScanner
        => Flow(ref scanner, result, out parsed, StatementParsers.Statement, orError);

    public static bool While<TScanner>(ref TScanner scanner, ParseResult result, out While parsed, ParserDelegate<TScanner, Statement> statementParser, ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (Tokens.Literal("while", ref scanner, advance: true) && Parsers.Spaces0(ref scanner, result, out _))
        {
            if (Tokens.Char('(', ref scanner, advance: true) && Parsers.Spaces0(ref scanner, result, out _))
            {
                if (ExpressionParser.Expression(ref scanner, result, out var expression, new(SDSLErrorMessages.SDSL0015, scanner[scanner.Position], scanner.Memory)))
                {
                    if (Tokens.Char(')', ref scanner, advance: true) && Parsers.Spaces0(ref scanner, result, out _))
                    {
                        if (statementParser(ref scanner, result, out var statement, new(SDSLErrorMessages.SDSL0010, scanner[scanner.Position], scanner.Memory)))
                        {
                            parsed = new(expression, statement, scanner[position..scanner.Position]);
                            return true;
                        }
                    }
                    else return Parsers.Exit(ref scanner, result, out parsed, position, new(SDSLErrorMessages.SDSL0018, scanner[scanner.Position], scanner.Memory));
                }
            }
            else return Parsers.Exit(ref scanner, result, out parsed, position, new(SDSLErrorMessages.SDSL0035, scanner[scanner.Position], scanner.Memory));
        }
        return Parsers.Exit(ref scanner, result, out parsed, position, orError);
    }

    public static bool While<TScanner>(ref TScanner scanner, ParseResult result, out While parsed, ParseError? orError = null)
        where TScanner : struct, IScanner
        => While(ref scanner, result, out parsed, StatementParsers.Statement, orError);

    public static bool For<TScanner>(ref TScanner scanner, ParseResult result, out For parsed, ParserDelegate<TScanner, Statement> statementParser, ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (
            Tokens.Literal("for", ref scanner, advance: true)
            && Parsers.FollowedBy(ref scanner, Tokens.Char('('), withSpaces: true, advance: true)
        )
        {
            Statement? init = null;
            Expression? condition = null;
            List<Statement>? expressions = null;
            Parsers.Spaces0(ref scanner, result, out _);

            // Parsing the initialization
            if (StatementParsers.Expression(ref scanner, result, out init)) { }
            else if (StatementParsers.DeclareOrAssign(ref scanner, result, out init)) { }
            else if (StatementParsers.Empty(ref scanner, result, out init)) { }
            else return Parsers.Exit(ref scanner, result, out parsed, position, new(SDSLErrorMessages.SDSL0036, scanner[scanner.Position], scanner.Memory));

            Parsers.Spaces0(ref scanner, result, out _);
            // Parsing the condition

            if (ExpressionParser.Expression(ref scanner, result, out condition)
                && Parsers.FollowedBy(ref scanner, Tokens.Char(';'), advance: true)) { }
            else return Parsers.Exit(ref scanner, result, out parsed, position, new(SDSLErrorMessages.SDSL0037, scanner[scanner.Position], scanner.Memory));

            Parsers.Spaces0(ref scanner, result, out _);
            // parsing the final expression

            var tmpPos = scanner.Position;

            if (!Parsers.Repeat(ref scanner, result, AssignOrExpression, out expressions, 0, withSpaces: true, separator: ","))
                expressions = [new EmptyStatement(scanner[tmpPos..scanner.Position])];
            if (!Parsers.FollowedBy(ref scanner, Tokens.Char(')'), withSpaces: true, advance: true))
                return Parsers.Exit(ref scanner, result, out parsed, position, new(SDSLErrorMessages.SDSL0018, scanner[scanner.Position], scanner.Memory));
            Parsers.Spaces0(ref scanner, result, out _);

            // parsing the block or statement

            if (statementParser(ref scanner, result, out var body))
            {
                parsed = new For(init, condition, expressions!, body, scanner[position..scanner.Position]);
                return true;
            }
            else return Parsers.Exit(ref scanner, result, out parsed, position, orError);
        }
        else return Parsers.Exit(ref scanner, result, out parsed, position, orError);
    }

    public static bool For<TScanner>(ref TScanner scanner, ParseResult result, out For parsed, ParseError? orError = null)
        where TScanner : struct, IScanner
        => For(ref scanner, result, out parsed, StatementParsers.Statement, orError);

    public static bool ForEach<TScanner>(ref TScanner scanner, ParseResult result, out ForEach parsed, ParserDelegate<TScanner, Statement> statementParser, ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (Tokens.Literal("foreach", ref scanner, advance: true) && Parsers.Spaces0(ref scanner, result, out _))
        {
            if (Tokens.Char('(', ref scanner, advance: true) && Parsers.Spaces0(ref scanner, result, out _))
            {
                if (
                    LiteralsParser.TypeName(ref scanner, result, out var typeName, new(SDSLErrorMessages.SDSL0017, scanner[scanner.Position], scanner.Memory))
                    && Parsers.Spaces1(ref scanner, result, out _)
                    && LiteralsParser.Identifier(ref scanner, result, out var identifier, new(SDSLErrorMessages.SDSL0032, scanner[scanner.Position], scanner.Memory))
                    && Parsers.Spaces1(ref scanner, result, out _)
                )
                {
                    if (Tokens.Literal("in", ref scanner, advance: true) && Parsers.Spaces1(ref scanner, result, out _))
                    {
                        if (
                            ExpressionParser.Expression(ref scanner, result, out var collection, new(SDSLErrorMessages.SDSL0032, scanner[scanner.Position], scanner.Memory))
                            && Parsers.Spaces0(ref scanner, result, out _)
                        )
                        {
                            if (Tokens.Char(')', ref scanner, advance: true) && Parsers.Spaces0(ref scanner, result, out _))
                            {
                                if (statementParser(ref scanner, result, out var statement, new(SDSLErrorMessages.SDSL0010, scanner[scanner.Position], scanner.Memory)))
                                {
                                    parsed = new(typeName, identifier, collection, statement, scanner[position..scanner.Position]);
                                    return true;
                                }
                            }
                            else return Parsers.Exit(ref scanner, result, out parsed, position, new(SDSLErrorMessages.SDSL0018, scanner[scanner.Position], scanner.Memory));
                        }
                    }
                    else return Parsers.Exit(ref scanner, result, out parsed, position, new(SDSLErrorMessages.SDSL0001, scanner[scanner.Position], scanner.Memory));
                }
            }
            else return Parsers.Exit(ref scanner, result, out parsed, position, new(SDSLErrorMessages.SDSL0035, scanner[scanner.Position], scanner.Memory));
        }
        return Parsers.Exit(ref scanner, result, out parsed, position, orError);
    }

    public static bool ForEach<TScanner>(ref TScanner scanner, ParseResult result, out ForEach parsed, ParseError? orError = null)
        where TScanner : struct, IScanner
        => ForEach(ref scanner, result, out parsed, StatementParsers.Statement, orError);

    internal static bool AssignOrExpression<TScanner>(ref TScanner scanner, ParseResult result, out Statement parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (ExpressionParser.Expression(ref scanner, result, out var expression))
        {
            parsed = new ExpressionStatement(expression, scanner[position..scanner.Position]);
            return true;
        }
        return Parsers.Exit(ref scanner, result, out parsed, position, orError);
    }
}
