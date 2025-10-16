using Stride.Shaders.Parsing.SDSL.AST;

namespace Stride.Shaders.Parsing.SDSL;


public record struct ShaderMethodParsers : IParser<ShaderMethod>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out ShaderMethod parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        if (Method(ref scanner, result, out var method, in orError))
        {
            parsed = method;
            return true;
        }
        else
        {
            parsed = null!;
            return false;
        }
    }

    public static bool Method<TScanner>(ref TScanner scanner, ParseResult result, out ShaderMethod parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
        => new MethodParser().Match(ref scanner, result, out parsed, in orError);
    public static bool Simple<TScanner>(ref TScanner scanner, ParseResult result, out ShaderMethod parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
        => new SimpleMethodParser().Match(ref scanner, result, out parsed, in orError);



    public static bool MethodParameters<TScanner>(ref TScanner scanner, ParseResult result, out List<MethodParameter> parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;
#warning We should not allow void to be a parameter, this is legacy C code
        if (
            Parsers.FollowedBy(ref scanner, Tokens.Char(')'), withSpaces: true)
            ||
            (
                Parsers.FollowedBy(ref scanner, Tokens.Literal("void"), withSpaces: true, advance: true)
                && Parsers.FollowedBy(ref scanner, Tokens.Char(')'), withSpaces: true)
            )
        )
        {
            parsed = [];
            return true;
        }
        else

        if (Parsers.Repeat(ref scanner, result, MethodParameter, out List<MethodParameter> parameters, 0, withSpaces: true, separator: ","))
        {
            parsed = parameters;
            return true;
        }
        else return Parsers.Exit(ref scanner, result, out parsed, position);

    }
    public static bool MethodParameter<TScanner>(ref TScanner scanner, ParseResult result, out MethodParameter parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;

        if (!(Tokens.AnyOf(["inout", "in", "out", "triangle", "point", "const"], ref scanner, out var storage, advance: true) && Parsers.Spaces1(ref scanner, result, out _)))
            scanner.Position = position;
        if (Parsers.TypeNameIdentifierArraySizeValue(ref scanner, result, out var typename, out var identifier, out var value, advance: true)
        )
        {
            if (
                Parsers.FollowedBy(ref scanner, Tokens.Char(':'), withSpaces: true, advance: true)
                && Parsers.FollowedBy(ref scanner, result, LiteralsParser.Identifier, out Identifier semantic, withSpaces: true, advance: true)
                && Parsers.Spaces0(ref scanner, result, out _)
            )
            {
                parsed = new(typename, identifier, scanner[position..scanner.Position], storage, semantic: semantic);
                return true;
            }
            else
            {
                parsed = new(typename, identifier, scanner[position..scanner.Position], storage);
                return true;
            }
        }
        else return Parsers.Exit(ref scanner, result, out parsed, position);

    }
}

public record struct SimpleMethodParser : IParser<ShaderMethod>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out ShaderMethod parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (
            LiteralsParser.TypeName(ref scanner, result, out var typename)
            && Parsers.Spaces1(ref scanner, result, out _, new(SDSLErrorMessages.SDSL0016, scanner[scanner.Position], scanner.Memory))
            && LiteralsParser.Identifier(ref scanner, result, out var methodName)
            && Parsers.FollowedBy(ref scanner, Tokens.Char('('), withSpaces: true, advance: true)
            && Parsers.FollowedByDel(ref scanner, result, ShaderMethodParsers.MethodParameters, out List<MethodParameter> parameters, withSpaces: true, advance: true)
            && Parsers.FollowedBy(ref scanner, Tokens.Char(')'), withSpaces: true, advance: true)
            && Parsers.Spaces0(ref scanner, result, out _)
            && StatementParsers.Block(ref scanner, result, out var body, new(SDSLErrorMessages.SDSL0040, scanner[scanner.Position], scanner.Memory))
        )
        {
            parsed = new ShaderMethod(typename, methodName, scanner[position..scanner.Position])
            {
                Parameters = parameters,
                Body = (BlockStatement)body
            };
            return true;
        }
        else return Parsers.Exit(ref scanner, result, out parsed, position, orError);
    }
}


public record struct MethodParser : IParser<ShaderMethod>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out ShaderMethod parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        parsed = null!;
        var position = scanner.Position;

        var hasAttributes = ShaderAttributeListParser.AttributeList(ref scanner, result, out var attributes) && Parsers.Spaces0(ref scanner, result, out _);
        var hasModifiers = Parsers.MethodModifiers(ref scanner, result, out var isStaged, out var isStatic, out var isClone, out var isOverride, out var isAbstract, advance: true) && Parsers.Spaces0(ref scanner, result, out _);

        if (isAbstract)
        {
            if (
                LiteralsParser.TypeName(ref scanner, result, out var typename, orError: new(SDSLErrorMessages.SDSL0017, scanner[scanner.Position], scanner.Memory))
                && Parsers.Spaces1(ref scanner, result, out _)
                && LiteralsParser.Identifier(ref scanner, result, out var methodName, orError: new(SDSLErrorMessages.SDSL0017, scanner[scanner.Position], scanner.Memory))
                && Parsers.Spaces0(ref scanner, result, out _)
            )
            {
                if (Tokens.Char('(', ref scanner, advance: true) && Parsers.Spaces0(ref scanner, result, out _))
                {
                    ShaderMethodParsers.MethodParameters(ref scanner, result, out var parameters);
                    Parsers.Spaces0(ref scanner, result, out _);
                    if (!Tokens.Char(')', ref scanner, advance: true))
                        return Parsers.Exit(ref scanner, result, out parsed, position, new(SDSLErrorMessages.SDSL0018, scanner[scanner.Position], scanner.Memory));

                    Parsers.Spaces0(ref scanner, result, out _);
                    if (!Tokens.Char(';', ref scanner, advance: true))
                    {
                        if (orError != null)
                            return Parsers.Exit(ref scanner, result, out parsed, position, new(SDSLErrorMessages.SDSL0033, scanner[scanner.Position], scanner.Memory));
                        else return Parsers.Exit(ref scanner, result, out parsed, position, orError);
                    }
                    else
                    {
                        parsed = new(typename, methodName, scanner[position..scanner.Position], isAbstract: true)
                        {
                            Parameters = parameters
                        };
                        return true;
                    }
                }
                else return Parsers.Exit(ref scanner, result, out parsed, position, orError);
            }
        }
        else if (isClone || isOverride || isStatic || isStaged)
        {
            if (ShaderMethodParsers.Simple(ref scanner, result, out parsed, orError))
            {
                if (hasAttributes)
                    parsed.Attributes = attributes.Attributes;
                parsed.IsStaged = isStaged;
                parsed.IsClone = isClone;
                parsed.IsOverride = isOverride;
                parsed.IsStatic = isStatic;
                parsed.Info = scanner[position..scanner.Position];
                return true;
            }
        }
        else if (ShaderMethodParsers.Simple(ref scanner, result, out parsed, orError))
        {
            parsed.Info = scanner[position..scanner.Position];
            return true;
        }
        return Parsers.Exit(ref scanner, result, out parsed, position, orError);
    }

}

