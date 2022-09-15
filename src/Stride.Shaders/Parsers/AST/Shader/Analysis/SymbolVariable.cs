namespace Stride.Shaders.Parsing.AST.Shader.Analysis;

public class SymbolVariable : ISymbol
{
    public string? Name {get;set;}
    public ISymbolType? Type {get;set;}
    public Declaration? Declaration {get;set;}
}