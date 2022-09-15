namespace Stride.Shaders.Parsing.AST.Shader.Analysis;

public abstract class ShaderTokenTyped : ShaderToken
{
    public abstract ISymbolType InferredType{get;set;}
	public abstract void TypeCheck(SymbolTable symbols, ISymbolType expected);
}