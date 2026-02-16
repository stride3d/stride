using Stride.Shaders.Core;
using Stride.Shaders.Parsing.SDSL.AST;

namespace Stride.Shaders.Parsing.SDSL;

public struct ExpressionParser : IParser<Expression>
{
    public static bool Expression<TScanner>(ref TScanner scanner, ParseResult result, out Expression parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
        => new ExpressionParser().Match(ref scanner, result, out parsed, in orError);

    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out Expression parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (Ternary(ref scanner, result, out parsed))
            return true;
        return Parsers.Exit(ref scanner, result, out parsed, position, orError);
    }

    /// <summary>
    /// <c>add ::= mul ( spaces '+' spaces add)*</c>
    /// </summary>
    public static bool Add<TScanner>(ref TScanner scanner, ParseResult result, out Expression parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        char op = '\0';
        parsed = null!;
        var position = scanner.Position;
        do
        {
            Parsers.Spaces0(ref scanner, result, out _);
            if (op != '\0' && parsed is not null)
            {
                if (Mul(ref scanner, result, out var mul))
                    parsed = new BinaryExpression(parsed, op.ToOperator(), mul, scanner[position..scanner.Position]);
                else return Parsers.Exit(ref scanner, result, out parsed, position);
            }
            else if (parsed is null && op == '\0')
            {
                if (Mul(ref scanner, result, out var mul))
                    parsed = mul;
                else return Parsers.Exit(ref scanner, result, out parsed, position, orError);
            }
        }
        while (Parsers.FollowedByAny(ref scanner, "+-", out op, withSpaces: true, advance: true));
        if (parsed is not null)
            return true;
        else return Parsers.Exit(ref scanner, result, out parsed, position, orError);
    }
    /// <summary>
    /// <c>mul ::= prefix ( spaces '*' spaces mul)*</c>
    /// </summary>
    public static bool Mul<TScanner>(ref TScanner scanner, ParseResult result, out Expression parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        char op = '\0';
        parsed = null!;
        var position = scanner.Position;
        do
        {
            Parsers.Spaces0(ref scanner, result, out _);
            if (op != '\0' && parsed is not null)
            {
                if (PrefixParser.Prefix(ref scanner, result, out var prefix))
                    parsed = new BinaryExpression(parsed, op.ToOperator(), prefix, scanner[position..scanner.Position]);
                else return Parsers.Exit(ref scanner, result, out parsed, position);
            }
            else if (parsed is null && op == '\0')
            {
                if (PrefixParser.Prefix(ref scanner, result, out var prefix))
                    parsed = prefix;
                else return Parsers.Exit(ref scanner, result, out parsed, position, orError);
            }
        }
        while (Parsers.FollowedByAny(ref scanner, "*/%", out op, withSpaces: true, advance: true));
        if (parsed is not null)
            return true;
        else return Parsers.Exit(ref scanner, result, out parsed, position, orError);
    }
    /// <summary>
    /// <c>shift ::= add ( spaces ('&lt;&lt;' | '&gt;&gt;') spaces shift)*</c>
    /// </summary>
    public static bool Shift<TScanner>(ref TScanner scanner, ParseResult result, out Expression parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        string op = "";
        parsed = null!;
        var position = scanner.Position;
        do
        {
            Parsers.Spaces0(ref scanner, result, out _);
            if (op != "" && parsed is not null)
            {
                if (Add(ref scanner, result, out var add))
                    parsed = new BinaryExpression(parsed, op.ToOperator(), add, scanner[position..scanner.Position]);
                else return Parsers.Exit(ref scanner, result, out parsed, position);
            }
            else if (parsed is null && op == "")
            {
                if (Add(ref scanner, result, out var add))
                    parsed = add;
                else return Parsers.Exit(ref scanner, result, out parsed, position, orError);
            }
        }
        while (Parsers.FollowedByAny(ref scanner, [">>", "<<"], out op, withSpaces: true, advance: true));
        if (parsed is not null)
            return true;
        else return Parsers.Exit(ref scanner, result, out parsed, position, orError);
    }
    /// <summary>
    /// <c>relational ::= shift ( spaces ('&lt;=' | '&gt;=' | '&lt;' | '&gt;') spaces relational)*</c>
    /// </summary>
    public static bool Relation<TScanner>(ref TScanner scanner, ParseResult result, out Expression parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        string op = "";
        parsed = null!;
        var position = scanner.Position;
        do
        {
            Parsers.Spaces0(ref scanner, result, out _);
            if (op != "" && parsed is not null)
            {
                if (Shift(ref scanner, result, out var shift))
                    parsed = new BinaryExpression(parsed, op.ToOperator(), shift, scanner[position..scanner.Position]);
                else return Parsers.Exit(ref scanner, result, out parsed, position);
            }
            else if (parsed is null && op == "")
            {
                if (Shift(ref scanner, result, out var shift))
                    parsed = shift;
                else return Parsers.Exit(ref scanner, result, out parsed, position, orError);
            }
            Parsers.Spaces0(ref scanner, result, out _);
        }
        while (Parsers.FollowedByAny(ref scanner, ["<=", ">=", "<", ">"], out op, withSpaces: true, advance: true));

        if (parsed is not null)
            return true;
        else return Parsers.Exit(ref scanner, result, out parsed, position, orError);
    }
    /// <summary>
    /// <c>equality ::= relational ( spaces ('==' | '!=') spaces equality)*</c>
    /// </summary>
    public static bool Equality<TScanner>(ref TScanner scanner, ParseResult result, out Expression parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        string op = "";
        parsed = null!;
        var position = scanner.Position;
        do
        {
            Parsers.Spaces0(ref scanner, result, out _);
            if (op != "" && parsed is not null)
            {
                if (Relation(ref scanner, result, out var rel))
                    parsed = new BinaryExpression(parsed, op.ToOperator(), rel, scanner[position..scanner.Position]);
                else return Parsers.Exit(ref scanner, result, out parsed, position);
            }
            else if (parsed is null && op == "")
            {
                if (Relation(ref scanner, result, out var rel))
                    parsed = rel;
                else return Parsers.Exit(ref scanner, result, out parsed, position, orError);
            }
        }
        while (Parsers.FollowedByAny(ref scanner, ["==", "!="], out op, withSpaces: true, advance: true));
        if (parsed is not null)
            return true;
        else return Parsers.Exit(ref scanner, result, out parsed, position, orError);
    }
    /// <summary>
    /// <c>band ::= equality ( spaces '&' spaces band)*</c>
    /// </summary>
    public static bool BAnd<TScanner>(ref TScanner scanner, ParseResult result, out Expression parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        string op = "";
        parsed = null!;
        var position = scanner.Position;
        do
        {
            Parsers.Spaces0(ref scanner, result, out _);
            if (op != "" && parsed is not null)
            {
                if (Equality(ref scanner, result, out var eq))
                    parsed = new BinaryExpression(parsed, op.ToOperator(), eq, scanner[position..scanner.Position]);
                else return Parsers.Exit(ref scanner, result, out parsed, position);
            }
            else if (parsed is null && op == "")
            {
                if (Equality(ref scanner, result, out var eq))
                    parsed = eq;
                else return Parsers.Exit(ref scanner, result, out parsed, position, orError);
            }
        }
        while (
            !Parsers.FollowedBy(ref scanner, Tokens.Literal("&&"), withSpaces: true)
            && Parsers.FollowedByAny(ref scanner, ["&"], out op, advance: true)
        );
        if (parsed is not null)
            return true;
        else return Parsers.Exit(ref scanner, result, out parsed, position, orError);
    }

