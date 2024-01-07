using CommunityToolkit.HighPerformance;
using SDSL.Parsing.AST.Shader;
using SDSL.Parsing.AST.Shader.Symbols;
using SDSL.Symbols;
using SoftTouch.Spirv;

namespace SDSL.Analysis;

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
        foreach (var m in program.Body.OfType<ModuleMethod>())
            Table.Methods.Add(m.Name, new(Table, m));
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