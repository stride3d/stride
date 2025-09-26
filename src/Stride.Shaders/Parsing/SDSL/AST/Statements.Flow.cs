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
        var (builder, context, module) = compiler;

        if (builder.CurrentEscapeBlocks is not { } escapeBlocks)
            throw new InvalidOperationException("Can't process break statement (no context)");

        builder.Insert(new OpBranch(escapeBlocks.MergeBlock));
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
        var (builder, context, module) = compiler;

        if (builder.CurrentEscapeBlocks is not { } escapeBlocks)
            throw new InvalidOperationException("Can't process continue statement (no context)");

        builder.Insert(new OpBranch(escapeBlocks.ContinueBlock));
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

        // Prepare blocks ids
        var forCheckBlock = context.Bound++;
        var forBodyBlock = context.Bound++;
        var previousEscapeBlocks = builder.CurrentEscapeBlocks;
        var currentEscapeBlocks = new SpirvBuilder.EscapeBlocks(context.Bound++, context.Bound++);
        builder.CurrentEscapeBlocks = currentEscapeBlocks;

        builder.Insert(new OpBranch(forCheckBlock));

        // Check block
        builder.CreateBlock(context, forCheckBlock, $"for_check_{builder.ForBlockCount}");

        var conditionValue = Condition.CompileAsValue(table, shader, compiler);
        if (Condition.ValueType != ScalarType.From("bool"))
            table.Errors.Add(new(Condition.Info, "not a boolean"));

        builder.Insert(new OpLoopMerge(currentEscapeBlocks.MergeBlock, currentEscapeBlocks.ContinueBlock, Specification.LoopControlMask.None));
        builder.Insert(new OpBranchConditional(conditionValue.Id, forBodyBlock, currentEscapeBlocks.MergeBlock, []));

        // Body block
        builder.CreateBlock(context, forBodyBlock, $"for_body_{builder.ForBlockCount}");
        Body.Compile(table, shader, compiler);
        builder.Insert(new OpBranch(currentEscapeBlocks.ContinueBlock));

        // Continue block
        builder.CreateBlock(context, currentEscapeBlocks.ContinueBlock, $"for_continue_{builder.ForBlockCount}");
        foreach (var update in Update)
            update.Compile(table, shader, compiler);
        builder.Insert(new OpBranch(forCheckBlock));

        // Merge block
        builder.CreateBlock(context, currentEscapeBlocks.MergeBlock, $"for_merge_{builder.ForBlockCount}");

        builder.ForBlockCount++;
        builder.CurrentEscapeBlocks = previousEscapeBlocks;
    }

    public override string ToString()
    {
        return $"for({Initializer} {Condition} {Update})\n{Body}";
    }
}

