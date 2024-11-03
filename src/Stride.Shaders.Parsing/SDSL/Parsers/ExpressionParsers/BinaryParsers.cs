using Stride.Shaders.Parsing.SDSL.AST;

namespace Stride.Shaders.Parsing.SDSL;

public struct ExpressionParser : IParser<Expression>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out Expression parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (Ternary(ref scanner, result, out parsed))
            return true;
        return CommonParsers.Exit(ref scanner, result, out parsed, position, orError);
    }

    public static bool Expression<TScanner>(ref TScanner scanner, ParseResult result, out Expression parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
        => new ExpressionParser().Match(ref scanner, result, out parsed, in orError);
    public static bool Add<TScanner>(ref TScanner scanner, ParseResult result, out Expression parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
        => new AdditionParser().Match(ref scanner, result, out parsed, in orError);
    public static bool Mul<TScanner>(ref TScanner scanner, ParseResult result, out Expression parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
        => new MultiplicationParser().Match(ref scanner, result, out parsed, in orError);
    public static bool Shift<TScanner>(ref TScanner scanner, ParseResult result, out Expression parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
        => new BitwiseShiftParser().Match(ref scanner, result, out parsed, in orError);
    public static bool Relation<TScanner>(ref TScanner scanner, ParseResult result, out Expression parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
        => new RelationalParser().Match(ref scanner, result, out parsed, in orError);
    public static bool Equality<TScanner>(ref TScanner scanner, ParseResult result, out Expression parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
        => new EqualityParser().Match(ref scanner, result, out parsed, in orError);
    public static bool BAnd<TScanner>(ref TScanner scanner, ParseResult result, out Expression parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
        => new BitwiseAndParser().Match(ref scanner, result, out parsed, in orError);
    public static bool BOr<TScanner>(ref TScanner scanner, ParseResult result, out Expression parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
        => new BitwiseOrParser().Match(ref scanner, result, out parsed, in orError);
    public static bool XOr<TScanner>(ref TScanner scanner, ParseResult result, out Expression parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
        => new BitwiseXOrParser().Match(ref scanner, result, out parsed, in orError);
    public static bool And<TScanner>(ref TScanner scanner, ParseResult result, out Expression parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
        => new AndParser().Match(ref scanner, result, out parsed, in orError);
    public static bool Or<TScanner>(ref TScanner scanner, ParseResult result, out Expression parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
        => new OrParser().Match(ref scanner, result, out parsed, in orError);
    public static bool Ternary<TScanner>(ref TScanner scanner, ParseResult result, out Expression parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
        => new TernaryParser().Match(ref scanner, result, out parsed, in orError);
}


public record struct TernaryParser : IParser<Expression>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out Expression parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (ExpressionParser.Or(ref scanner, result, out parsed))
        {
            var pos2 = scanner.Position;
            CommonParsers.Spaces0(ref scanner, result, out _);
            if (Terminals.Char('?', ref scanner, advance: true))
            {

                CommonParsers.Spaces0(ref scanner, result, out _);
                if (ExpressionParser.Expression(ref scanner, result, out var left, new(SDSLParsingMessages.SDSL0015, scanner.GetErrorLocation(scanner.Position), scanner.Memory)))
                {
                    CommonParsers.Spaces0(ref scanner, result, out _);
                    if (Terminals.Char(':', ref scanner, advance: true))
                    {
                        CommonParsers.Spaces0(ref scanner, result, out _);
                        if (ExpressionParser.Expression(ref scanner, result, out var right, new(SDSLParsingMessages.SDSL0015, scanner.GetErrorLocation(scanner.Position), scanner.Memory)))
                        {
                            parsed = new TernaryExpression(parsed, left, right, scanner.GetLocation(position, scanner.Position - position));
                            return true;
                        }
                    }
                }
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

public record struct OrParser() : IParser<Expression>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out Expression parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        string op = "";
        parsed = null!;
        var position = scanner.Position;
        do
        {
            CommonParsers.Spaces0(ref scanner, result, out _);
            if (op != "" && parsed is not null)
            {
                if (ExpressionParser.And(ref scanner, result, out var and))
                    parsed = new BinaryExpression(parsed, op.ToOperator(), and, scanner.GetLocation(position..scanner.Position));
                else return CommonParsers.Exit(ref scanner, result, out parsed, position);
            }
            else if (parsed is null && op == "")
            {
                if (ExpressionParser.And(ref scanner, result, out var and))
                    parsed = and;
                else return CommonParsers.Exit(ref scanner, result, out parsed, position, orError);
            }
        }
        while (Terminals.AnyOf(["||"], ref scanner, out op, advance: true));
        if (parsed is not null)
            return true;
        else return CommonParsers.Exit(ref scanner, result, out parsed, position, orError);
    }
}

public record struct AndParser() : IParser<Expression>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out Expression parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        string op = "";
        parsed = null!;
        var position = scanner.Position;
        do
        {
            CommonParsers.Spaces0(ref scanner, result, out _);
            if (op != "" && parsed is not null)
            {
                if (ExpressionParser.BOr(ref scanner, result, out var bOr))
                    parsed = new BinaryExpression(parsed, op.ToOperator(), bOr, scanner.GetLocation(position..scanner.Position));
                else return CommonParsers.Exit(ref scanner, result, out parsed, position);
            }
            else if (parsed is null && op == "")
            {
                if (ExpressionParser.BOr(ref scanner, result, out var bOr))
                    parsed = bOr;
                else return CommonParsers.Exit(ref scanner, result, out parsed, position, orError);
            }
        }
        while (Terminals.AnyOf(["&&"], ref scanner, out op, advance: true));
        if (parsed is not null)
            return true;
        else return CommonParsers.Exit(ref scanner, result, out parsed, position, orError);
    }
}

public record struct BitwiseOrParser() : IParser<Expression>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out Expression parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        string op = "";
        parsed = null!;
        var position = scanner.Position;
        do
        {
            CommonParsers.Spaces0(ref scanner, result, out _);
            if (op != "" && parsed is not null)
            {
                if (ExpressionParser.XOr(ref scanner, result, out var xor))
                    parsed = new BinaryExpression(parsed, op.ToOperator(), xor, scanner.GetLocation(position..scanner.Position));
                else return CommonParsers.Exit(ref scanner, result, out parsed, position);
            }
            else if (parsed is null && op == "")
            {
                if (ExpressionParser.XOr(ref scanner, result, out var xor))
                    parsed = xor;
                else return CommonParsers.Exit(ref scanner, result, out parsed, position, orError);
            }
        }
        while (!Terminals.Literal("||", ref scanner) && Terminals.AnyOf(["|"], ref scanner, out op, advance: true));
        if (parsed is not null)
            return true;
        else return CommonParsers.Exit(ref scanner, result, out parsed, position, orError);
    }
}
public record struct BitwiseXOrParser() : IParser<Expression>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out Expression parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        string op = "";
        parsed = null!;
        var position = scanner.Position;
        do
        {
            CommonParsers.Spaces0(ref scanner, result, out _);
            if (op != "" && parsed is not null)
            {
                if (ExpressionParser.BAnd(ref scanner, result, out var bAnd))
                    parsed = new BinaryExpression(parsed, op.ToOperator(), bAnd, scanner.GetLocation(position..scanner.Position));
                else return CommonParsers.Exit(ref scanner, result, out parsed, position);
            }
            else if (parsed is null && op == "")
            {
                if (ExpressionParser.BAnd(ref scanner, result, out var bAnd))
                    parsed = bAnd;
                else return CommonParsers.Exit(ref scanner, result, out parsed, position, orError);
            }
        }
        while (Terminals.AnyOf(["^"], ref scanner, out op, advance: true));
        if (parsed is not null)
            return true;
        else return CommonParsers.Exit(ref scanner, result, out parsed, position, orError);
    }
}
public record struct BitwiseAndParser() : IParser<Expression>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out Expression parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        string op = "";
        parsed = null!;
        var position = scanner.Position;
        do
        {
            CommonParsers.Spaces0(ref scanner, result, out _);
            if (op != "" && parsed is not null)
            {
                if (ExpressionParser.Equality(ref scanner, result, out var eq))
                    parsed = new BinaryExpression(parsed, op.ToOperator(), eq, scanner.GetLocation(position..scanner.Position));
                else return CommonParsers.Exit(ref scanner, result, out parsed, position);
            }
            else if (parsed is null && op == "")
            {
                if (ExpressionParser.Equality(ref scanner, result, out var eq))
                    parsed = eq;
                else return CommonParsers.Exit(ref scanner, result, out parsed, position, orError);
            }
        }
        while (!Terminals.Literal("&&", ref scanner) && Terminals.AnyOf(["&"], ref scanner, out op, advance: true));
        if (parsed is not null)
            return true;
        else return CommonParsers.Exit(ref scanner, result, out parsed, position, orError);
    }
}



public record struct EqualityParser() : IParser<Expression>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out Expression parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        string op = "";
        parsed = null!;
        var position = scanner.Position;
        do
        {
            CommonParsers.Spaces0(ref scanner, result, out _);
            if (op != "" && parsed is not null)
            {
                if (ExpressionParser.Relation(ref scanner, result, out var rel))
                    parsed = new BinaryExpression(parsed, op.ToOperator(), rel, scanner.GetLocation(position..scanner.Position));
                else return CommonParsers.Exit(ref scanner, result, out parsed, position);
            }
            else if (parsed is null && op == "")
            {
                if (ExpressionParser.Relation(ref scanner, result, out var rel))
                    parsed = rel;
                else return CommonParsers.Exit(ref scanner, result, out parsed, position, orError);
            }
        }
        while (Terminals.AnyOf(["==", "!="], ref scanner, out op, advance: true));
        if (parsed is not null)
            return true;
        else return CommonParsers.Exit(ref scanner, result, out parsed, position, orError);
    }
}

public record struct RelationalParser() : IParser<Expression>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out Expression parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        string op = "";
        parsed = null!;
        var position = scanner.Position;
        do
        {
            CommonParsers.Spaces0(ref scanner, result, out _);
            if (op != "" && parsed is not null)
            {
                if (ExpressionParser.Shift(ref scanner, result, out var shift))
                    parsed = new BinaryExpression(parsed, op.ToOperator(), shift, scanner.GetLocation(position..scanner.Position));
                else return CommonParsers.Exit(ref scanner, result, out parsed, position);
            }
            else if (parsed is null && op == "")
            {
                if (ExpressionParser.Shift(ref scanner, result, out var shift))
                    parsed = shift;
                else return CommonParsers.Exit(ref scanner, result, out parsed, position, orError);
            }
            CommonParsers.Spaces0(ref scanner, result, out _);
        }
        while (Terminals.AnyOf(["<=", ">=", "<", ">"], ref scanner, out op, advance: true));

        if (parsed is not null)
            return true;
        else return CommonParsers.Exit(ref scanner, result, out parsed, position, orError);
    }
}

