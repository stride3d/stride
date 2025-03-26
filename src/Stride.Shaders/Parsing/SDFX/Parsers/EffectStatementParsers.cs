using Stride.Shaders.Core;
using Stride.Shaders.Parsing.SDFX.AST;
using Stride.Shaders.Parsing.SDSL;
using Stride.Shaders.Parsing.SDSL.AST;

namespace Stride.Shaders.Parsing.SDFX.Parsers;


public record struct EffectStatementParsers : IParser<EffectStatement>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out EffectStatement parsed, in ParseError? orError = null) where TScanner : struct, IScanner
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
        else if (MixinCompose(ref scanner, result, out var p2))
        {
            parsed = p2;
            return true;
        }
        else if (MixinComposeAdd(ref scanner, result, out var mca) && SDSL.Parsers.FollowedBy(ref scanner, Tokens.Char(';'), withSpaces: true, advance: true))
        {
            parsed = mca;
            return true;
        }
        else if (MixinChild(ref scanner, result, out var mc) && SDSL.Parsers.FollowedBy(ref scanner, Tokens.Char(';'), withSpaces: true, advance: true))
        {
            parsed = mc;
            return true;
        }
        else if (MixinClone(ref scanner, result, out var mcl) && SDSL.Parsers.FollowedBy(ref scanner, Tokens.Char(';'), withSpaces: true, advance: true))
        {
            parsed = mcl;
            return true;
        }
        else if (MixinConst(ref scanner, result, out var mconst) && SDSL.Parsers.FollowedBy(ref scanner, Tokens.Char(';'), withSpaces: true, advance: true))
        {
            parsed = mconst;
            return true;
        }
        else if (MixinUse(ref scanner, result, out var p3) && SDSL.Parsers.FollowedBy(ref scanner, Tokens.Char(';'), withSpaces: true, advance: true))
        {
            parsed = p3;
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
            parsed = new EffectExpressionStatement(exp, scanner[position..scanner.Position]);
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

    public static bool Statement<TScanner>(ref TScanner scanner, ParseResult result, out EffectStatement parsed, in ParseError? orError = null) where TScanner : struct, IScanner
        => new EffectStatementParsers().Match(ref scanner, result, out parsed, orError);
    public static bool UsingParams<TScanner>(ref TScanner scanner, ParseResult result, out UsingParams parsed, in ParseError? orError = null) where TScanner : struct, IScanner
        => new UsingParamsParser().Match(ref scanner, result, out parsed, orError);
    public static bool MixinCompose<TScanner>(ref TScanner scanner, ParseResult result, out MixinCompose parsed, in ParseError? orError = null) where TScanner : struct, IScanner
        => new MixinComposeParser().Match(ref scanner, result, out parsed, orError);
    public static bool MixinComposeAdd<TScanner>(ref TScanner scanner, ParseResult result, out MixinComposeAdd parsed, in ParseError? orError = null) where TScanner : struct, IScanner
        => new MixinComposeAddParser().Match(ref scanner, result, out parsed, orError);
    public static bool MixinUse<TScanner>(ref TScanner scanner, ParseResult result, out MixinUse parsed, in ParseError? orError = null) where TScanner : struct, IScanner
        => new MixinUseParser().Match(ref scanner, result, out parsed, orError);
    public static bool MixinChild<TScanner>(ref TScanner scanner, ParseResult result, out MixinChild parsed, in ParseError? orError = null) where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (
            SDSL.Parsers.SequenceOf(ref scanner, ["mixin", "child"], advance: true)
            && SDSL.Parsers.FollowedByDel(ref scanner, result, ShaderClassParsers.Mixin, out Mixin mixin, withSpaces: true, advance: true)
            && SDSL.Parsers.FollowedBy(ref scanner, Tokens.Char(';'), withSpaces: true)
        )
        {
            parsed = new(mixin, scanner[position..scanner.Position]);
            return true;
        }
        return SDSL.Parsers.Exit(ref scanner, result, out parsed, position, orError);
    }
    public static bool MixinClone<TScanner>(ref TScanner scanner, ParseResult result, out MixinClone parsed, in ParseError? orError = null) where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (
            SDSL.Parsers.SequenceOf(ref scanner, ["mixin", "clone"], advance: true)
            && SDSL.Parsers.FollowedByDel(ref scanner, result, ShaderClassParsers.Mixin, out Mixin mixin, withSpaces: true, advance: true)
            && SDSL.Parsers.FollowedBy(ref scanner, Tokens.Char(';'), withSpaces: true)
        )
        {
            parsed = new(mixin, scanner[position..scanner.Position]);
            return true;
        }
        return SDSL.Parsers.Exit(ref scanner, result, out parsed, position, orError);
    }
    public static bool MixinConst<TScanner>(ref TScanner scanner, ParseResult result, out MixinConst parsed, in ParseError? orError = null) where TScanner : struct, IScanner
        => new MixinConstParser().Match(ref scanner, result, out parsed, orError);
    public static bool Flow<TScanner>(ref TScanner scanner, ParseResult result, out EffectFlow parsed, in ParseError? orError = null) where TScanner : struct, IScanner
        => new FlowParsers().Match(ref scanner, result, out parsed, orError);

    public static bool EffectBlock<TScanner>(ref TScanner scanner, ParseResult result, out EffectStatement parsed, in ParseError? orError = null) where TScanner : struct, IScanner
    {
        var position = scanner.Position;

        if (Tokens.Char('{', ref scanner, advance: true))
        {
            List<EffectStatement> statements = [];
            while (SDSL.Parsers.FollowedByDel(ref scanner, result, Statement, out EffectStatement statement, withSpaces: true, advance: true))
            {
                statements.Add(statement);
            }
            if (!SDSL.Parsers.FollowedBy(ref scanner, Tokens.Char('}'), withSpaces: true, advance: true))
                return SDSL.Parsers.Exit(ref scanner, result, out parsed, position, new(SDSLErrorMessages.SDSL0001, scanner[scanner.Position], scanner.Memory));
            parsed = new EffectBlock(scanner[position..scanner.Position]) { Statements = statements };
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

public record struct MixinConstParser : IParser<MixinConst>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out MixinConst parsed, in ParseError? orError = null) where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (
            SDSL.Parsers.SequenceOf(ref scanner, ["mixin", "macro"], advance: true)
            || SDSL.Parsers.SequenceOf(ref scanner, ["mixin", "const"], advance: true)
        )
        {
            SDSL.Parsers.Spaces0(ref scanner, result, out _);
            var tmp = scanner.Position;
            SDSL.Parsers.Until(ref scanner, ';');
            if (Tokens.Char(';', ref scanner))
            {
                parsed = new(scanner.Memory[tmp..scanner.Position].ToString().Trim(), scanner[position..scanner.Position]);
                return true;
            }
            else return SDSL.Parsers.Exit(ref scanner, result, out parsed, position, new(SDSLErrorMessages.SDSL0001, scanner[position], scanner.Memory));
        }
        return SDSL.Parsers.Exit(ref scanner, result, out parsed, position, orError);
    }
}

