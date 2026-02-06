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
    public override void ProcessSymbol(SymbolTable table)
    {
    }
    public override void Compile(SymbolTable table, CompilerUnit compiler)
    {
        var (builder, context) = compiler;

        if (builder.CurrentEscapeBlocks is not { } escapeBlocks)
            throw new InvalidOperationException("Can't process break statement (no context)");

        builder.Insert(new OpBranch(escapeBlocks.MergeBlock));
    }
}
public class Discard(TextLocation info) : Statement(info)
{
    public override void ProcessSymbol(SymbolTable table)
    {
    }
    public override void Compile(SymbolTable table, CompilerUnit compiler)
    {
        throw new NotImplementedException();
    }
}
public class Continue(TextLocation info) : Statement(info)
{
    public override void ProcessSymbol(SymbolTable table)
    {
    }
    public override void Compile(SymbolTable table, CompilerUnit compiler)
    {
        var (builder, context) = compiler;

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

    public SymbolFrame SymbolFrame { get; set; }

    public override void ProcessSymbol(SymbolTable table)
    {
        Collection.ProcessSymbol(table);
        if (!(Collection.Type is PointerType p && p.BaseType is ArrayType arrayType))
            throw new InvalidOperationException("foreach: Array type is expected");


        var variableType = new PointerType(arrayType.BaseType, Specification.StorageClass.Function);
        Variable.Type = variableType;

        if (TypeName.Name != "var")
            TypeName.ProcessSymbol(table, variableType);
        else
            TypeName.Type = arrayType.BaseType;
        
        // TODO: check conversions
        if (variableType.BaseType != TypeName.Type)
            throw new InvalidOperationException("foreach: collection and variable type not matching");
        
        table.Push();
        var variableSymbol = new Symbol(new(Variable.Name, SymbolKind.Variable), Variable.Type, 0, OwnerType: table.CurrentShader);
        table.CurrentFrame.Add(Variable.Name, variableSymbol);
        Body.ProcessSymbol(table);
        SymbolFrame = table.Pop();
    }
    public override void Compile(SymbolTable table, CompilerUnit compiler)
    {
        var (builder, context) = compiler;

        var collection = Collection.Compile(table, compiler);
        if (!(Collection.Type is PointerType p && p.BaseType is ArrayType arrayType))
            throw new InvalidOperationException("foreach: Array type is expected");

        var variableType = new PointerType(arrayType.BaseType, Specification.StorageClass.Function);

        // Since foreach need to be processed and expanded later, we use custom opcode
        // (we could emit a "For" loop statement, but it would be too complex to write a general decompiler for a "for" loop when processing it later)
        var variableId = builder.Insert(new OpForeachSDSL(context.GetOrRegister(variableType), context.Bound++, collection.Id));
        table.Push(SymbolFrame);
        SymbolFrame.UpdateId(Variable.Name, variableId);
        Body.Compile(table, compiler);
        table.Pop();
        builder.Insert(new OpForeachEndSDSL());
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

    public override void Compile(SymbolTable table, CompilerUnit compiler)
    {
        var (builder, context) = compiler;

        var conditionValue = Condition.CompileAsValue(table, compiler);
        if (Condition.ValueType is not ScalarType)
            table.AddError(new(Condition.Info, "while statement condition expression must evaluate to a scalar"));

        // Might need implicit conversion from float/int to bool
        conditionValue = builder.Convert(context, conditionValue, ScalarType.Boolean);

        Body.Compile(table, compiler);
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

    public override void ProcessSymbol(SymbolTable table)
    {
        Initializer.ProcessSymbol(table);
        Condition.ProcessSymbol(table);
        Body.ProcessSymbol(table);
        foreach (var update in Update)
            update.ProcessSymbol(table);
    }

    public override void Compile(SymbolTable table, CompilerUnit compiler)
    {
        var (builder, context) = compiler;

        Initializer.Compile(table, compiler);

        // Prepare blocks ids
        var forCheckBlock = context.Bound++;
        var forBodyBlock = context.Bound++;
        var previousEscapeBlocks = builder.CurrentEscapeBlocks;
        var currentEscapeBlocks = new SpirvBuilder.EscapeBlocks(context.Bound++, context.Bound++);
        builder.CurrentEscapeBlocks = currentEscapeBlocks;

        builder.Insert(new OpBranch(forCheckBlock));

        // Check block
        builder.CreateBlock(context, forCheckBlock, $"for_check_{builder.ForBlockCount}");

        var conditionValue = Condition.CompileAsValue(table, compiler);
        if (Condition.ValueType is not ScalarType)
            table.AddError(new(Condition.Info, "for statement condition expression must evaluate to a scalar"));

        // Might need implicit conversion from float/int to bool
        conditionValue = builder.Convert(context, conditionValue, ScalarType.Boolean);

        builder.Insert(new OpLoopMerge(currentEscapeBlocks.MergeBlock, currentEscapeBlocks.ContinueBlock, Specification.LoopControlMask.None, []));
        builder.Insert(new OpBranchConditional(conditionValue.Id, forBodyBlock, currentEscapeBlocks.MergeBlock, []));

        // Body block
        builder.CreateBlock(context, forBodyBlock, $"for_body_{builder.ForBlockCount}");
        Body.Compile(table, compiler);
        if (!SpirvBuilder.IsBlockTermination(builder.GetLastInstructionType()))
            builder.Insert(new OpBranch(currentEscapeBlocks.ContinueBlock));

        // Continue block
        builder.CreateBlock(context, currentEscapeBlocks.ContinueBlock, $"for_continue_{builder.ForBlockCount}");
        foreach (var update in Update)
            update.Compile(table, compiler);
        if (!SpirvBuilder.IsBlockTermination(builder.GetLastInstructionType()))
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

