using Stride.Shaders.Parsing.SDSL.AST;

namespace Stride.Shaders.Parsing.SDSL;


public record struct StatementParsers : IParser<Statement>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out Statement parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (Empty(ref scanner, result, out parsed))
            return true;
        else if (Controls(ref scanner, result, out var cond))
        {
            parsed = cond;
            return true;
        }
        else if (Flow(ref scanner, result, out var flow))
        {
            parsed = flow;
            return true;
        }
        else if (Expression(ref scanner, result, out parsed))
            return true;
        else if (Break(ref scanner, result, out parsed))
            return true;
        else if (Return(ref scanner, result, out parsed))
            return true;
        else if (Continue(ref scanner, result, out parsed))
            return true;
        else if (Declare(ref scanner, result, out parsed))
            return true;
        else if (Assignments(ref scanner, result, out parsed))
            return true;
        else if (Block(ref scanner, result, out parsed))
            return true;
        return CommonParsers.Exit(ref scanner, result, out parsed, position, orError);
    }
    internal static bool Statement<TScanner>(ref TScanner scanner, ParseResult result, out Statement parsed, ParseError? orError = null)
        where TScanner : struct, IScanner
        => new StatementParsers().Match(ref scanner, result, out parsed, orError);
    internal static bool Empty<TScanner>(ref TScanner scanner, ParseResult result, out Statement parsed, ParseError? orError = null)
        where TScanner : struct, IScanner
        => new EmptyStatementParser().Match(ref scanner, result, out parsed, orError);
    internal static bool Block<TScanner>(ref TScanner scanner, ParseResult result, out Statement parsed, ParseError? orError = null)
        where TScanner : struct, IScanner
        => new BlockStatementParser().Match(ref scanner, result, out parsed, orError);
    internal static bool Break<TScanner>(ref TScanner scanner, ParseResult result, out Statement parsed, ParseError? orError = null)
        where TScanner : struct, IScanner
        => new BreakParser().Match(ref scanner, result, out parsed, orError);
    internal static bool Return<TScanner>(ref TScanner scanner, ParseResult result, out Statement parsed, ParseError? orError = null)
        where TScanner : struct, IScanner
        => new ReturnStatementParser().Match(ref scanner, result, out parsed, orError);
    internal static bool Continue<TScanner>(ref TScanner scanner, ParseResult result, out Statement parsed, ParseError? orError = null)
        where TScanner : struct, IScanner
    => new ContinueParser().Match(ref scanner, result, out parsed, orError);
    internal static bool Expression<TScanner>(ref TScanner scanner, ParseResult result, out Statement parsed, ParseError? orError = null)
        where TScanner : struct, IScanner
        => new ExpressionStatementParser().Match(ref scanner, result, out parsed, orError);
    internal static bool Declare<TScanner>(ref TScanner scanner, ParseResult result, out Statement parsed, ParseError? orError = null)
        where TScanner : struct, IScanner
        => new DeclareStatementParser().Match(ref scanner, result, out parsed, orError);
    internal static bool Assignments<TScanner>(ref TScanner scanner, ParseResult result, out Statement parsed, ParseError? orError = null)
        where TScanner : struct, IScanner
        => new AssignmentsParser().Match(ref scanner, result, out parsed, orError);
    internal static bool VarAssign<TScanner>(ref TScanner scanner, ParseResult result, out VariableAssign parsed, ParseError? orError = null)
        where TScanner : struct, IScanner
        => new VariableAssignParser().Match(ref scanner, result, out parsed, orError);
    internal static bool Controls<TScanner>(ref TScanner scanner, ParseResult result, out ConditionalFlow parsed, ParseError? orError = null)
       where TScanner : struct, IScanner
       => new ControlsParser().Match(ref scanner, result, out parsed, orError);
    internal static bool Flow<TScanner>(ref TScanner scanner, ParseResult result, out Flow parsed, ParseError? orError = null)
      where TScanner : struct, IScanner
      => new FlowParsers().Match(ref scanner, result, out parsed, orError);
}



public record struct EmptyStatementParser : IParser<Statement>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out Statement parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        parsed = null!;
        var position = scanner.Position;
        if(Terminals.Char(';', ref scanner, advance : true))
        {
            parsed = new EmptyStatement(scanner.GetLocation(position..scanner.Position));
            return true;
        }
        return false;
    }
}


public record struct ReturnStatementParser : IParser<Statement>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out Statement parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (Terminals.Literal("return;", ref scanner, advance: true))
        {
            parsed = new Return(scanner.GetLocation(position..scanner.Position));
            return true;
        }
        else if (
            Terminals.Literal("return", ref scanner, advance: true)
            && CommonParsers.Spaces1(ref scanner, result, out _, new(SDSLErrors.SDSL0016, scanner.GetErrorLocation(scanner.Position), scanner.Memory))
        )
        {
            if (Terminals.Char(';', ref scanner, advance: true))
            {
                parsed = new Return(scanner.GetLocation(position..scanner.Position));
                return true;
            }
            else if (
                ExpressionParser.Expression(ref scanner, result, out var val)
                && CommonParsers.Spaces0(ref scanner, result, out _)
                && Terminals.Char(';', ref scanner, advance: true)
            )
            {
                parsed = new Return(scanner.GetLocation(position, scanner.Position - position), val);
                return true;
            }
            else return CommonParsers.Exit(ref scanner, result, out parsed, position, new(SDSLErrors.SDSL0041, scanner.GetErrorLocation(scanner.Position), scanner.Memory));


        }
        else return CommonParsers.Exit(ref scanner, result, out parsed, position, orError);
    }
}

