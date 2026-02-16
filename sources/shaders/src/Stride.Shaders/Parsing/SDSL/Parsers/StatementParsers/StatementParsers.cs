using Stride.Shaders.Core;
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
        else if (Break(ref scanner, result, out parsed))
            return true;
        else if (Discard(ref scanner, result, out parsed))
            return true;
        else if (Return(ref scanner, result, out parsed))
            return true;
        else if (Continue(ref scanner, result, out parsed))
            return true;
        else if (Declare(ref scanner, result, out parsed))
            return true;
        else if (!Tokens.Char('{', ref scanner) && Expression(ref scanner, result, out parsed))
            return true;
        else if (!Tokens.Char('{', ref scanner) && Assignments(ref scanner, result, out parsed))
            return true;
        else if (Block(ref scanner, result, out parsed))
            return true;
        return Parsers.Exit(ref scanner, result, out parsed, position, orError);
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
    internal static bool Discard<TScanner>(ref TScanner scanner, ParseResult result, out Statement parsed, ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (
            Parsers.FollowedBy(ref scanner, Tokens.Literal("discard"), withSpaces: true, advance: true)
            && Parsers.FollowedBy(ref scanner, Tokens.Char(';'), withSpaces: true, advance: true)
        )
        {
            parsed = new Discard(scanner[position..scanner.Position]);
            return true;
        }
        else return Parsers.Exit(ref scanner, result, out parsed, position, orError);
    }
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
    internal static bool DeclareOrAssign<TScanner>(ref TScanner scanner, ParseResult result, out Statement parsed, ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (Assignments(ref scanner, result, out parsed, orError))
            return true;
        else if (Declare(ref scanner, result, out parsed, orError))
            return true;
        else return Parsers.Exit(ref scanner, result, out parsed, position, orError);
    }
    internal static bool AssignOrExpression<TScanner>(ref TScanner scanner, ParseResult result, out Statement parsed, ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (Assignments(ref scanner, result, out parsed, orError))
            return true;
        else if (Expression(ref scanner, result, out parsed, orError))
            return true;
        else return Parsers.Exit(ref scanner, result, out parsed, position, orError);
    }
    internal static bool VarAssign<TScanner>(ref TScanner scanner, ParseResult result, out VariableAssign parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
        => new VariableAssignParser().Match(ref scanner, result, out parsed, orError);
    internal static bool DeclaredVarAssign<TScanner>(ref TScanner scanner, ParseResult result, out DeclaredVariableAssign parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
        => new DeclaredVariableAssignParser().Match(ref scanner, result, out parsed, orError);
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
        if (Tokens.Char(';', ref scanner, advance: true))
        {
            parsed = new EmptyStatement(scanner[position..scanner.Position]);
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
        if (Tokens.Literal("return;", ref scanner, advance: true))
        {
            parsed = new Return(scanner[position..scanner.Position]);
            return true;
        }
        else if (
            Tokens.Literal("return", ref scanner, advance: true)
        )
        {

            if (Parsers.FollowedBy(ref scanner, Tokens.Char(';'), withSpaces: true, advance: true))
            {
                parsed = new Return(scanner[position..scanner.Position]);
                return true;
            }
            else if (
                Parsers.FollowedByDel(ref scanner, result, PrimaryParsers.Parenthesis, out Expression p, advance : true)
                && Parsers.FollowedBy(ref scanner, Tokens.Char(';'), withSpaces: true, advance: true)
            )
            {
                parsed = new Return(scanner[position..scanner.Position], p);
                return true;
            }
            else if (
                Parsers.Spaces1(ref scanner, result, out _)
                && ExpressionParser.Expression(ref scanner, result, out var val)
                && Parsers.Spaces0(ref scanner, result, out _)
                && Tokens.Char(';', ref scanner, advance: true)
            )
            {
                parsed = new Return(scanner[position..scanner.Position], val);
                return true;
            }
            else return Parsers.Exit(ref scanner, result, out parsed, position, new(SDSLErrorMessages.SDSL0041, scanner[scanner.Position], scanner.Memory));
        }
        else return Parsers.Exit(ref scanner, result, out parsed, position, orError);
    }
}

public record struct BreakParser : IParser<Statement>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out Statement parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (
            Tokens.Literal("break", ref scanner, advance: true)
            && Parsers.Spaces0(ref scanner, result, out _)
            && Tokens.Char(';', ref scanner, advance: true)
        )
        {
            parsed = new Break(scanner[position..scanner.Position]);
            return true;
        }
        else return Parsers.Exit(ref scanner, result, out parsed, position, orError);
    }
}
public record struct ContinueParser : IParser<Statement>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out Statement parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (
            Tokens.Literal("continue", ref scanner, advance: true)
            && Parsers.Spaces0(ref scanner, result, out _)
            && Tokens.Char(';', ref scanner, advance: true)
        )
        {
            parsed = new Continue(scanner[position..scanner.Position]);
            return true;
        }
        else return Parsers.Exit(ref scanner, result, out parsed, position, orError);
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
            && Parsers.FollowedBy(ref scanner, Tokens.Char(';'), advance: true)
        )
        {
            parsed = new ExpressionStatement(expression, scanner[position..scanner.Position]);
            return true;
        }
        else return Parsers.Exit(ref scanner, result, out parsed, position, orError);
    }
}




