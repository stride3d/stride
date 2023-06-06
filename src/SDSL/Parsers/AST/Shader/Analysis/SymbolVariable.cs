namespace SDSL.Parsing.AST.Shader.Analysis;


public interface ISymbol { }


public class SymbolVariable : ISymbol
{
    public string Name {get;set;}
    public SymbolType Type {get;set;}
    public Declaration? Declaration {get;set;}
}