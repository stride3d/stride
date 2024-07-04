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
            else if(Terminals.Literal("shader", ref scanner)
                && ShaderClassParsers.Class(ref scanner, result, out var shader)
            )
            {
                file.RootClasses.Add(shader);
                CommonParsers.Spaces0(ref scanner, result, out _);
            }
        }
        parsed = file;
        return true;
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
                if (LiteralsParser.Identifier(ref scanner, result, out var identifier))
                    ns.NamespacePath.Add(identifier);
                else return CommonParsers.Exit(ref scanner, result, out parsed, position, new("Expected identifier", scanner.CreateError(scanner.Position)));
                    
                CommonParsers.Spaces0(ref scanner, result, out _);
            }
            while (!scanner.IsEof && Terminals.Char('.', ref scanner, advance: true));
            if(ns.NamespacePath.Count > 0)
                ns.Namespace = string.Join(".", ns.NamespacePath);
            if (Terminals.Char(';', ref scanner, advance: true))
            {
                CommonParsers.Spaces0(ref scanner, result, out _);
                while (ShaderClassParsers.Class(ref scanner, result, out var shader))
                {
                    ns.ShaderClasses.Add(shader);
                }
            }
            else if (Terminals.Char('{', ref scanner, advance: true))
            {
                CommonParsers.Spaces0(ref scanner, result, out _);
                while (!scanner.IsEof && !Terminals.Char('}', ref scanner, advance: true))
                {
                    if(ShaderClassParsers.Class(ref scanner, result, out var shader) && CommonParsers.Spaces0(ref scanner, result, out _))
                        ns.ShaderClasses.Add(shader);
                    else 
                        return CommonParsers.Exit(ref scanner, result, out parsed, position, new("Expected shader class", scanner.CreateError(scanner.Position)));
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