    /// <summary>
    /// <c>bor ::= band ( spaces '|' spaces bor)*</c>
    /// </summary>

    public static bool BOr<TScanner>(ref TScanner scanner, ParseResult result, out Expression parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        string op = "";
        parsed = null!;
        var position = scanner.Position;
        do
        {
            Parsers.Spaces0(ref scanner, result, out _);
            if (op != "" && parsed is not null)
            {
                if (XOr(ref scanner, result, out var xor))
                    parsed = new BinaryExpression(parsed, op.ToOperator(), xor, scanner[position..scanner.Position]);
                else return Parsers.Exit(ref scanner, result, out parsed, position);
            }
            else if (parsed is null && op == "")
            {
                if (XOr(ref scanner, result, out var xor))
                    parsed = xor;
                else return Parsers.Exit(ref scanner, result, out parsed, position, orError);
            }
        }
        while (
            !Parsers.FollowedBy(ref scanner, Tokens.Literal("||"), withSpaces: true)
            && Parsers.FollowedByAny(ref scanner, ["|"], out op, advance: true)
        );
        if (parsed is not null)
            return true;
        else return Parsers.Exit(ref scanner, result, out parsed, position, orError);
    }
    /// <summary>
    /// <c>xor ::= bor ( spaces '^' spaces xor)*</c>
    /// </summary>

