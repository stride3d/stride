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
        if(CommonParsers.Repeat(ref scanner, result, MethodParameter, out List<MethodParameter> parameters, 0, withSpaces: true, separator: ","))
        {
            parsed = parameters;
            return true;
        }
        else return CommonParsers.Exit(ref scanner, result, out parsed, position);

    }
    public static bool MethodParameter<TScanner>(ref TScanner scanner, ParseResult result, out MethodParameter parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;

        if(Terminals.AnyOf(["inout", "in", "out", "triangle"], ref scanner, out var storage, advance: true))
            CommonParsers.Spaces1(ref scanner, result, out _);
        if(CommonParsers.TypeNameIdentifierArraySizeValue(ref scanner, result, out var typename, out var identifier, out var arraySize, out var value, advance: true)
        )
        {
            parsed = new(typename, identifier, scanner.GetLocation(position..scanner.Position), storage);
            return true;
        }
        else return CommonParsers.Exit(ref scanner, result, out parsed, position);

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
            && CommonParsers.Spaces1(ref scanner, result, out _, new(SDSLParsingMessages.SDSL0016, scanner.GetErrorLocation(scanner.Position), scanner.Memory))
            && LiteralsParser.Identifier(ref scanner, result, out var methodName)
            && CommonParsers.FollowedBy(ref scanner, Terminals.Char('('), withSpaces: true, advance: true)
            && CommonParsers.FollowedByDel(ref scanner, result, ShaderMethodParsers.MethodParameters, out List<MethodParameter> parameters, withSpaces: true, advance: true)
            && CommonParsers.FollowedBy(ref scanner, Terminals.Char(')'), withSpaces: true, advance: true)
            && CommonParsers.Spaces0(ref scanner, result, out _)
            && StatementParsers.Block(ref scanner, result, out var body, new(SDSLParsingMessages.SDSL0040, scanner.GetErrorLocation(scanner.Position), scanner.Memory))
        )
        {
            parsed = new ShaderMethod(typename, methodName, scanner.GetLocation(position, scanner.Position - position))
            {
                Parameters = parameters,
                Body = (BlockStatement)body
            };
            return true;
        }
        else return CommonParsers.Exit(ref scanner, result, out parsed, position, orError);
    }
}


public record struct MethodParser : IParser<ShaderMethod>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out ShaderMethod parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        parsed = null!;
        var position = scanner.Position;
        if (Terminals.Literal("abstract", ref scanner, advance: true) && CommonParsers.Spaces1(ref scanner, result, out _))
        {
            if (
                LiteralsParser.TypeName(ref scanner, result, out var typename, orError: new(SDSLParsingMessages.SDSL0017, scanner.GetErrorLocation(scanner.Position), scanner.Memory))
                && CommonParsers.Spaces1(ref scanner, result, out _)
                && LiteralsParser.Identifier(ref scanner, result, out var methodName, orError: new(SDSLParsingMessages.SDSL0017, scanner.GetErrorLocation(scanner.Position), scanner.Memory))
                && CommonParsers.Spaces0(ref scanner, result, out _)
            )
            {
                if (Terminals.Char('(', ref scanner, advance: true) && CommonParsers.Spaces0(ref scanner, result, out _))
                {
                    ShaderMethodParsers.MethodParameters(ref scanner, result, out var parameters);
                    CommonParsers.Spaces0(ref scanner, result, out _);
                    if (!Terminals.Char(')', ref scanner, advance: true))
                        return CommonParsers.Exit(ref scanner, result, out parsed, position, new(SDSLParsingMessages.SDSL0018, scanner.GetErrorLocation(scanner.Position), scanner.Memory));

                    CommonParsers.Spaces0(ref scanner, result, out _);
                    if (!Terminals.Char(';', ref scanner, advance: true))
                    {
                        if (orError != null)
                            return CommonParsers.Exit(ref scanner, result, out parsed, position, new(SDSLParsingMessages.SDSL0033, scanner.GetErrorLocation(scanner.Position), scanner.Memory));
                        else return CommonParsers.Exit(ref scanner, result, out parsed, position, orError);
                    }
                    else
                    {
                        parsed = new(typename, methodName, scanner.GetLocation(position..scanner.Position), isAbstract: true)
                        {
                            Parameters = parameters
                        };
                        return true;
                    }
                }
                else return CommonParsers.Exit(ref scanner, result, out parsed, position, orError);
            }
        }
        else
            scanner.Position = position;
        if (Terminals.AnyOf(["clone", "override"], ref scanner, out var matched, advance: true) && CommonParsers.Spaces1(ref scanner, result, out _))
        {
            var isClone = false;
            var isOverride = false;
            var tmpPos = scanner.Position;
            if (matched == "clone")
                isClone = true;
            else if (matched == "override")
                isOverride = true;

            CommonParsers.Spaces0(ref scanner, result, out _);
            if (ShaderMethodParsers.Simple(ref scanner, result, out parsed, orError))
            {
                parsed.IsClone = isClone;
                parsed.IsOverride = isOverride;
                parsed.Info = scanner.GetLocation(position..scanner.Position);
                return true;
            }
        }
        else
            scanner.Position = position;
        if (ShaderMethodParsers.Simple(ref scanner, result, out parsed, orError))
        {
            parsed.Info = scanner.GetLocation(position..scanner.Position);
            return true;
        }
        return CommonParsers.Exit(ref scanner, result, out parsed, position, orError);
    }

}