public record struct BitwiseShiftParser() : IParser<Expression>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out Expression parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        string op = "";
        parsed = null!;
        var position = scanner.Position;
        do
        {
            CommonParsers.Spaces0(ref scanner, result, out _);
            if (op != "" && parsed is not null)
            {
                if (ExpressionParser.Add(ref scanner, result, out var add))
                    parsed = new BinaryExpression(parsed, op.ToOperator(), add, scanner.GetLocation(position..scanner.Position));
                else return CommonParsers.Exit(ref scanner, result, out parsed, position);
            }
            else if (parsed is null && op == "")
            {
                if (ExpressionParser.Add(ref scanner, result, out var add))
                    parsed = add;
                else return CommonParsers.Exit(ref scanner, result, out parsed, position, orError);
            }
        }
        while (Terminals.AnyOf([">>", "<<"], ref scanner, out op, advance: true));
        if (parsed is not null)
            return true;
        else return CommonParsers.Exit(ref scanner, result, out parsed, position, orError);
    }
}

public record struct AdditionParser() : IParser<Expression>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out Expression parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        char op = '\0';
        parsed = null!;
        var position = scanner.Position;
        do
        {
            CommonParsers.Spaces0(ref scanner, result, out _);
            if (op != '\0' && parsed is not null)
            {
                if (ExpressionParser.Mul(ref scanner, result, out var mul))
                    parsed = new BinaryExpression(parsed, op.ToOperator(), mul, scanner.GetLocation(position..scanner.Position));
                else return CommonParsers.Exit(ref scanner, result, out parsed, position);
            }
            else if (parsed is null && op == '\0')
            {
                if (ExpressionParser.Mul(ref scanner, result, out var mul))
                    parsed = mul;
                else return CommonParsers.Exit(ref scanner, result, out parsed, position, orError);
            }
        }
        while (Terminals.Set("+-", ref scanner, out op, advance: true));
        if (parsed is not null)
            return true;
        else return CommonParsers.Exit(ref scanner, result, out parsed, position, orError);
    }
}

public record struct MultiplicationParser() : IParser<Expression>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out Expression parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        char op = '\0';
        parsed = null!;
        var position = scanner.Position;
        do
        {
            CommonParsers.Spaces0(ref scanner, result, out _);
            if (op != '\0' && parsed is not null)
            {
                if (UnaryParsers.Prefix(ref scanner, result, out var prefix))
                    parsed = new BinaryExpression(parsed, op.ToOperator(), prefix, scanner.GetLocation(position..scanner.Position));
                else return CommonParsers.Exit(ref scanner, result, out parsed, position);
            }
            else if (parsed is null && op == '\0')
            {
                if (UnaryParsers.Prefix(ref scanner, result, out var prefix))
                    parsed = prefix;
                else return CommonParsers.Exit(ref scanner, result, out parsed, position, orError);
            }
        }
        while (Terminals.Set("*/%", ref scanner, out op, advance: true));
        if (parsed is not null)
            return true;
        else return CommonParsers.Exit(ref scanner, result, out parsed, position, orError);
    }
}