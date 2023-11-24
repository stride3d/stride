using CommunityToolkit.HighPerformance;
using SDSL.Parsing.AST.Shader.Symbols;
using SoftTouch.Spirv;

namespace SDSL.Parsing.AST.Shader.Analysis;

public struct Machin
{
    public int Value { get; set; }
}

public static class MachinExtensions
{
    public static ref Machin AddOne(this ref Machin machin)
    {
        machin.Value += 1;
        return ref machin;
    }
}


public class Analyzer
{
    SymbolTable Table;
    ErrorList Errors;
    List<Mixin> Mixins;


    public Analyzer()
    {
        Table = new();
        Table = new();
        Errors = [];
        Mixins = [];
    }

    public void Analyze(ShaderProgram program)
    {
        // Recover all mixins and add variables and types to the symbol table
        TypeCheck(program);
    }

    public void TypeCheck(ShaderProgram program)
    {
        foreach (var func in program.Body.OfType<ShaderMethod>())
            foreach (var statement in func.Statements)
            {
                TypeCheck(statement);
            }
    }
    public void TypeCheck(Statement statement)
    {
        if (statement is BlockStatement block)
            foreach (var s in block.Statements)
                TypeCheck(s);
        else
            throw new NotImplementedException();
    }

}