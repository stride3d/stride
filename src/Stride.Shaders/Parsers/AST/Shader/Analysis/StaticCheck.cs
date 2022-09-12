namespace Stride.Shaders.Parsing.AST.Shader.Analysis;


public interface IStaticCheck
{
    public bool CheckStatic(SymbolTable s);
}

public interface IStreamCheck
{
    public bool CheckStream(SymbolTable s);
    public IEnumerable<string> GetUsedStream();
    public IEnumerable<string> GetAssignedStream();
    
}