public record struct MixinComposeParser : IParser<MixinCompose>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out MixinCompose parsed, in ParseError? orError = null) where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (
            SDSL.Parsers.SequenceOf(ref scanner, ["mixin", "compose"], advance: true)
            && LiteralsParser.Identifier(ref scanner, result, out var name)
            && SDSL.Parsers.Spaces0(ref scanner, result, out _)
            && Tokens.AnyOf(["=", "+="], ref scanner, out var op, advance: true)
        )
        {
            if(
                SDSL.Parsers.FollowedBy(ref scanner, Tokens.Char('('), withSpaces: true, advance: true)
                && SDSL.Parsers.Spaces0(ref scanner, result, out _)
                && ComposeValue(ref scanner, result, out var composeValue)
                && SDSL.Parsers.FollowedBy(ref scanner, Tokens.Char(')'), withSpaces: true, advance: true)
                && SDSL.Parsers.FollowedBy(ref scanner, Tokens.Char(';'), withSpaces: true, advance: true)
            )
            {
                parsed = new MixinCompose(name, op.ToAssignOperator(), composeValue, scanner[position..scanner.Position]);
                return true;
            }
            else if(
                SDSL.Parsers.Spaces0(ref scanner, result, out _)
                && ComposeValue(ref scanner, result, out var composeValue2)
                && SDSL.Parsers.FollowedBy(ref scanner, Tokens.Char(';'), withSpaces: true, advance: true)
            )
            {
                parsed = new MixinCompose(name, op.ToAssignOperator(), composeValue2, scanner[position..scanner.Position]);
                return true;
            }
            
        }
        return SDSL.Parsers.Exit(ref scanner, result, out parsed, position, orError);
    }

    public static bool ComposeValue<TScanner>(ref TScanner scanner, ParseResult result, out ComposeValue value, in ParseError? orError = null) where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if(
            ShaderClassParsers.Mixin(ref scanner, result, out var mixin) 
            && (
                SDSL.Parsers.FollowedBy(ref scanner, Tokens.Char(';'), withSpaces: true)
                || SDSL.Parsers.FollowedBy(ref scanner, Tokens.Char(')'), withSpaces: true)
            )
        )
        {
            value = new ComposeMixinValue(mixin, scanner[position..scanner.Position]);
            return true;
        }
        else 
        {
            scanner.Position = position;
            if(Tokens.IdentifierFirstChar(ref scanner, advance: true))
            {
                while(
                    Tokens.LetterOrDigit(ref scanner, advance: true)
                    || Tokens.Char('_', ref scanner, advance: true)
                    || Tokens.Char('.', ref scanner, advance: true)
                );
                if(
                    SDSL.Parsers.FollowedBy(ref scanner, Tokens.Char(')'), withSpaces: true)
                    || SDSL.Parsers.FollowedBy(ref scanner, Tokens.Char(';'), withSpaces: true)
                )
                {
                    value = new ComposePathValue(scanner.Memory[position..scanner.Position].ToString(), scanner[position..scanner.Position]);
                    return true;
                }
            }
        }
        return SDSL.Parsers.Exit(ref scanner, result, out value, position);
    }
}

