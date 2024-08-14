using Stride.Shaders.Parsing.SDSL.AST;

namespace Stride.Shaders.Parsing.SDSL.Analysis;




public enum SymbolKind
{
    Constant,
    ConstantGeneric,
    Composition,
    Method,
    Variable,
}


public record struct Symbol(string Name, SymbolType Type, SymbolKind Kind);