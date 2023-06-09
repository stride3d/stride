using SoftTouch.Spirv;

namespace SDSL.Parsing.AST.Shader.Analysis;


public class Analyzer
{
    SymbolTable Table;
    ErrorList Errors;
    List<Mixin> Mixins;


    public Analyzer()
    {
        Table = new();
        Errors = new();
    }

    public void Analyze(ShaderProgram program)
    {
        // Recover all mixins and add variables and types to the symbol table
    }
}