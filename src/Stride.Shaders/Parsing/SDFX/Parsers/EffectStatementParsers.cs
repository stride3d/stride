using Stride.Shaders.Core;
using Stride.Shaders.Parsing.SDFX.AST;
using Stride.Shaders.Parsing.SDSL;
using Stride.Shaders.Parsing.SDSL.AST;

namespace Stride.Shaders.Parsing.SDFX.Parsers;


public record struct EffectStatementParsers : IParser<Statement>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out Statement parsed, in ParseError? orError = null) where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (EffectBlock(ref scanner, result, out var block))
        {
            parsed = block;
            return true;
        }
        else if (UsingParams(ref scanner, result, out var p1) && SDSL.Parsers.FollowedBy(ref scanner, Tokens.Char(';'), withSpaces: true, advance: true))
        {
            parsed = p1;
            return true;
        }
        else if (Mixin(ref scanner, result, out var p2))
        {
            parsed = p2;
            return true;
        }
        else if (EffectControlsParser.Control(ref scanner, result, out var control))
        {
            parsed = control;
            return true;
        }
        else if (Flow(ref scanner, result, out var flow))
        {
            parsed = flow;
            return true;
        }
        else if (ShaderSourceDeclaration(ref scanner, result, out var ssd))
        {
            parsed = ssd;
            return true;
        }
        else if (StatementParsers.Expression(ref scanner, result, out var exp))
        {
            parsed = exp;
            return true;
        }
        else if (
            SDSL.Parsers.FollowedBy(ref scanner, Tokens.Literal("discard"), withSpaces: true, advance: true)
            && SDSL.Parsers.FollowedBy(ref scanner, Tokens.Char(';'), withSpaces: true, advance: true)
        )
        {
            parsed = new EffectDiscardStatement(scanner[position..scanner.Position]);
            return true;
        }
        return SDSL.Parsers.Exit(ref scanner, result, out parsed, position, orError);
    }

    public static bool Statement<TScanner>(ref TScanner scanner, ParseResult result, out Statement parsed, in ParseError? orError = null) where TScanner : struct, IScanner
        => new EffectStatementParsers().Match(ref scanner, result, out parsed, orError);
    public static bool UsingParams<TScanner>(ref TScanner scanner, ParseResult result, out UsingParams parsed, in ParseError? orError = null) where TScanner : struct, IScanner
        => new UsingParamsParser().Match(ref scanner, result, out parsed, orError);
    public static bool Mixin<TScanner>(ref TScanner scanner, ParseResult result, out Mixin parsed, in ParseError? orError = null) where TScanner : struct, IScanner
        => new MixinParser().Match(ref scanner, result, out parsed, orError);
    public static bool Flow<TScanner>(ref TScanner scanner, ParseResult result, out EffectFlow parsed, in ParseError? orError = null) where TScanner : struct, IScanner
        => new FlowParsers().Match(ref scanner, result, out parsed, orError);

    public static bool EffectBlock<TScanner>(ref TScanner scanner, ParseResult result, out BlockStatement parsed, in ParseError? orError = null) where TScanner : struct, IScanner
    {
        var position = scanner.Position;

        if (Tokens.Char('{', ref scanner, advance: true))
        {
            List<Statement> statements = [];
            while (SDSL.Parsers.FollowedByDel(ref scanner, result, Statement, out Statement statement, withSpaces: true, advance: true))
            {
                statements.Add(statement);
            }
            if (!SDSL.Parsers.FollowedBy(ref scanner, Tokens.Char('}'), withSpaces: true, advance: true))
                return SDSL.Parsers.Exit(ref scanner, result, out parsed, position, new(SDSLErrorMessages.SDSL0001, scanner[scanner.Position], scanner.Memory));
            parsed = new BlockStatement(scanner[position..scanner.Position]) { Statements = statements };
            return true;
        }
        return SDSL.Parsers.Exit(ref scanner, result, out parsed, position);
    }
    public static bool ShaderSourceDeclaration<TScanner>(ref TScanner scanner, ParseResult result, out ShaderSourceDeclaration parsed, in ParseError? orError = null) where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (
            Tokens.AnyOf(["ShaderSourceCollection ", "ShaderSource ", "var "], ref scanner, out _)
            && SDSL.Parsers.TypeNameIdentifierArraySizeValue(ref scanner, result, out var typename, out var name,out var value)
            && SDSL.Parsers.FollowedBy(ref scanner, Tokens.Char(';'), withSpaces: true, advance: true)
        )
        {
            parsed = new(name, scanner[position..scanner.Position], value);
            return true;
        }
        return SDSL.Parsers.Exit(ref scanner, result, out parsed, position);
    }

}


