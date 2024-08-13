using Stride.Shaders.Parsing.SDSL.AST;

namespace Stride.Shaders.Parsing.SDSL.Analysis;




public enum SymbolKind
{
    Variable,
    Method
}


public record struct Symbol(Identifier Name, SymbolType Type, SymbolKind Kind);