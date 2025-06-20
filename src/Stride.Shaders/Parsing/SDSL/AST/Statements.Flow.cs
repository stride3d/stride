using Stride.Shaders.Core;
using Stride.Shaders.Parsing.Analysis;
using Stride.Shaders.Spirv.Building;

namespace Stride.Shaders.Parsing.SDSL.AST;

public abstract class Flow(TextLocation info) : Statement(info);

public abstract class Loop(TextLocation info) : Flow(info);
public class Break(TextLocation info) : Statement(info)
{
    public override void Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        throw new NotImplementedException();
    }
}
public class Discard(TextLocation info) : Statement(info)
{
    public override void Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        throw new NotImplementedException();
    }
}
public class Continue(TextLocation info) : Statement(info)
{
    public override void Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        throw new NotImplementedException();
    }
}


public class ForEach(TypeName typename, Identifier variable, Expression collection, Statement body, TextLocation info) : Loop(info)
{
    public TypeName TypeName { get; set; } = typename;
    public Identifier Variable { get; set; } = variable;
    public Expression Collection { get; set; } = collection;
    public Statement Body { get; set; } = body;


    public override void Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        Collection.Compile(table, shader, compiler);
        if (Collection.Type is ArrayType arrSym)
        {
            var btype = arrSym.BaseType;
            TypeName.Compile(table, shader, compiler);
        }
        throw new NotImplementedException();
    }

    public override string ToString()
    {
        return $"foreach({TypeName} {Variable} in {Collection})\n{Body}";
    }
}


public class While(Expression condition, Statement body, TextLocation info, ShaderAttribute? attribute = null) : Loop(info)
{
    public Expression Condition { get; set; } = condition;
    public Statement Body { get; set; } = body;
    public ShaderAttribute? Attribute { get; internal set; } = attribute;

    public override void Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        Condition.Compile(table, shader, compiler);
        Body.Compile(table, shader, compiler);
        throw new NotImplementedException();
    }

    public override string ToString()
    {
        return $"while({Condition})\n{Body}";
    }
}

public enum ForAnnotationKind
{
    Unroll,
    Loop,
    Fastopt,
    AllowUAVCondition
}
public record struct ForAnnotation(ForAnnotationKind Kind, int? Count = null);

public class For(Statement initializer, Statement cond, List<Statement> update, Statement body, TextLocation info, ShaderAttribute? attribute = null) : Loop(info)
{
    public Statement Initializer { get; set; } = initializer;
    public Statement Condition { get; set; } = cond;
    public List<Statement> Update { get; set; } = update;
    public Statement Body { get; set; } = body;
    public ShaderAttribute? Attribute = attribute;
    public List<ForAnnotation> Annotations { get; set; } = [];

    public override void Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        throw new NotImplementedException();
    }

    public override string ToString()
    {
        return $"for({Initializer} {Condition} {Update})\n{Body}";
    }
}

