using SDSL.Parsing.AST.Shader.Symbols;
using SDSL.Symbols;

namespace SDSL.Parsing.AST.Shader;

public abstract class ShaderTokenTyped : ShaderToken
{
    public abstract SymbolType? InferredType { get; set; }
    public abstract void TypeCheck(SymbolTable symbols, in SymbolType? expected);
}