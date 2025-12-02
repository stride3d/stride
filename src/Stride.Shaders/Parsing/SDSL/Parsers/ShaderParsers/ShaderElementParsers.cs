using Stride.Shaders.Parsing.SDSL.AST;

namespace Stride.Shaders.Parsing.SDSL;


public record struct ShaderElementParsers : IParser<ShaderElement>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out ShaderElement parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (TypeDef(ref scanner, result, out var typeDef))
        {
            parsed = typeDef;
            return true;
        }
        else if (BufferParsers.Buffer(ref scanner, result, out var buffer))
        {
            Parsers.FollowedBy(ref scanner, Tokens.Char(';'), withSpaces: true, advance: true);
            parsed = buffer;
            return true;
        }
        else if (Struct(ref scanner, result, out var structElement))
        {
            Parsers.FollowedBy(ref scanner, Tokens.Char(';'), withSpaces: true, advance: true);
            parsed = structElement;
            return true;
        }
        else
        {


            if(AnySamplers(ref scanner, result, out var sampler))
            {
                parsed = sampler;
                return true;
            }
            else if (Method(ref scanner, result, out var method))
            {
                parsed = method;
                return true;
            }
            else if (ShaderMemberParser.Member(ref scanner, result, out var member))
            {
                parsed = member;
                return true;
            }


            else return Parsers.Exit(ref scanner, result, out parsed, position);
        }

    }
    public static bool Compose<TScanner>(ref TScanner scanner, ParseResult result, out ShaderCompose parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
        => new CompositionParser().Match(ref scanner, result, out parsed, in orError);
    public static bool Struct<TScanner>(ref TScanner scanner, ParseResult result, out ShaderStruct parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
        => new ShaderStructParser().Match(ref scanner, result, out parsed, in orError);


    public static bool AnySamplers<TScanner>(ref TScanner scanner, ParseResult result, out ShaderElement parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        var isStaged = Tokens.Literal("stage", ref scanner, advance: true) && Parsers.Spaces0(ref scanner, result, out _);

        if (SamplerState(ref scanner, result, out var samplerState))
        {
            samplerState.IsStaged = isStaged;
            parsed = samplerState;
            return true;
        }
        else if (SamplerComparisonState(ref scanner, result, out var samplerCompState))
        {
            samplerCompState.IsStaged = isStaged;
            parsed = samplerCompState;
            return true;
        }
        else return Parsers.Exit(ref scanner,  result, out parsed, position);
    }
    public static bool SamplerState<TScanner>(ref TScanner scanner, ParseResult result, out ShaderSamplerState parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
        => new ShaderSamplerStateParser().Match(ref scanner, result, out parsed, in orError);
    public static bool SamplerComparisonState<TScanner>(ref TScanner scanner, ParseResult result, out ShaderSamplerComparisonState parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
        => new ShaderSamplerComparisonStateParser().Match(ref scanner, result, out parsed, in orError);
    public static bool ShaderElement<TScanner>(ref TScanner scanner, ParseResult result, out ShaderElement parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
        => new ShaderElementParsers().Match(ref scanner, result, out parsed, in orError);

    public static bool Method<TScanner>(ref TScanner scanner, ParseResult result, out ShaderMethod parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
        => new ShaderMethodParsers().Match(ref scanner, result, out parsed, in orError);

    public static bool ShaderVariable<TScanner>(ref TScanner scanner, ParseResult result, out ShaderElement parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;

        var hasStorageClass =
            Tokens.AnyOf(
                ["extern", "nointerpolation", "precise", "shared", "groupshared", "static", "uniform", "volatile"],
                ref scanner,
                out var storageClass,
                advance: true)
            && Parsers.Spaces1(ref scanner, result, out _)
            ;
        var hasTypeModifier =
            Tokens.AnyOf(
                ["const", "row_major", "column_major"],
                ref scanner,
                out var typemodifier,
                advance: true)
            && Parsers.Spaces1(ref scanner, result, out _)
            ;

        if (
            Parsers.TypeNameIdentifierArraySizeValue(ref scanner, result, out var typeName, out var identifier, out var value)
            && Parsers.FollowedBy(ref scanner, Tokens.Char(';'), withSpaces: true, advance: true)
        )
        {
            parsed = new ShaderVariable(typeName, identifier, value, scanner[position..scanner.Position])
            {
                StorageClass = storageClass.ToStorageClass(),
                TypeModifier = typemodifier.ToTypeModifier()
            };
            return true;
        }
        else return Parsers.Exit(ref scanner, result, out parsed, position, orError);

    }

    public static bool TypeDef<TScanner>(ref TScanner scanner, ParseResult result, out ShaderElement parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;

        if (
            Tokens.Literal("typedef", ref scanner, advance: true)
            && Parsers.Spaces1(ref scanner, result, out _)
            && LiteralsParser.TypeName(ref scanner, result, out var type)
            && Parsers.Spaces1(ref scanner, result, out _)
            && LiteralsParser.Identifier(ref scanner, result, out var name)
            && Parsers.Spaces0(ref scanner, result, out _)
            && Tokens.Char(';', ref scanner, advance: true)
        )
        {
            parsed = new TypeDef(type, name, scanner[position..scanner.Position]);
            return true;
        }
        else return Parsers.Exit(ref scanner, result, out parsed, position, orError);
    }

}