public record struct BlockStatementParser : IParser<Statement>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out Statement parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (Tokens.Char('{', ref scanner, advance: true) && Parsers.Spaces0(ref scanner, result, out _))
        {
            var block = new BlockStatement(new());

            while (!scanner.IsEof && !Tokens.Char('}', ref scanner, advance: true))
            {
                if (StatementParsers.Statement(ref scanner, result, out var statement))
                {
                    block.Statements.Add(statement);
                    Parsers.Spaces0(ref scanner, result, out _);
                }
                else return Parsers.Exit(ref scanner, result, out parsed, position, new(SDSLErrorMessages.SDSL0010, scanner[scanner.Position], scanner.Memory));
            }
            block.Info = scanner[position..scanner.Position];
            parsed = block;
            return true;
        }
        else return Parsers.Exit(ref scanner, result, out parsed, position, orError);
    }
}



public record struct VariableAssignParser : IParser<VariableAssign>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out VariableAssign parsed, in ParseError? orError = null) where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (PostfixParser.Postfix(ref scanner, result, out var p))
        {
            if (
                Parsers.FollowedBy(
                    ref scanner,
                    result,
                    (ref TScanner s, ParseResult result, out AssignOperator op, in ParseError? orError = null) => LiteralsParser.AssignOperators(ref s, null!, out op) && Parsers.Spaces0(ref s, result, out _),
                    out var op,
                    withSpaces: true,
                    advance: true)
            )
            {
                Parsers.Spaces0(ref scanner, result, out _);
                if (ExpressionParser.Expression(ref scanner, result, out var expression))
                {
                    parsed = new(p, false, scanner[position..scanner.Position], op, expression);
                    return true;
                }
                else return Parsers.Exit(ref scanner, result, out parsed, position, new(SDSLErrorMessages.SDSL0015, scanner[position], scanner.Memory));
            }
            else
            {
                parsed = new(p, false, scanner[position..scanner.Position]);
                return true;
            }
        }
        else return Parsers.Exit(ref scanner, result, out parsed, position);
    }
}

public record struct DeclaredVariableAssignParser : IParser<DeclaredVariableAssign>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out DeclaredVariableAssign parsed, in ParseError? orError = null) where TScanner : struct, IScanner
    {
        var position = scanner.Position;

        if (Parsers.IdentifierArraySizeOptionalValue(ref scanner, result, out var identifier, out var arraySizes, out var value, advance: true))
        {
            if (
                Parsers.FollowedBy(
                    ref scanner,
                    result,
                    (ref TScanner s, ParseResult result, out AssignOperator op, in ParseError? orError = null) => LiteralsParser.AssignOperators(ref s, null!, out op) && Parsers.Spaces0(ref s, result, out _),
                    out var op,
                    withSpaces: true,
                    advance: true)
            )
            {
                Parsers.Spaces0(ref scanner, result, out _);
                if (ExpressionParser.Expression(ref scanner, result, out var expression))
                {
                    parsed = new(identifier, false, scanner[position..scanner.Position], op, expression)
                    {
                        ArraySizes = arraySizes,
                        Value = value
                    };
                    return true;
                }
                else return Parsers.Exit(ref scanner, result, out parsed, position, new(SDSLErrorMessages.SDSL0015, scanner[position], scanner.Memory));
            }
            else
            {
                parsed = new(identifier, false, scanner[position..scanner.Position])
                {
                    ArraySizes = arraySizes,
                    Value = value
                };
                return true;
            }
        }
        else return Parsers.Exit(ref scanner, result, out parsed, position);
    }
}



public record struct DeclareStatementParser : IParser<Statement>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out Statement parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        var isConst =
            Tokens.Literal("const", ref scanner, advance: true) && Parsers.Spaces1(ref scanner, result, out _)
            || Parsers.SequenceOf(ref scanner, ["static", "const"], advance: true) && Parsers.Spaces0(ref scanner, result, out _);
        if (!isConst)
            scanner.Position = position;
        if (
            LiteralsParser.TypeName(ref scanner, result, out var typeName)
            && Parsers.Spaces1(ref scanner, result, out _)

        )
        {
            if (Parsers.Repeat(ref scanner, result, StatementParsers.DeclaredVarAssign, out List<DeclaredVariableAssign> assigns, 1, true, ","))
            {
                foreach (var a in assigns)
                {
                    a.IsConst = isConst;
                    a.ReplaceTypeName(typeName);
                }
                Parsers.Spaces0(ref scanner, result, out _);
                if (Tokens.Char(';', ref scanner, advance: true))
                {
                    parsed = new Declare(typeName, scanner[position..scanner.Position])
                    {
                        Variables = assigns
                    };
                    return true;
                }
                else return Parsers.Exit(ref scanner, result, out parsed, position, new(SDSLErrorMessages.SDSL0033, scanner[scanner.Position], scanner.Memory));
            }
        }
        return Parsers.Exit(ref scanner, result, out parsed, position, orError);
    }
}

public record struct AssignmentsParser : IParser<Statement>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out Statement parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (Parsers.Repeat<TScanner, VariableAssign>(ref scanner, result, StatementParsers.VarAssign, out var assigns, 1, true, ","))
        {
            Parsers.Spaces0(ref scanner, result, out _);
            if (Tokens.Char(';', ref scanner, advance: true))
            {
                parsed = new Assign(scanner[position..scanner.Position])
                {
                    Variables = assigns
                };
                return true;
            }
            else return Parsers.Exit(ref scanner, result, out parsed, position, new(SDSLErrorMessages.SDSL0033, scanner[scanner.Position], scanner.Memory));
        }
        else return Parsers.Exit(ref scanner, result, out parsed, position, orError);
    }
}
