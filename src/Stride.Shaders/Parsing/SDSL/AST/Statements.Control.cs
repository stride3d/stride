using Stride.Shaders;
using Stride.Shaders.Core;
using Stride.Shaders.Spirv.Building;
using Stride.Shaders.Parsing.Analysis;

namespace Stride.Shaders.Parsing.SDSL.AST;


public abstract class Control(TextLocation info) : Flow(info);


public class ConditionalFlow(If first, TextLocation info) : Flow(info)
{
    public If If { get; set; } = first;
    public List<ElseIf> ElseIfs { get; set; } = [];
    public Else? Else { get; set; }
    public ShaderAttributeList? Attributes { get; set; }

    public override void ProcessSymbol(SymbolTable table, ShaderMethod method)
    {
        If.ProcessSymbol(table, method);
        foreach (var ei in ElseIfs)
            ei.ProcessSymbol(table, method);
        Else?.ProcessSymbol(table, method);

    }
    
    public override void Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        throw new NotImplementedException();
    }

    public override string ToString()
    {
        return $"{If}{string.Join("\n", ElseIfs.Select(x => x.ToString()))}{Else}";
    }
}
public class If(Expression condition, Statement body, TextLocation info) : Flow(info)
{
    public Expression Condition { get; set; } = condition;
    public Statement Body { get; set; } = body;

    public override void ProcessSymbol(SymbolTable table, ShaderMethod method)
    {
        Condition.ProcessSymbol(table);
        Body.ProcessSymbol(table, method);
        if(Condition.Type != ScalarType.From("bool"))
            table.Errors.Add(new(Condition.Info, "not a boolean"));
    }
    public override void Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        throw new NotImplementedException();
    }

    public override string ToString()
    {
        return $"if({Condition})\n{Body}";
    }
}

public class ElseIf(Expression condition, Statement body, TextLocation info) : If(condition, body, info)
{
    public override void ProcessSymbol(SymbolTable table, ShaderMethod method)
    {
        Condition.ProcessSymbol(table);
        Body.ProcessSymbol(table, method);
        if(Condition.Type != ScalarType.From("bool"))
            table.Errors.Add(new(Condition.Info, "not a boolean"));
    }
    public override void Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        throw new NotImplementedException();
    }
    public override string ToString()
    {
        return $"else if({Condition}){Body}";
    }
}

public class Else(Statement body, TextLocation info) : Flow(info)
{
    public Statement Body { get; set; } = body;

    public override void ProcessSymbol(SymbolTable table, ShaderMethod method)
    {
        Body.ProcessSymbol(table, method);
    }
    public override void Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        throw new NotImplementedException();
    }
    public override string ToString()
    {
        return $"else {Body}";
    }
}