public record struct BreakParser : IParser<Statement>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out Statement parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (
            Terminals.Literal("break", ref scanner, advance: true)
            && CommonParsers.Spaces0(ref scanner, result, out _)
            && Terminals.Char(';', ref scanner, advance: true)
        )
        {
            parsed = new Break(scanner.GetLocation(position, scanner.Position - position));
            return true;
        }
        else return CommonParsers.Exit(ref scanner, result, out parsed, position, orError);
    }
}
public record struct ContinueParser : IParser<Statement>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out Statement parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (
            Terminals.Literal("continue", ref scanner, advance: true)
            && CommonParsers.Spaces0(ref scanner, result, out _)
            && Terminals.Char(';', ref scanner, advance: true)
        )
        {
            parsed = new Break(scanner.GetLocation(position, scanner.Position - position));
            return true;
        }
        else return CommonParsers.Exit(ref scanner, result, out parsed, position, orError);
    }
}

public record struct ExpressionStatementParser : IParser<Statement>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out Statement parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (
            ExpressionParser.Expression(ref scanner, result, out var expression)
            && CommonParsers.Spaces0(ref scanner, result, out _)
            && Terminals.Char(';', ref scanner, advance: true)
        )
        {
            parsed = new ExpressionStatement(expression, scanner.GetLocation(position, scanner.Position - position));
            return true;
        }
        else return CommonParsers.Exit(ref scanner, result, out parsed, position, orError);
    }
}




public record struct BlockStatementParser : IParser<Statement>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out Statement parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (Terminals.Char('{', ref scanner, advance: true) && CommonParsers.Spaces0(ref scanner, result, out _))
        {
            var block = new BlockStatement(new());

            while (!scanner.IsEof && !Terminals.Char('}', ref scanner, advance: true))
            {
                if (StatementParsers.Statement(ref scanner, result, out var statement))
                {
                    block.Statements.Add(statement);
                    CommonParsers.Spaces0(ref scanner, result, out _);
                }
                else return CommonParsers.Exit(ref scanner, result, out parsed, position, new(SDSLErrors.SDSL0010, scanner.GetErrorLocation(scanner.Position), scanner.Memory));
            }
            block.Info = scanner.GetLocation(position, scanner.Position - position);
            parsed = block;
            return true;
        }
        else return CommonParsers.Exit(ref scanner, result, out parsed, position, orError);
    }
}



public record struct VariableAssignParser : IParser<VariableAssign>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out VariableAssign parsed, in ParseError? orError = null) where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (PostfixParser.Postfix(ref scanner, result, out var identifier))
        {
            if (
                CommonParsers.FollowedBy(
                    ref scanner,
                    result,
                    (ref TScanner s, ParseResult result, out AssignOperator op, ParseError? orError = null) => LiteralsParser.AssignOperators(ref s, null!, out op) && CommonParsers.Spaces0(ref s, result, out _),
                    out var op,
                    withSpaces: true,
                    advance: true)
            )
            {
                CommonParsers.Spaces0(ref scanner, result, out _);
                if (ExpressionParser.Expression(ref scanner, result, out var expression))
                {
                    parsed = new(identifier, scanner.GetLocation(position..scanner.Position), op, expression);
                    return true;
                }
                else return CommonParsers.Exit(ref scanner, result, out parsed, position, new(SDSLErrors.SDSL0015, scanner.GetErrorLocation(position), scanner.Memory));
            }
            else
            {
                parsed = new(identifier, scanner.GetLocation(position..scanner.Position));
                return true;
            }
        }
        else return CommonParsers.Exit(ref scanner, result, out parsed, position);
    }
}



public record struct DeclareStatementParser : IParser<Statement>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out Statement parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (
            LiteralsParser.TypeName(ref scanner, result, out var typeName)
            && CommonParsers.Spaces1(ref scanner, result, out _)

        )
        {
            if (CommonParsers.Repeat<TScanner, VariableAssign>(ref scanner, StatementParsers.VarAssign, result, out var assigns, 1, true, ","))
            {
                CommonParsers.Spaces0(ref scanner, result, out _);
                if (Terminals.Char(';', ref scanner, advance: true))
                {
                    parsed = new Declare(typeName, scanner.GetLocation(position..scanner.Position))
                    {
                        Variables = assigns
                    };
                    return true;
                }
                else return CommonParsers.Exit(ref scanner, result, out parsed, position, new(SDSLErrors.SDSL0033, scanner.GetErrorLocation(scanner.Position), scanner.Memory));
            }
        }
        return CommonParsers.Exit(ref scanner, result, out parsed, position, orError);
    }
}

public record struct AssignmentsParser : IParser<Statement>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out Statement parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (CommonParsers.Repeat<TScanner, VariableAssign>(ref scanner, StatementParsers.VarAssign, result, out var assigns, 1, true, ","))
        {
            CommonParsers.Spaces0(ref scanner, result, out _);
            if (Terminals.Char(';', ref scanner, advance: true))
            {
                parsed = new Assign(scanner.GetLocation(position..scanner.Position))
                {
                    Variables = assigns
                };
                return true;
            }
            else return CommonParsers.Exit(ref scanner, result, out parsed, position, new(SDSLErrors.SDSL0033, scanner.GetErrorLocation(scanner.Position), scanner.Memory));
        }
        else return CommonParsers.Exit(ref scanner, result, out parsed, position, orError);
    }
}
