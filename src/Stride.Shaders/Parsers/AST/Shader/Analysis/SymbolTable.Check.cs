namespace Stride.Shaders.Parsing.AST.Shader.Analysis;


public partial class SymbolTable
{
    public void CheckVar(Statement s)
    {
        if(s is IVariableCheck v)
            v.CheckVariables(this);
    }
}