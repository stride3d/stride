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

    public override void Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        If.Compile(table, shader, compiler);
        foreach (var ei in ElseIfs)
            ei.Compile(table, shader, compiler);
        Else?.Compile(table, shader, compiler);
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

    public override void Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        Condition.CompileAsValue(table, shader, compiler);
        Body.Compile(table, shader, compiler);
        if (Condition.ValueType != ScalarType.From("bool"))
            table.Errors.Add(new(Condition.Info, "not a boolean"));
        throw new NotImplementedException();
    }

    public override string ToString()
    {
        return $"if({Condition})\n{Body}";
    }
}

public class ElseIf(Expression condition, Statement body, TextLocation info) : If(condition, body, info)
{
    public override void Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        Condition.CompileAsValue(table, shader, compiler);
        Body.Compile(table, shader, compiler);
        if (Condition.ValueType != ScalarType.From("bool"))
            table.Errors.Add(new(Condition.Info, "not a boolean"));
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

    public override void Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        Body.Compile(table, shader, compiler);
    }
    public override string ToString()
    {
        return $"else {Body}";
    }
}