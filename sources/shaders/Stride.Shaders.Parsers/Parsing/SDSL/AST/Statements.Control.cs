using Stride.Shaders;
using Stride.Shaders.Core;
using Stride.Shaders.Spirv.Building;
using Stride.Shaders.Parsing.Analysis;
using Stride.Shaders.Spirv;
using Stride.Shaders.Spirv.Core;
using Stride.Shaders.Spirv.Core.Buffers;

namespace Stride.Shaders.Parsing.SDSL.AST;


public abstract class Control(TextLocation info) : Flow(info);


public partial class ConditionalFlow(If first, TextLocation info) : Flow(info)
{
    public If If { get; set; } = first;
    public List<ElseIf> ElseIfs { get; set; } = [];
    public Else? Else { get; set; }
    public ShaderAttributeList? Attributes { get; set; }

    public override void ProcessSymbol(SymbolTable table)
    {
        If.ProcessSymbol(table);
        foreach (var elseIf in ElseIfs)
            elseIf.ProcessSymbol(table);
        Else?.ProcessSymbol(table);
    }

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
            if (currentIf.Condition.ValueType is not ScalarType)
                table.AddError(new(currentIf.Condition.Info, "if statement conditional expressions must evaluate to a scalar"));

            // Might need implicit conversion from float/int to bool
            conditionValue = builder.Convert(context, conditionValue, ScalarType.Boolean);

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
public partial class If(Expression condition, Statement body, TextLocation info) : Flow(info)
{
    public Expression Condition { get; set; } = condition;
    public Statement Body { get; set; } = body;

    public override void ProcessSymbol(SymbolTable table)
    {
        Condition.ProcessSymbol(table);
        Body.ProcessSymbol(table);
    }

    public override void Compile(SymbolTable table, CompilerUnit compiler)
    {
        throw new InvalidOperationException("Handled by ConditionalFlow");
    }

    public override string ToString()
    {
        return $"if({Condition})\n{Body}";
    }
}

public partial class ElseIf(Expression condition, Statement body, TextLocation info) : If(condition, body, info)
{
    public override string ToString()
    {
        return $"else if({Condition}){Body}";
    }
}

public partial class Else(Statement body, TextLocation info) : Flow(info)
{
    public Statement Body { get; set; } = body;

    public override void ProcessSymbol(SymbolTable table)
    {
        Body.ProcessSymbol(table);
    }
    public override void Compile(SymbolTable table, CompilerUnit compiler)
    {
        Body.Compile(table, compiler);
    }
    public override string ToString()
    {
        return $"else {Body}";
    }
}


public partial class SwitchStatement(Expression selector, TextLocation info) : Flow(info)
{
    public Expression Selector { get; set; } = selector;
    public List<SwitchSection> Sections { get; set; } = [];

    public override void ProcessSymbol(SymbolTable table)
    {
        Selector.ProcessSymbol(table);
        foreach (var section in Sections)
        {
            foreach (var label in section.Labels)
            {
                if (label is CaseLabel caseLabel)
                    caseLabel.Value.ProcessSymbol(table);
            }
            foreach (var stmt in section.Statements)
                stmt.ProcessSymbol(table);
        }
    }

    public override void Compile(SymbolTable table, CompilerUnit compiler)
    {
        var (builder, context) = compiler;

        // Compile selector (must be integer scalar)
        var selectorValue = Selector.CompileAsValue(table, compiler);
        if (Selector.ValueType is not ScalarType st || !st.IsInteger())
            table.AddError(new(Selector.Info, "switch selector must evaluate to an integer scalar"));

        // Pre-allocate block IDs: one per section + merge block
        var mergeBlock = context.Bound++;
        var sectionBlockIds = new int[Sections.Count];
        for (int i = 0; i < Sections.Count; i++)
            sectionBlockIds[i] = context.Bound++;

        // Set up escape blocks so break targets the merge block
        var previousEscapeBlocks = builder.CurrentEscapeBlocks;
        builder.CurrentEscapeBlocks = new SpirvBuilder.EscapeBlocks(mergeBlock, mergeBlock);

        // Build (literal, blockId) pairs and find default block
        int defaultBlockId = mergeBlock;
        var casePairs = new List<(int, int)>();
        for (int i = 0; i < Sections.Count; i++)
        {
            foreach (var label in Sections[i].Labels)
            {
                if (label is DefaultLabel)
                    defaultBlockId = sectionBlockIds[i];
                else if (label is CaseLabel caseLabel && caseLabel.Value is IntegerLiteral intLit)
                    casePairs.Add(((int)intLit.Value, sectionBlockIds[i]));
                else
                    table.AddError(new(label.Info, "case label must be an integer literal"));
            }
        }

        // Emit selection merge + switch
        builder.Insert(new OpSelectionMerge(mergeBlock, Specification.SelectionControlMask.None));
        Span<(int, int)> pairsSpan = casePairs.ToArray();
        builder.Insert(new OpSwitch(selectorValue.Id, defaultBlockId, new LiteralArray<(int, int)>(pairsSpan)));

        // Compile each section's block
        for (int i = 0; i < Sections.Count; i++)
        {
            builder.CreateBlock(context, sectionBlockIds[i], $"switch_case_{builder.SwitchBlockCount}_{i}");
            foreach (var stmt in Sections[i].Statements)
                stmt.Compile(table, compiler);
            if (!SpirvBuilder.IsBlockTermination(builder.GetLastInstructionType()))
                builder.Insert(new OpBranch(mergeBlock));
        }

        // Merge block
        builder.CreateBlock(context, mergeBlock, $"switch_merge_{builder.SwitchBlockCount}");

        builder.SwitchBlockCount++;
        builder.CurrentEscapeBlocks = previousEscapeBlocks;
    }
}

public class SwitchSection(List<SwitchLabel> labels, List<Statement> statements, TextLocation info)
{
    public TextLocation Info { get; set; } = info;
    public List<SwitchLabel> Labels { get; set; } = labels;
    public List<Statement> Statements { get; set; } = statements;
}

public abstract class SwitchLabel(TextLocation info)
{
    public TextLocation Info { get; set; } = info;
}

public class CaseLabel(Expression value, TextLocation info) : SwitchLabel(info)
{
    public Expression Value { get; set; } = value;
}

public class DefaultLabel(TextLocation info) : SwitchLabel(info);
