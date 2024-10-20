using Stride.Shaders.Parsing.SDSL.AST;

namespace Stride.Shaders.Parsing.SDSL;


public record struct ParameterParsers : IParser<ParameterListNode>
{
    public bool Match<TScanner>(ref TScanner scanner, ParseResult result, out ParameterListNode parsed, in ParseError? orError = null) where TScanner : struct, IScanner
    {
        throw new NotImplementedException();
    }
    public static bool Declarations<TScanner>(ref TScanner scanner, ParseResult result, out ShaderParameterDeclarations parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
        => new ParameterDeclarationsParser().Match(ref scanner, result, out parsed, in orError);
    public static bool Values<TScanner>(ref TScanner scanner, ParseResult result, out ShaderExpressionList parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
        => new ParameterListParser().Match(ref scanner, result, out parsed, in orError);

    public static bool GenericsList<TScanner>(ref TScanner scanner, ParseResult result, out ShaderExpressionList parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
        => new GenericsListParser().Match(ref scanner, result, out parsed, in orError);
    public static bool GenericsValue<TScanner>(ref TScanner scanner, ParseResult result, out Expression parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
        => new GenericsValueParser().Match(ref scanner, result, out parsed, in orError);
}


public record struct ParameterDeclarationsParser : IParser<ShaderParameterDeclarations>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out ShaderParameterDeclarations parsed, in ParseError? orError = null) where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        List<ShaderParameter> parameters = [];
        while (
            LiteralsParser.TypeName(ref scanner, result, out var typename)
            && CommonParsers.Spaces1(ref scanner, result, out _)
            && LiteralsParser.Identifier(ref scanner, result, out var name)
            && CommonParsers.Spaces0(ref scanner, result, out _)
        )
        {
            parameters.Add(new(typename, name));
            if (!Terminals.Char(',', ref scanner, advance: true) && CommonParsers.Spaces0(ref scanner, result, out _))
                break;
        }
        parsed = new(scanner.GetLocation(position..scanner.Position)) { Parameters = parameters };
        return true;
    }
}
public record struct ParameterListParser : IParser<ShaderExpressionList>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out ShaderExpressionList parsed, in ParseError? orError = null) where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        List<Expression> values = [];
        while (ExpressionParser.Expression(ref scanner, result, out var expr) && CommonParsers.Spaces0(ref scanner, result, out _))
        {
            values.Add(expr);
            if (!Terminals.Char(',', ref scanner, advance: true) && CommonParsers.Spaces0(ref scanner, result, out _))
                break;
        }
        parsed = new(scanner.GetLocation(position..scanner.Position))
        {
            Values = values
        };
        return true;
    }
}

public record struct GenericsListParser : IParser<ShaderExpressionList>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out ShaderExpressionList parsed, in ParseError? orError = null) where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (ParameterParsers.GenericsValue(ref scanner, result, out var parameter, new("Expecting at least one generics value", scanner.GetErrorLocation(scanner.Position), scanner.Memory)))
        {
            parsed = new(scanner.GetLocation(position..scanner.Position));
            parsed.Values.Add(parameter);
            CommonParsers.Spaces0(ref scanner, result, out _);
            while (Terminals.Char(',', ref scanner, advance: true))
            {
                CommonParsers.Spaces0(ref scanner, result, out _);
                if (ParameterParsers.GenericsValue(ref scanner, result, out var other, new("Expecting at least one generics value", scanner.GetErrorLocation(scanner.Position), scanner.Memory)))
                {
                    parsed.Values.Add(other);
                    CommonParsers.Spaces0(ref scanner, result, out _);
                }
            }
            return true;
        }
        return CommonParsers.Exit(ref scanner, result, out parsed, position, orError);
    }
}

public record struct GenericsValueParser : IParser<Expression>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out Expression parsed, in ParseError? orError = null) where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (LiteralsParser.Number(ref scanner, result, out var number))
        {
            parsed = number;
            return true;
        }
        else if (LiteralsParser.Vector(ref scanner, result, out var vector))
        {
            parsed = vector;
            return true;
        }
        else if (PostfixParser.Accessor(ref scanner, result, out var accessor))
        {
            if (accessor is AccessorExpression ae && ae.Accessed is Identifier)
            {
                parsed = accessor;
                return true;
            }
            scanner.Position = position;
        }
        if (LiteralsParser.Identifier(ref scanner, result, out var identifier))
        {
            // previous parser might have matched somehow and advanced the scanner

            parsed = identifier;
            return true;
        }
        return CommonParsers.Exit(ref scanner, result, out parsed, position, orError);
    }

}