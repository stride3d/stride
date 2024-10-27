using Stride.Shaders.Parsing.SDFX.Parsers;
using Stride.Shaders.Parsing.SDSL.AST;

namespace Stride.Shaders.Parsing.SDSL;




public record struct ShaderFileParser : IParser<ShaderFile>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out ShaderFile parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        CommonParsers.Spaces0(ref scanner, result, out _);
        var file = new ShaderFile(new(scanner.Memory, ..));
        while (!scanner.IsEof)
        {
            if (
                Terminals.Literal("namespace", ref scanner)
                && NamespaceParsers.Namespace(ref scanner, result, out var ns)
            )
            {
                file.Namespaces.Add(ns);
                CommonParsers.Spaces0(ref scanner, result, out _);
            }
            else if (
                (
                    Terminals.Literal("class", ref scanner) 
                    || Terminals.Literal("shader", ref scanner) 
                    || CommonParsers.SequenceOf(ref scanner, ["internal", "shader"])
                )
                && ShaderClassParsers.Class(ref scanner, result, out var shader)
            )
            {
                file.RootDeclarations.Add(shader);
                CommonParsers.Spaces0(ref scanner, result, out _);
            }
            else if ((Terminals.Literal("effect", ref scanner) || CommonParsers.SequenceOf(ref scanner, ["partial", "effect"]))
                && EffectParser.Effect(ref scanner, result, out var effect)
            )
            {
                file.RootDeclarations.Add(effect);
                CommonParsers.Spaces0(ref scanner, result, out _);
            }
            else if (Terminals.Literal("params", ref scanner)
                && ParamsParsers.Params(ref scanner, result, out var p)
            )
            {
                file.RootDeclarations.Add(p);
                CommonParsers.Spaces0(ref scanner, result, out _);
            }
            else if (Terminals.Literal("using ", ref scanner)
                && UsingNamespace(ref scanner, result, out var uns)
            )
            {
                file.RootDeclarations.Add(uns);
                CommonParsers.Spaces0(ref scanner, result, out _);
            }
            else return CommonParsers.Exit(ref scanner, result, out parsed, position, new(SDSLParsingMessages.SDSL0001, scanner.GetErrorLocation(scanner.Position), scanner.Memory));
        }
        parsed = file;
        return true;
    }
    public static bool UsingNamespace<TScanner>(ref TScanner scanner, ParseResult result, out UsingShaderNamespace parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
        => new UsingNamespaceParser().Match(ref scanner, result, out parsed, orError);
}

public record struct UsingNamespaceParser : IParser<UsingShaderNamespace>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out UsingShaderNamespace parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (Terminals.Literal("using", ref scanner, advance: true) && CommonParsers.Spaces0(ref scanner, result, out _))
        {
            parsed = new(scanner.GetLocation(..));
            do
            {
                if (CommonParsers.FollowedByDel(ref scanner, result, LiteralsParser.Identifier, out Identifier identifier, withSpaces: true, advance: true))
                {
                    parsed.NamespacePath.Add(identifier);
                }
                else return CommonParsers.Exit(ref scanner, result, out parsed, position, new(SDSLParsingMessages.SDSL0001, scanner.GetErrorLocation(scanner.Position), scanner.Memory));
            }
            while (!scanner.IsEof && Terminals.Char('.', ref scanner, advance: true));


            
            if (CommonParsers.FollowedBy(ref scanner, Terminals.Char(';'), withSpaces: true, advance: true))
            {
                parsed.Info = scanner.GetLocation(position..scanner.Position);
                return true;
            }
            else return CommonParsers.Exit(ref scanner, result, out parsed, position, new(SDSLParsingMessages.SDSL0013, scanner.GetErrorLocation(scanner.Position), scanner.Memory));
        }
        return CommonParsers.Exit(ref scanner, result, out parsed, position, orError);
    }


}


public record struct NamespaceParsers : IParser<ShaderNamespace>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out ShaderNamespace parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (
            Terminals.Literal("namespace", ref scanner, advance: true)
            && CommonParsers.Spaces1(ref scanner, result, out _)
        )
        {
            var ns = new ShaderNamespace(new());
            do
            {
                CommonParsers.Spaces0(ref scanner, result, out _);

                if (LiteralsParser.Identifier(ref scanner, result, out var identifier))
                    ns.NamespacePath.Add(identifier);
                else return CommonParsers.Exit(ref scanner, result, out parsed, position, new(SDSLParsingMessages.SDSL0017, scanner.GetErrorLocation(scanner.Position), scanner.Memory));

                CommonParsers.Spaces0(ref scanner, result, out _);
            }
            while (!scanner.IsEof && Terminals.Char('.', ref scanner, advance: true));
            if (ns.NamespacePath.Count > 0)
                ns.Namespace = string.Join(".", ns.NamespacePath);
            if (Terminals.Char(';', ref scanner, advance: true))
            {
                CommonParsers.Spaces0(ref scanner, result, out _);
                while (ShaderClassParsers.Class(ref scanner, result, out var shader))
                {
                    ns.Declarations.Add(shader);
                }
            }
            else if (Terminals.Char('{', ref scanner, advance: true))
            {
                CommonParsers.Spaces0(ref scanner, result, out _);
                while (!scanner.IsEof && !Terminals.Char('}', ref scanner, advance: true))
                {
                    if (ShaderClassParsers.Class(ref scanner, result, out var shader) && CommonParsers.Spaces0(ref scanner, result, out _))
                        ns.Declarations.Add(shader);
                    else if (EffectParser.Effect(ref scanner, result, out var effect) && CommonParsers.Spaces0(ref scanner, result, out _))
                        ns.Declarations.Add(effect);
                    else
                        return CommonParsers.Exit(ref scanner, result, out parsed, position, new(SDSLParsingMessages.SDSL0039, scanner.GetErrorLocation(scanner.Position), scanner.Memory));
                }
            }
            else return CommonParsers.Exit(ref scanner, result, out parsed, position, orError);

            ns.Info = scanner.GetLocation(position, scanner.Position - position);
            parsed = ns;
            return true;
        }
        return CommonParsers.Exit(ref scanner, result, out parsed, position, orError);
    }

    public static bool Namespace<TScanner>(ref TScanner scanner, ParseResult result, out ShaderNamespace parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
        => new NamespaceParsers().Match(ref scanner, result, out parsed, orError);
}