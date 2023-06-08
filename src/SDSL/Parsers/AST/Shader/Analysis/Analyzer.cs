namespace SDSL.Parsing.AST.Shader.Analysis;


public class Analyzer
{
    SymbolTable Table;


    public Analyzer()
    {
        Table = new();
    }

    public void Analyze(ShaderProgram program)
    {
        // Recover all mixins and add variables and types to the symbol table
    }
}