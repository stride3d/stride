namespace Stride.Shaders.Parsing.AST.Shader.Analysis;

public abstract class ShaderTokenTyped : ShaderToken
{
    public abstract string InferredType{get;set;}
	public abstract void TypeCheck(SymbolTable symbols, string expected = "");
}