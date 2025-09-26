using Stride.Shaders.Core;
using Stride.Shaders.Parsing.Analysis;
using Stride.Shaders.Spirv;
using Stride.Shaders.Spirv.Building;
using Stride.Shaders.Spirv.Core;

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
        Condition.CompileAsValue(table, shader, compiler);
        Body.Compile(table, shader, compiler);
        if (Condition.ValueType != ScalarType.From("bool"))
            table.Errors.Add(new(Condition.Info, "not a boolean"));
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

public class For(Statement initializer, Expression cond, List<Statement> update, Statement body, TextLocation info, ShaderAttribute? attribute = null) : Loop(info)
{
    public Statement Initializer { get; set; } = initializer;
    public Expression Condition { get; set; } = cond;
    public List<Statement> Update { get; set; } = update;
    public Statement Body { get; set; } = body;
    public ShaderAttribute? Attribute = attribute;
    public List<ForAnnotation> Annotations { get; set; } = [];

    public override void Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        var (builder, context, module) = compiler;

        Initializer.Compile(table, shader, compiler);

        var startBranch = new OpBranch(0);
        builder.Insert(startBranch);

        var forCheckBlock = builder.CreateBlock(context, $"for_check_{builder.ForBlockCount}");
        startBranch.TargetLabel = forCheckBlock.Id;

        var conditionValue = Condition.CompileAsValue(table, shader, compiler);
        if (Condition.ValueType != ScalarType.From("bool"))
            table.Errors.Add(new(Condition.Info, "not a boolean"));

        var loopMerge = new OpLoopMerge(0, 0, Specification.LoopControlMask.None);
        builder.Insert(loopMerge);

        var branchConditional = new OpBranchConditional(conditionValue.Id, 0, 0, []);
        builder.Insert(branchConditional);

        // Body block
        var forBodyBlock = builder.CreateBlock(context, $"for_body_{builder.ForBlockCount}");
        branchConditional.TrueLabel = forBodyBlock.Id;
        Body.Compile(table, shader, compiler);
        var forBodyBranch = new OpBranch(0);
        builder.Insert(forBodyBranch);

        // Continue block
        var forContinueBlock = builder.CreateBlock(context, $"for_continue_{builder.ForBlockCount}");
        loopMerge.ContinueTarget = forContinueBlock.Id;
        forBodyBranch.TargetLabel = forContinueBlock.Id;
        foreach (var update in Update)
            update.Compile(table, shader, compiler);
        builder.Insert(new OpBranch(forCheckBlock.Id));

        // Merge block
        var forMergeBlock = builder.CreateBlock(context, $"for_merge_{builder.ForBlockCount}");
        branchConditional.FalseLabel = forMergeBlock.Id;
        loopMerge.MergeBlock = forMergeBlock.Id;

        builder.ForBlockCount++; 
    }

    public override string ToString()
    {
        return $"for({Initializer} {Condition} {Update})\n{Body}";
    }
}