public record struct UsingParamsParser : IParser<UsingParams>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out UsingParams parsed, in ParseError? orError = null) where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (SDSL.Parsers.SequenceOf(ref scanner, ["using", "params"], advance: true))
        {
            if (LiteralsParser.Identifier(ref scanner, result, out var identifier))
            {
                parsed = new(identifier, scanner[position..scanner.Position]);
                return true;
            }

        }
        return SDSL.Parsers.Exit(ref scanner, result, out parsed, position, orError);
    }
}

public record struct MixinParser : IParser<Mixin>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out Mixin parsed, in ParseError? orError = null) where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        var mixinType = MixinStatementType.Default;
        if (Tokens.Literal("mixin", ref scanner, advance: true) && SDSL.Parsers.Spaces0(ref scanner, null!, out _))
        {
            if (Tokens.AnyOf(["compose", "child", "clone", "macro"], ref scanner, out var mixinTypeString, advance: true) && SDSL.Parsers.Spaces1(ref scanner, result, out _))
            {
                mixinType = mixinTypeString switch
                {
                    "compose" => MixinStatementType.ComposeSet,
                    "child" => MixinStatementType.Child,
                    "clone" => MixinStatementType.Clone,
                    "macro" => MixinStatementType.Macro,
                    "remove" => MixinStatementType.Remove,
                    _ => throw new Exception("Invalid mixin type")
                };
            }

            if (AssignOrExpression(ref scanner, result, out var statement)
                && SDSL.Parsers.FollowedBy(ref scanner, Tokens.Char(';'), withSpaces: true, advance: true))
            {
                if (mixinType is MixinStatementType.ComposeSet or MixinStatementType.Child or MixinStatementType.Macro
                    && statement is Assign { Variables: [{ Value: {} value, Variable: Identifier variable }] } assign)
                {
                    if (assign.Variables[0].Operator == AssignOperator.Plus && mixinType == MixinStatementType.ComposeSet)
                        mixinType = MixinStatementType.ComposeAdd;
                    parsed = new Mixin(mixinType, variable, value, scanner[position..scanner.Position]);
                }
                else if (statement is ExpressionStatement expressionStatement)
                {
                    parsed = new Mixin(mixinType, null, expressionStatement.Expression, scanner[position..scanner.Position]);
                }
                else
                {
                    throw new Exception("Invalid mixin statement");
                }
                return true;
            }
        }
        return SDSL.Parsers.Exit(ref scanner, result, out parsed, position, orError);
    }
    
    internal static bool AssignOrExpression<TScanner>(ref TScanner scanner, ParseResult result, out Statement parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if(
            PostfixParser.Postfix(ref scanner, result, out var variable)
            && SDSL.Parsers.FollowedByDel(ref scanner, result, LiteralsParser.AssignOperators, out AssignOperator op, withSpaces: true, advance: true)
            && SDSL.Parsers.FollowedByDel(ref scanner, result, ExpressionParser.Expression, out Expression value, withSpaces: true, advance: true)
        )
        {
            parsed = new Assign(scanner[position..scanner.Position])
            {
                Variables = [new(variable, false, scanner[position..scanner.Position], op, value)]
            };
            return true;
        }
        scanner.Position = position;
        if(
            ExpressionParser.Expression(ref scanner, result, out var expression) 
            && SDSL.Parsers.FollowedBy(ref scanner, Tokens.Char(';'), true) 
        )
        {
            parsed = new ExpressionStatement(expression, scanner[position..scanner.Position]);
            return true;
        }
        return SDSL.Parsers.Exit(ref scanner, result, out parsed, position, orError);
    }
}
