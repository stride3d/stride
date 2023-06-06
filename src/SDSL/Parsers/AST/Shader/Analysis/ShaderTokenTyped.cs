namespace SDSL.Parsing.AST.Shader.Analysis;

public abstract class ShaderTokenTyped : ShaderToken
{
    public abstract SymbolType? InferredType{get;set;}
	public abstract void TypeCheck(SymbolTable symbols, in SymbolType? expected);
}