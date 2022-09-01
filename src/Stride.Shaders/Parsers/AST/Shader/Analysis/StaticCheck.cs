namespace Stride.Shaders.Parsing.AST.Shader.Analysis;


public interface IStaticCheck
{
    
    public bool UsesShaderVar {get;set;}
    public void CheckStatic(SymbolTable s);
}

public interface IStreamCheck
{
    public bool UsesStream {get;set;}
    public void CheckStream(SymbolTable s);
}