using Stride.Shaders;
using Stride.Shaders.Core;
using Stride.Shaders.Spirv.Building;
using Stride.Shaders.Parsing.Analysis;
using Stride.Shaders.Spirv;
using Stride.Shaders.Spirv.Core;

namespace Stride.Shaders.Parsing.SDSL.AST;


public abstract class Control(TextLocation info) : Flow(info);


public class ConditionalFlow(If first, TextLocation info) : Flow(info)
{
    public If If { get; set; } = first;
    public List<ElseIf> ElseIfs { get; set; } = [];
    public Else? Else { get; set; }
    public ShaderAttributeList? Attributes { get; set; }

    public override unsafe void Compile(SymbolTable table, CompilerUnit compiler)
    {
        var (builder, context) = compiler;

        var blockTrueIds = stackalloc int[ElseIfs.Count + 1];
        var blockMergeIds = stackalloc int[ElseIfs.Count + 1];
        var isMergeBlockReachable = stackalloc bool[ElseIfs.Count + 1];

        // Create and connect true/false blocks
        for (int i = 0; i < ElseIfs.Count + 1; ++i)
        {
            var currentIf = i == 0 ? If : ElseIfs[i - 1];

            blockTrueIds[i] = context.Bound++;
            blockMergeIds[i] = context.Bound++;

            var conditionValue = currentIf.Condition.CompileAsValue(table, compiler);
            if (currentIf.Condition.ValueType != ScalarType.From("bool"))
                table.Errors.Add(new(currentIf.Condition.Info, "not a boolean"));

            int? falseBlock = (i + 1 < ElseIfs.Count + 1 || Else != null)
                ? context.Bound++
                : null;

            // OpSelectionMerge and OpBranchConditional
            builder.Insert(new OpSelectionMerge(blockMergeIds[i], Specification.SelectionControlMask.None));
            builder.Insert(new OpBranchConditional(conditionValue.Id, blockTrueIds[i], falseBlock ?? blockMergeIds[i], []));

            builder.CreateBlock(context, blockTrueIds[i], $"if_true_{builder.IfBlockCount + i}");
            currentIf.Body.Compile(table, compiler);

            // Do we have a specific false block?
            if (falseBlock != null)
            {
                if (!SpirvBuilder.IsBlockTermination(builder.GetLastInstructionType()))
                {
                    isMergeBlockReachable[i] = true;
                    builder.Insert(new OpBranch(blockMergeIds[i]));
                }

                builder.CreateBlock(context, falseBlock.Value, $"if_false_{builder.IfBlockCount + i}");

                // If there's an else without condition and we are at the last iteration, add the code now (otherwise it will happen next loop)
                if (i + 1 == ElseIfs.Count + 1)
                    Else!.Compile(table, compiler);
            }
            else
            {
                isMergeBlockReachable[i] = true;
            }
        }

        // Create and connect merge branches
        for (int i = ElseIfs.Count; i >= 0; --i)
        {
            if (!SpirvBuilder.IsBlockTermination(builder.GetLastInstructionType()))
            {
                isMergeBlockReachable[i] = true;
                builder.Insert(new OpBranch(blockMergeIds[i]));
            }
            builder.CreateBlock(context, blockMergeIds[i], $"if_merge_{builder.IfBlockCount + i}");
            if (!isMergeBlockReachable[i])
                builder.Insert(new OpUnreachable());
        }

        builder.IfBlockCount += ElseIfs.Count + 1;
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

    public override void Compile(SymbolTable table, CompilerUnit compiler)
    {
        throw new InvalidOperationException("Handled by ConditionalFlow");
    }

    public override string ToString()
    {
        return $"if({Condition})\n{Body}";
    }
}

public class ElseIf(Expression condition, Statement body, TextLocation info) : If(condition, body, info)
{
    public override void Compile(SymbolTable table, CompilerUnit compiler)
    {
        throw new InvalidOperationException("Handled by ConditionalFlow");
        Condition.CompileAsValue(table, compiler);
        Body.Compile(table, compiler);
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

    public override void Compile(SymbolTable table, CompilerUnit compiler)
    {
        Body.Compile(table, compiler);
    }
    public override string ToString()
    {
        return $"else {Body}";
    }
}