public record struct MixinComposeAddParser : IParser<MixinComposeAdd>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out MixinComposeAdd parsed, in ParseError? orError = null) where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (
            SDSL.Parsers.SequenceOf(ref scanner, ["mixin", "compose"], advance: true)
            && LiteralsParser.Identifier(ref scanner, result, out var name)
            && SDSL.Parsers.Spaces0(ref scanner, result, out _)
            && Tokens.Literal("+=", ref scanner, advance: true)
            && SDSL.Parsers.Spaces0(ref scanner, result, out _)

        )
        {
            var start = scanner.Position;
            SDSL.Parsers.Until(ref scanner, ';');
            parsed = new MixinComposeAdd(name, new(scanner.Memory[start..scanner.Position].ToString().Trim(), scanner[start..scanner.Position]), scanner[position..scanner.Position]);
            return true;
        }
        return SDSL.Parsers.Exit(ref scanner, result, out parsed, position, orError);
    }
}

public record struct MixinUseParser : IParser<MixinUse>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out MixinUse parsed, in ParseError? orError = null) where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (
            Tokens.Literal("mixin", ref scanner, advance: true)
            && SDSL.Parsers.Spaces1(ref scanner, result, out _)
        )
        {
            var betweenParenthesis = SDSL.Parsers.FollowedBy(ref scanner, Tokens.Char('('), withSpaces: true, advance: true);
            if (SDSL.Parsers.Repeat(ref scanner, result, ShaderClassParsers.Mixin, out List<Mixin> mixins, 1, withSpaces: true, separator: ","))
            {
                var checkParen = betweenParenthesis == SDSL.Parsers.FollowedBy(ref scanner, Tokens.Char(')'), withSpaces: true, advance: true);
                var finished = SDSL.Parsers.FollowedBy(ref scanner, Tokens.Char(';'), withSpaces: true);
                if (finished && checkParen)
                {
                    parsed = new(mixins, scanner[position..scanner.Position]);
                    return finished;
                }
                else return SDSL.Parsers.Exit(ref scanner, result, out parsed, position);
            }
            return SDSL.Parsers.Exit(ref scanner, result, out parsed, position);
        }
        return SDSL.Parsers.Exit(ref scanner, result, out parsed, position, orError);
    }
}