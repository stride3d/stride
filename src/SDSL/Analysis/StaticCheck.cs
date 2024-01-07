using SDSL.Parsing.AST.Shader.Symbols;

namespace SDSL.Analysis;


public interface IStaticCheck
{
    public bool CheckStatic(SymbolTable s);
}

public interface IStreamCheck
{
    public bool CheckStream(SymbolTable s);
    public IEnumerable<string>? GetUsedStream();
    public IEnumerable<string>? GetAssignedStream();

}

public interface IVariableCheck
{
    public void CheckVariables(SymbolTable s);
}