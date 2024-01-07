using SDSL.Parsing.AST.Shader;
using SDSL.Parsing.AST.Shader.Symbols;

namespace SDSL.Symbols;

public record struct MethodSymbol(SymbolTable Table, ModuleMethod Method)
{
    public readonly string Name => Method.Name;
    public readonly SymbolType Type => Method.ReturnType;
    public readonly List<MethodParameter>? Parameters => Method.ParameterList;

}
