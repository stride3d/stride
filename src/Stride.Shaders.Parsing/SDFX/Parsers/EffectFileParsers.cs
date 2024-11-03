using Stride.Shaders.Parsing.SDFX.AST;
using Stride.Shaders.Parsing.SDSL;

namespace Stride.Shaders.Parsing.SDFX.Parsers;

// public record struct EffectFileParser : IParser<EffectFile>
// {
//     public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out EffectFile parsed, in ParseError? orError = null) 
//         where TScanner : struct, IScanner
//     {
//         var position = scanner.Position;

//         CommonParsers.Spaces0(ref scanner, result, out _);
//         throw new NotImplementedException();
//     }
// }


// public record struct EffectNamespaceParser : IParser<ShaderNamespace>
// {
//     public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out ShaderNamespace parsed, in ParseError? orError = null) where TScanner : struct, IScanner
//     {
//         var position = scanner.Position;

//         if(Terminals.Literal("namespace", ref scanner, advance: true) && CommonParsers.Spaces1(ref scanner, result, out _))
//         {
//             do
//             {
                
//             }
//             while (!scanner.IsEof && !Terminals.Char(';', ref scanner) && Terminals.Char('.', ref scanner, advance: true));
//         }
//         return CommonParsers.Exit(ref scanner, result, out parsed, position, orError);

//     }
// }