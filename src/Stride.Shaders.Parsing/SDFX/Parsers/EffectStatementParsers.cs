using Stride.Shaders.Parsing.SDFX.AST;
using Stride.Shaders.Parsing.SDSL;
using Stride.Shaders.Parsing.SDSL.AST;

namespace Stride.Shaders.Parsing.SDFX.Parsers;


public record struct EffectStatementParsers : IParser<EffectStatement>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out EffectStatement parsed, in ParseError? orError = null) where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (EffectBlock(ref scanner, result, out var block, orError))
        {
            parsed = block;
            return true;
        }
        else if (UsingParams(ref scanner, result, out var p1, orError) && CommonParsers.FollowedBy(ref scanner, Terminals.Char(';'), withSpaces: true, advance: true))
        {
            parsed = p1;
            return true;
        }
        else if (MixinCompose(ref scanner, result, out var p2, orError))
        {
            parsed = p2;
            return true;
        }
        else if (MixinComposeAdd(ref scanner, result, out var mca, orError) && CommonParsers.FollowedBy(ref scanner, Terminals.Char(';'), withSpaces: true, advance: true))
        {
            parsed = mca;
            return true;
        }
        else if (MixinChild(ref scanner, result, out var mc, orError) && CommonParsers.FollowedBy(ref scanner, Terminals.Char(';'), withSpaces: true, advance: true))
        {
            parsed = mc;
            return true;
        }
        else if (MixinClone(ref scanner, result, out var mcl, orError) && CommonParsers.FollowedBy(ref scanner, Terminals.Char(';'), withSpaces: true, advance: true))
        {
            parsed = mcl;
            return true;
        }
        else if (MixinConst(ref scanner, result, out var mconst, orError) && CommonParsers.FollowedBy(ref scanner, Terminals.Char(';'), withSpaces: true, advance: true))
        {
            parsed = mconst;
            return true;
        }
        else if (MixinUse(ref scanner, result, out var p3, orError) && CommonParsers.FollowedBy(ref scanner, Terminals.Char(';'), withSpaces: true, advance: true))
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
        else if (ShaderSourceDeclaration(ref scanner, result, out var ssd, orError))
        {
            parsed = ssd;
            return true;
        }
        return CommonParsers.Exit(ref scanner, result, out parsed, position, orError);
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
            CommonParsers.SequenceOf(ref scanner, ["mixin", "child"], advance: true)
            && CommonParsers.FollowedByDel(ref scanner, result, ShaderClassParsers.Mixin, out Mixin mixin, withSpaces: true, advance: true)
            && CommonParsers.FollowedBy(ref scanner, Terminals.Char(';'), withSpaces: true)
        )
        {
            parsed = new(mixin, scanner.GetLocation(position..scanner.Position));
            return true;
        }
        return CommonParsers.Exit(ref scanner, result, out parsed, position, orError);
    }
    public static bool MixinClone<TScanner>(ref TScanner scanner, ParseResult result, out MixinClone parsed, in ParseError? orError = null) where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (
            CommonParsers.SequenceOf(ref scanner, ["mixin", "clone"], advance: true)
            && CommonParsers.FollowedByDel(ref scanner, result, ShaderClassParsers.Mixin, out Mixin mixin, withSpaces: true, advance: true)
            && CommonParsers.FollowedBy(ref scanner, Terminals.Char(';'), withSpaces: true)
        )
        {
            parsed = new(mixin, scanner.GetLocation(position..scanner.Position));
            return true;
        }
        return CommonParsers.Exit(ref scanner, result, out parsed, position, orError);
    }
    public static bool MixinConst<TScanner>(ref TScanner scanner, ParseResult result, out MixinConst parsed, in ParseError? orError = null) where TScanner : struct, IScanner
        => new MixinConstParser().Match(ref scanner, result, out parsed, orError);
    public static bool Flow<TScanner>(ref TScanner scanner, ParseResult result, out EffectFlow parsed, in ParseError? orError = null) where TScanner : struct, IScanner
        => new FlowParsers().Match(ref scanner, result, out parsed, orError);

    public static bool EffectBlock<TScanner>(ref TScanner scanner, ParseResult result, out EffectStatement parsed, in ParseError? orError = null) where TScanner : struct, IScanner
    {
        var position = scanner.Position;

        if (Terminals.Char('{', ref scanner, advance: true))
        {
            List<EffectStatement> statements = [];
            while (CommonParsers.FollowedByDel(ref scanner, result, Statement, out EffectStatement statement, withSpaces: true, advance: true))
            {
                statements.Add(statement);
            }
            if (!CommonParsers.FollowedBy(ref scanner, Terminals.Char('}'), withSpaces: true, advance: true))
                return CommonParsers.Exit(ref scanner, result, out parsed, position, new(SDSLParsingMessages.SDSL0001, scanner.GetErrorLocation(scanner.Position), scanner.Memory));
            parsed = new EffectBlock(scanner.GetLocation(position..scanner.Position)) { Statements = statements };
            return true;
        }
        return CommonParsers.Exit(ref scanner, result, out parsed, position);
    }
    public static bool ShaderSourceDeclaration<TScanner>(ref TScanner scanner, ParseResult result, out ShaderSourceDeclaration parsed, in ParseError? orError = null) where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (
            Terminals.AnyOf(["ShaderSourceCollection ", "ShaderSource "], ref scanner, out _)
            && CommonParsers.TypeNameIdentifierArraySizeValue(ref scanner, result, out var typename, out var name, out var arraySize, out var value)
            && CommonParsers.FollowedBy(ref scanner, Terminals.Char(';'), withSpaces: true, advance: true)
        )
        {
            typename.ArraySize = arraySize;
            parsed = new(name, scanner.GetLocation(position..scanner.Position), value);
            return true;
        }
        return CommonParsers.Exit(ref scanner, result, out parsed, position);
    }

}


