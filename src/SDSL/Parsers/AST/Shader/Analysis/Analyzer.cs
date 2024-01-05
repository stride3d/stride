using CommunityToolkit.HighPerformance;
using SDSL.Parsing.AST.Shader.Symbols;
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
        {
            Table.Variables.PushScope();
            foreach (var statement in func.Statements)
            {
                TypeCheck(statement);
            }
            Table.Variables.PopScope();
        }
    }
    public void TypeCheck(Statement statement)
    {
        if (statement is BlockStatement block)
        {
            Table.Variables.PushScope();
            foreach (var s in block.Statements)
                TypeCheck(s);
            Table.Variables.PopScope();
        }
        else
        {
            statement.TypeCheck(Table, null);
            if (statement is Declaration da)
                Table.Variables.Push(new VariableSymbol(da.VariableName, da.TypeName ?? SymbolType.Void));

        }
    }

}