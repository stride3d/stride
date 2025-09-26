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

    public override unsafe void Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        var (builder, context, module) = compiler;

        var blockTrueBranchPositions = stackalloc int[ElseIfs.Count + 1];
        var selectionMergePositions = stackalloc int[ElseIfs.Count + 1];
        var ifTestConditions = stackalloc int[ElseIfs.Count + 1];

        // Create and connect true/false blocks
        for (int i = 0; i < ElseIfs.Count + 1; ++i)
        {
            var currentIf = i == 0 ? If : ElseIfs[i - 1];

            var conditionValue = currentIf.Condition.CompileAsValue(table, shader, compiler);
            if (currentIf.Condition.ValueType != ScalarType.From("bool"))
                table.Errors.Add(new(currentIf.Condition.Info, "not a boolean"));

            // OpSelectionMerge and OpBranchConditional (will be filled later)
            selectionMergePositions[i] = builder.Position;
            var ifTestMerge = new OpSelectionMerge(0, Specification.SelectionControlMask.None);
            builder.Insert(ifTestMerge);
            ifTestConditions[i] = builder.Position;
            var ifTestCondition = new OpBranchConditional(conditionValue.Id, 0, 0, []);
            builder.Insert(ifTestCondition);

            var blockTrue = builder.CreateBlock(context, $"if_true_{builder.IfBlockCount + i}");
            ifTestCondition.TrueLabel = blockTrue.Id;
            currentIf.Body.Compile(table, shader, compiler);

            // Do we have a specific false block?
            if (i + 1 < ElseIfs.Count + 1 || Else != null)
            {
                var blockTrueBranch = new OpBranch(0);
                blockTrueBranchPositions[i] = builder.Position;
                builder.Insert(blockTrueBranch);

                var blockFalse = builder.CreateBlock(context, $"if_false_{builder.IfBlockCount + i}");
                ifTestCondition.FalseLabel = blockFalse.Id;

                // If there's an else without condition and we are at the last iteration, add the code now (otherwise it will happen next loop)
                if (i + 1 == ElseIfs.Count + 1)
                    Else!.Compile(table, shader, compiler);
            }
        }

        // Create and connect merge branches
        for (int i = ElseIfs.Count; i >= 0; --i)
        {
            var mergeBranch = new OpBranch(0);
            builder.Insert(mergeBranch);

            var blockMerge = builder.CreateBlock(context, $"if_merge_{builder.IfBlockCount + i}");
            mergeBranch.TargetLabel = blockMerge.Id;

            if (i + 1 < ElseIfs.Count + 1 || Else != null)
            {
                var blockTrueBranch = (OpBranch)builder.GetBuffer()[blockTrueBranchPositions[i]];
                blockTrueBranch.TargetLabel = blockMerge.Id;
            }
            else
            {
                // If there is no false block, we adjust the OpConditionlBranch false to directly point to the merge block
                var ifTestCondition = (OpBranchConditional)builder.GetBuffer()[ifTestConditions[i]];
                ifTestCondition.FalseLabel = blockMerge.Id;
            }

            var selectionMerge = (OpSelectionMerge)builder.GetBuffer()[selectionMergePositions[i]];
            selectionMerge.MergeBlock = blockMerge.Id;
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

    public override void Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
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
    public override void Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        throw new InvalidOperationException("Handled by ConditionalFlow");
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