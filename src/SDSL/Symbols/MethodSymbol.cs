using SDSL.Parsing.AST.Shader.Symbols;

namespace SDSL.Symbols;

public record struct MethodSymbol(SymbolTable Table, string Name, string TypeName);