public record struct UsingParamsParser : IParser<UsingParams>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out UsingParams parsed, in ParseError? orError = null) where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (CommonParsers.SequenceOf(ref scanner, ["using", "params"], advance: true))
        {
            if (LiteralsParser.Identifier(ref scanner, result, out var identifier))
            {
                parsed = new(identifier, scanner.GetLocation(position..scanner.Position));
                return true;
            }

        }
        return CommonParsers.Exit(ref scanner, result, out parsed, position, orError);
    }
}

public record struct MixinConstParser : IParser<MixinConst>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out MixinConst parsed, in ParseError? orError = null) where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (
            CommonParsers.SequenceOf(ref scanner, ["mixin", "macro"], advance: true)
            || CommonParsers.SequenceOf(ref scanner, ["mixin", "const"], advance: true)
        )
        {
            CommonParsers.Spaces0(ref scanner, result, out _);
            var tmp = scanner.Position;
            CommonParsers.Until(ref scanner, ';');
            if (Terminals.Char(';', ref scanner))
            {
                parsed = new(scanner.Memory[tmp..scanner.Position].ToString().Trim(), scanner.GetLocation(position..scanner.Position));
                return true;
            }
            else return CommonParsers.Exit(ref scanner, result, out parsed, position, new(SDSLParsingMessages.SDSL0001, scanner.GetErrorLocation(position), scanner.Memory));
        }
        return CommonParsers.Exit(ref scanner, result, out parsed, position, orError);
    }
}

public record struct MixinComposeParser : IParser<MixinCompose>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out MixinCompose parsed, in ParseError? orError = null) where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (
            CommonParsers.SequenceOf(ref scanner, ["mixin", "compose"], advance: true)
            && LiteralsParser.Identifier(ref scanner, result, out var name)
            && CommonParsers.FollowedBy(ref scanner, Terminals.Char('='), withSpaces: true, advance: true)
        )
        {
            var paren = CommonParsers.FollowedBy(ref scanner, Terminals.Char('('), withSpaces: true, advance: true);
            if (
                ExpressionParser.Expression(ref scanner, result, out var mixin)
                && paren == CommonParsers.FollowedBy(ref scanner, Terminals.Char(')'), withSpaces: true, advance: true)
                && CommonParsers.FollowedBy(ref scanner, Terminals.Char(';'), withSpaces: true, advance: true)
            )
            {
                parsed = new(name, mixin, scanner.GetLocation(position..scanner.Position));
                return true;
            }
            else if(
                CommonParsers.FollowedBy(ref scanner, result, PostfixParser.Postfix, out Expression postfix, withSpaces: true, advance: true)
                && paren == CommonParsers.FollowedBy(ref scanner, Terminals.Char(')'), withSpaces: true, advance: true)
                && CommonParsers.FollowedBy(ref scanner, Terminals.Char(';'), withSpaces: true, advance: true)
            )
            {
                parsed = new(name, postfix, scanner.GetLocation(position..scanner.Position));
                return true;
            }
        }
        return CommonParsers.Exit(ref scanner, result, out parsed, position, orError);
    }
}

public record struct MixinComposeAddParser : IParser<MixinComposeAdd>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out MixinComposeAdd parsed, in ParseError? orError = null) where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (
            CommonParsers.SequenceOf(ref scanner, ["mixin", "compose"], advance: true)
            && LiteralsParser.Identifier(ref scanner, result, out var name)
            && CommonParsers.Spaces0(ref scanner, result, out _)
            && Terminals.Literal("+=", ref scanner, advance: true)
            && CommonParsers.Spaces0(ref scanner, result, out _)

        )
        {
            var start = scanner.Position;
            CommonParsers.Until(ref scanner, ';');
            parsed = new MixinComposeAdd(name, new(scanner.Memory[start..scanner.Position].ToString().Trim(), scanner.GetLocation(start..scanner.Position)), scanner.GetLocation(position..scanner.Position));
            return true;
        }
        return CommonParsers.Exit(ref scanner, result, out parsed, position, orError);
    }
}

public record struct MixinUseParser : IParser<MixinUse>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out MixinUse parsed, in ParseError? orError = null) where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (
            Terminals.Literal("mixin", ref scanner, advance: true)
            && CommonParsers.Spaces1(ref scanner, result, out _)
        )
        {
            var betweenParenthesis = CommonParsers.FollowedBy(ref scanner, Terminals.Char('('), withSpaces: true, advance: true);
            if (ShaderClassParsers.Mixin(ref scanner, result, out var mixin))
            {
                var checkParen = betweenParenthesis == CommonParsers.FollowedBy(ref scanner, Terminals.Char(')'), withSpaces: true, advance: true);
                var finished = CommonParsers.FollowedBy(ref scanner, Terminals.Char(';'), withSpaces: true);
                if (finished && checkParen)
                {
                    parsed = new(mixin, scanner.GetLocation(position..scanner.Position));
                    return finished;
                }
                else return CommonParsers.Exit(ref scanner, result, out parsed, position);
            }
            else return CommonParsers.Exit(ref scanner, result, out parsed, position);
        }
        return CommonParsers.Exit(ref scanner, result, out parsed, position, orError);
    }
}