    public static bool XOr<TScanner>(ref TScanner scanner, ParseResult result, out Expression parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        string op = "";
        parsed = null!;
        var position = scanner.Position;
        do
        {
            Parsers.Spaces0(ref scanner, result, out _);
            if (op != "" && parsed is not null)
            {
                if (BAnd(ref scanner, result, out var bAnd))
                    parsed = new BinaryExpression(parsed, op.ToOperator(), bAnd, scanner[position..scanner.Position]);
                else return Parsers.Exit(ref scanner, result, out parsed, position);
            }
            else if (parsed is null && op == "")
            {
                if (BAnd(ref scanner, result, out var bAnd))
                    parsed = bAnd;
                else return Parsers.Exit(ref scanner, result, out parsed, position, orError);
            }
        }
        while (Parsers.FollowedByAny(ref scanner, ["^"], out op, advance: true));
        if (parsed is not null)
            return true;
        else return Parsers.Exit(ref scanner, result, out parsed, position, orError);
    }
    /// <summary>
    /// <c>and ::= xor ( spaces '&&' spaces and)*</c>
    /// </summary>

    public static bool And<TScanner>(ref TScanner scanner, ParseResult result, out Expression parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        string op = "";
        parsed = null!;
        var position = scanner.Position;
        do
        {
            Parsers.Spaces0(ref scanner, result, out _);
            if (op != "" && parsed is not null)
            {
                if (BOr(ref scanner, result, out var bOr))
                    parsed = new BinaryExpression(parsed, op.ToOperator(), bOr, scanner[position..scanner.Position]);
                else return Parsers.Exit(ref scanner, result, out parsed, position);
            }
            else if (parsed is null && op == "")
            {
                if (BOr(ref scanner, result, out var bOr))
                    parsed = bOr;
                else return Parsers.Exit(ref scanner, result, out parsed, position, orError);
            }
        }
        while (Parsers.FollowedByAny(ref scanner, ["&&"], out op, advance: true));
        if (parsed is not null)
            return true;
        else return Parsers.Exit(ref scanner, result, out parsed, position, orError);
    }
    /// <summary>
    /// <c>or ::= and ( spaces '&&' spaces or)*</c>
    /// </summary>
    public static bool Or<TScanner>(ref TScanner scanner, ParseResult result, out Expression parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        string op = "";
        parsed = null!;
        var position = scanner.Position;
        do
        {
            Parsers.Spaces0(ref scanner, result, out _);
            if (op != "" && parsed is not null)
            {
                if (And(ref scanner, result, out var and))
                    parsed = new BinaryExpression(parsed, op.ToOperator(), and, scanner[position..scanner.Position]);
                else return Parsers.Exit(ref scanner, result, out parsed, position);
            }
            else if (parsed is null && op == "")
            {
                if (And(ref scanner, result, out var and))
                    parsed = and;
                else return Parsers.Exit(ref scanner, result, out parsed, position, orError);
            }
        }
        while (Parsers.FollowedByAny(ref scanner, ["||"], out op, advance: true));
        if (parsed is not null)
            return true;
        else return Parsers.Exit(ref scanner, result, out parsed, position, orError);
    }
    /// <summary>
    /// <c>ternary ::= or ( spaces '?' spaces expression spaces ':' spaces expression)*</c>
    /// </summary>
    public static bool Ternary<TScanner>(ref TScanner scanner, ParseResult result, out Expression parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (Or(ref scanner, result, out parsed))
        {
            var pos2 = scanner.Position;
            Parsers.Spaces0(ref scanner, result, out _);
            if (Tokens.Char('?', ref scanner, advance: true))
            {

                Parsers.Spaces0(ref scanner, result, out _);
                if (Expression(ref scanner, result, out var left, new(SDSLErrorMessages.SDSL0015, scanner[scanner.Position], scanner.Memory)))
                {
                    Parsers.Spaces0(ref scanner, result, out _);
                    if (Tokens.Char(':', ref scanner, advance: true))
                    {
                        Parsers.Spaces0(ref scanner, result, out _);
                        if (Expression(ref scanner, result, out var right, new(SDSLErrorMessages.SDSL0015, scanner[scanner.Position], scanner.Memory)))
                        {
                            parsed = new TernaryExpression(parsed, left, right, scanner[position..scanner.Position]);
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
        return Parsers.Exit(ref scanner, result, out parsed, position, orError);
    }
}
