using Stride.Shaders.Parsing.SDSL.AST;

namespace Stride.Shaders.Parsing.SDSL;


public record struct ShaderElementParsers : IParser<ShaderElement>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out ShaderElement parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if(TypeDef(ref scanner, result, out var typeDef))
        {
            parsed = typeDef;
            return true;
        }
        else if (BufferParsers.Buffer(ref scanner, result, out var buffer))
        {
            CommonParsers.FollowedBy(ref scanner, Terminals.Char(';'), withSpaces: true, advance: true);
            parsed = buffer;
            return true;
        }
        else if(Struct(ref scanner, result, out var structElement))
        {
            CommonParsers.FollowedBy(ref scanner, Terminals.Char(';'), withSpaces: true, advance: true);
            parsed = structElement;
            return true;
        }
        else
        {
            bool isOverride = false;
            bool isStaged = false;
            bool isStreamed = false;
            bool hasAttributes = ShaderAttributeListParser.AttributeList(ref scanner, result, out var attributes, orError);
            var tmpPos = scanner.Position;
            #warning override keyword should always happen after stage and stream
            if (Terminals.Literal("override", ref scanner, advance: true) && CommonParsers.Spaces1(ref scanner, result, out _))
            {
                isOverride = true;
                tmpPos = scanner.Position;
            }
            else
                scanner.Position = tmpPos;
            if (Terminals.Literal("stage", ref scanner, advance: true) && CommonParsers.Spaces1(ref scanner, result, out _))
            {
                isStaged = true;
                tmpPos = scanner.Position;
            }
            else
                scanner.Position = tmpPos;
            if (Terminals.Literal("stream", ref scanner, advance: true) && CommonParsers.Spaces1(ref scanner, result, out _))
                isStreamed = true;
            else
                scanner.Position = tmpPos;
            if(Compose(ref scanner, result, out var compose))
            {
                compose.IsStaged = isStaged;
                if(hasAttributes)
                    compose.Attributes = attributes.Attributes;
                parsed = compose;
                return true;
            }
            else if (Method(ref scanner, result, out var method))
            {
                method.IsOverride = isOverride;
                method.IsStaged = isStaged;
                if(hasAttributes)
                    method.Attributes = attributes.Attributes;
                parsed = method;
                return true;
            }
            else if (ShaderMemberParser.Member(ref scanner, result, out var member))
            {
                member.IsStream = isStreamed;
                member.IsStaged = isStaged;
                if(hasAttributes)
                    member.Attributes = attributes.Attributes;
                parsed = member;
                return true;
            }
            
            
            else return CommonParsers.Exit(ref scanner, result, out parsed, position);
        }
        
    }
    public static bool Compose<TScanner>(ref TScanner scanner, ParseResult result, out ShaderCompose parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
        => new CompositionParser().Match(ref scanner, result, out parsed, in orError);
    public static bool Struct<TScanner>(ref TScanner scanner, ParseResult result, out ShaderStruct parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
        => new ShaderStructParser().Match(ref scanner, result, out parsed, in orError);
    public static bool ShaderElement<TScanner>(ref TScanner scanner, ParseResult result, out ShaderElement parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
        => new ShaderElementParsers().Match(ref scanner, result, out parsed, in orError);

    public static bool Method<TScanner>(ref TScanner scanner, ParseResult result, out ShaderMethod parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
        => new ShaderMethodParsers().Match(ref scanner, result, out parsed, in orError);

    public static bool TypeDef<TScanner>(ref TScanner scanner, ParseResult result, out ShaderElement parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;

        if (
            Terminals.Literal("typedef", ref scanner, advance: true)
            && CommonParsers.Spaces1(ref scanner, result, out _)
            && LiteralsParser.TypeName(ref scanner, result, out var type)
            && CommonParsers.Spaces1(ref scanner, result, out _)
            && LiteralsParser.Identifier(ref scanner, result, out var name)
            && CommonParsers.Spaces0(ref scanner, result, out _)
            && Terminals.Char(';', ref scanner, advance: true)
        )
        {
            parsed = new TypeDef(type, name, scanner.GetLocation(position..scanner.Position));
            return true;
        }
        else return CommonParsers.Exit(ref scanner, result, out parsed, position, orError);

    }

}