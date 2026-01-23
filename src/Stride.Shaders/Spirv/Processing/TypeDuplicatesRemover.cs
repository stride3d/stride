using CommunityToolkit.HighPerformance.Buffers;
using Stride.Shaders.Spirv.Building;
using Stride.Shaders.Spirv.Core;
using Stride.Shaders.Spirv.Core.Buffers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static Stride.Shaders.Spirv.Specification;

namespace Stride.Shaders.Spirv.Processing;




/// <summary>
/// Remove duplicate simple types.
/// Should be applied after the IdRefOffsetter.
/// </summary>
public class TypeDuplicateHelper
{
    public int[] FindItemsWithTypes(NewSpirvBuffer buffer, params Span<Op> ops)
    {
        var itemCount = 0;
        for (var index = 0; index < buffer.Count; index++)
        {
            var i = buffer[index];
            if (ops.Contains(i.Op))
                itemCount++;
        }
        var result = new int[itemCount];
        itemCount = 0;
        for (var index = 0; index < buffer.Count; index++)
        {
            var i = buffer[index];
            if (ops.Contains(i.Op))
                result[itemCount++] = index;
        }
        return result;
    }

    // Note: Target is only for OpName and OpMember
    record struct InstructionSortHelper(Op Op, int Index, OpData Data)
    {
        public InstructionSortHelper(OpDataIndex i) : this(i.Op, i.Index, i.Data) { }

        public override string ToString() => Data.Memory != null ? Data.ToString() : $"{Op} Index: {Index}";
    }

    class OperationComparer(SpirvContext Context, bool UseIndices) : IComparer<InstructionSortHelper>
    {
        private static int RemapOp(Op op)
        {
            // Make sure all OpName and OpMember are contiguous
            return op switch
            {
                Op.OpName or Op.OpMemberName or Op.OpMemberDecorate or Op.OpMemberDecorateString => -1,
                _ => (int)op,
            };
        }

        public int Compare(InstructionSortHelper x, InstructionSortHelper y)
        {
            var comparison = RemapOp(x.Op).CompareTo(RemapOp(y.Op));
            if (comparison != 0)
                return comparison;

            // Special values for searching bounds
            if (UseIndices)
            {
                if (x.Index == -1 || y.Index == int.MaxValue)
                    return -1;
                if (y.Index == -1 || x.Index == int.MaxValue)
                    return 1;
            }

            // Only for OpName and OpMember: we sort by target
            // Note: RemapOp earlier made sure we sort first per Target then OpCode, i.e.:
            // OpName %3 "Test"
            // OpDecorate %3 ....
            // OpName %4 "Test2"
            if (x.Op == Op.OpName || x.Op == Op.OpDecorate || x.Op == Op.OpDecorateString || x.Op == Op.OpMemberName || x.Op == Op.OpMemberDecorate || x.Op == Op.OpMemberDecorateString)
            {
                comparison = (x.Data.Memory.Span[1]).CompareTo(y.Data.Memory.Span[1]);
                if (comparison != 0)
                    return comparison;
            }

            // Only process types that we care about
            // For arrays, we have some additional checks: same name and member info
            if (x.Op == Op.OpTypeArray)
            {
                comparison = x.Data.Memory.Span[2].CompareTo(y.Data.Memory.Span[2]);
                if (comparison != 0)
                    return comparison;

                comparison = CompareIntConstant(Context, x.Data.Memory.Span[3], y.Data.Memory.Span[3]);
                if (comparison != 0)
                    return comparison;
            }
            // Standard ResultType/ResultId instructions: ignore ResultId (Span[2]) and compare the rest
            else if (x.Op == Op.OpSDSLGenericParameter || OpCheckDuplicateForConstant(x.Op))
            {
                comparison = x.Data.Memory.Span[1].CompareTo(y.Data.Memory.Span[1]);
                if (comparison != 0)
                    return comparison;

                comparison = MemoryExtensions.SequenceCompareTo(x.Data.Memory.Span[3..], y.Data.Memory.Span[3..]);
                if (comparison != 0)
                    return comparison;
            }
            else if (OpCheckDuplicateForTypesAndImport(x.Op))
            {
                comparison = MemoryExtensions.SequenceCompareTo(x.Data.Memory.Span[2..], y.Data.Memory.Span[2..]);
                if (comparison != 0)
                    return comparison;
            }
            else if (x.Op == Op.OpName || x.Op == Op.OpDecorate || x.Op == Op.OpDecorateString || x.Op == Op.OpMemberName || x.Op == Op.OpMemberDecorate || x.Op == Op.OpMemberDecorateString)
            {
                // Use actual op (they were all remapped to same ID in RemapOp() to be grouped by TargetId first)
                comparison = x.Op.CompareTo(y.Op);
                if (comparison != 0)
                    return comparison;

                comparison = MemoryExtensions.SequenceCompareTo(x.Data.Memory != null ? x.Data.Memory.Span[2..] : [], y.Data.Memory != null ? y.Data.Memory.Span[2..] : []);
                if (comparison != 0)
                    return comparison;
            }

            comparison = UseIndices ? x.Index.CompareTo(y.Index) : 0;
            return comparison;
        }
    }

    private static int CompareIntConstant(SpirvContext context, int id1, int id2)
    {
        if (id1 == id2)
            return 0;

        var value1Success = context.TryGetConstantValue(id1, out var value1, out _, false);
        var value2Success = context.TryGetConstantValue(id2, out var value2, out _, false);

        return (value1Success, value2Success) switch
        {
            // Both succeeds: compare values
            (true, true) => ((int)value1).CompareTo((int)value2),
            // Only one succeeds (use bool order)
            (true, false) or (false, true) => value1Success.CompareTo(value2Success),
            // Both fails: use ID
            (false, false) => id1.CompareTo(id2),
        };
    }

    private SpirvContext context;
    private List<InstructionSortHelper> instructionsByOp;
    private List<InstructionSortHelper> namesByOp;
    private OperationComparer comparerSort;
    private OperationComparer comparerInsert;
    private bool namesSorted;

    public TypeDuplicateHelper(SpirvContext context)
    {
        this.context = context;
        instructionsByOp = new();
        namesByOp = new();
        namesSorted = false;
        foreach (var i in context)
        {
            GetTargetList(i.Data).Add(new InstructionSortHelper(i.Op, i.Index, i.Data));
        }

        comparerSort = new OperationComparer(context, true);
        namesByOp.Sort(comparerSort);
        instructionsByOp.Sort(comparerSort);

        comparerInsert = new OperationComparer(context, false);
    }

    public OpDataIndex InsertInstruction(int index, OpData data)
    {
        var result = context.Insert(index, data);

        // Adjust indices (optimization: we skip if we added at last index)
        if (index != context.Count - 1)
        {
            var namesByOpSpan = CollectionsMarshal.AsSpan(namesByOp);
            for (int i = 0; i < namesByOp.Count; i++)
            {
                ref var inst = ref namesByOpSpan[i];
                if (inst.Index >= index)
                    inst.Index++;
            }

            var instructionsByOpSpan = CollectionsMarshal.AsSpan(instructionsByOp);
            for (int i = 0; i < instructionsByOp.Count; i++)
            {
                ref var inst = ref instructionsByOpSpan[i];
                if (inst.Index >= index)
                    inst.Index++;
            }
        }

        // Add new item
        var targetList = GetTargetList(data);
        var newItem = new InstructionSortHelper(data.Op, index, data);
        var sortedInsertionIndex = targetList.BinarySearch(newItem, comparerSort);
        // Since comparerSort uses Index as last key, it should never be an exact match
        if (sortedInsertionIndex >= 0)
            throw new InvalidOperationException();
        targetList.Insert(~sortedInsertionIndex, newItem);

        return result;
    }

    public void RemoveInstructionAt(int index, bool dispose)
    {
        context.RemoveAt(index, dispose);

        // Adjust indices and remove at same time
        var namesByOpSpan = CollectionsMarshal.AsSpan(namesByOp);
        for (int i = 0; i < namesByOp.Count; i++)
        {
            ref var inst = ref namesByOpSpan[i];
            if (inst.Index > index)
                inst.Index--;
            else if (inst.Index == index)
                namesByOp.RemoveAt(i--);
        }
        var instructionsByOpSpan = CollectionsMarshal.AsSpan(instructionsByOp);
        for (int i = 0; i < instructionsByOp.Count; i++)
        {
            ref var inst = ref instructionsByOpSpan[i];
            if (inst.Index > index)
                inst.Index--;
            else if (inst.Index == index)
                instructionsByOp.RemoveAt(i--);
        }
    }

    private List<InstructionSortHelper> GetTargetList(OpData data)
    {
        switch (data.Op)
        {
            case Op.OpName or Op.OpMemberName or Op.OpMemberDecorate or Op.OpMemberDecorateString:
                // Target is always in operand 1 for all those instructions
                return namesByOp;
            default:
                return instructionsByOp;
        }
    }

    private List<InstructionSortHelper> GetSortedNames()
    {
        // If any name was added, sort them
        if (!namesSorted)
        {
            namesByOp.Sort(comparerSort);
            namesSorted = true;
        }
        return namesByOp;
    }

    public static bool OpCheckDuplicateForTypesAndImport(Op op)
    {
        return op == Op.OpTypeVoid
            || op == Op.OpTypeInt
            || op == Op.OpTypeFloat
            || op == Op.OpTypeBool
            || op == Op.OpTypeVector
            || op == Op.OpTypeMatrix
            || op == Op.OpTypeArray
            || op == Op.OpTypeRuntimeArray
            || op == Op.OpTypePointer
            || op == Op.OpTypeFunction
            || op == Op.OpTypeFunctionSDSL
            || op == Op.OpTypeImage
            || op == Op.OpTypeSampler
            || op == Op.OpTypeSampledImage
            || op == Op.OpTypeGenericSDSL
            || op == Op.OpTypeStreamsSDSL
            || op == Op.OpSDSLImportShader
            || op == Op.OpSDSLImportVariable
            || op == Op.OpSDSLImportFunction
            || op == Op.OpSDSLImportStruct;
    }

    public static bool OpCheckDuplicateForConstant(Op op)
    {
        return op == Op.OpConstant
            || op == Op.OpConstantTrue
            || op == Op.OpConstantFalse
            || op == Op.OpConstantNull
            || op == Op.OpConstantSampler
            || op == Op.OpConstantComposite
            || op == Op.OpConstantStringSDSL
            || op == Op.OpSpecConstant
            || op == Op.OpSpecConstantComposite
            || op == Op.OpSpecConstantTrue
            || op == Op.OpSpecConstantFalse
            || op == Op.OpSpecConstantOp;
    }

    public bool CheckForDuplicates(OpData data, out OpDataIndex foundData)
    {
        var index = instructionsByOp.BinarySearch(new InstructionSortHelper { Op = data.Op, Index = -1, Data = data }, comparerInsert);

        if (index >= 0)
        {
            foundData = new(instructionsByOp[index].Index, context.GetBuffer());
            return true;
        }

        foundData = default;
        return false;
    }

    public void RemoveDuplicates()
    {
        var buffer = context.GetBuffer();
        
        // Note: We process instruction by types depending on their dependencies
        // i.e. a OpTypeFloat being unified means a OpTypeVector depending on it might too

        // Covers OpTypeVoid, OpTypeBool, OpTypeInt, OpTypeFloat at the same time (no interdependencies)
        ProcessInstructions(buffer, instructionsByOp, Op.OpTypeVoid, Op.OpTypeFloat, false, comparerSort);
        ProcessInstructions(buffer, instructionsByOp, Op.OpTypeVector, Op.OpTypeVector, true, comparerSort);
        ProcessInstructions(buffer, instructionsByOp, Op.OpTypeMatrix, Op.OpTypeMatrix, true, comparerSort);

        ProcessInstructions(buffer, instructionsByOp, Op.OpTypeArray, Op.OpTypeRuntimeArray, true, comparerSort);

        ProcessInstructions(buffer, instructionsByOp, Op.OpTypeStruct, Op.OpTypeStruct, true, comparerSort);

        ProcessInstructions(buffer, instructionsByOp, Op.OpTypePointer, Op.OpTypePointer, true, comparerSort);
        ProcessInstructions(buffer, instructionsByOp, Op.OpTypeFunction, Op.OpTypeFunction, true, comparerSort);

        ProcessInstructions(buffer, instructionsByOp, Op.OpTypeGenericSDSL, Op.OpTypeGenericSDSL, true, comparerSort);

        // Note: due to RemapOp, this will also cover OpMemberDecorate and OpMemberName
        ProcessInstructions(buffer, namesByOp, Op.OpName, Op.OpName, true, comparerSort);
    }

    private static void ProcessInstructions(NewSpirvBuffer buffer, List<InstructionSortHelper> instructionsByOp, Op startOp, Op endOp, bool sort, OperationComparer comparer)
    {
        var start = ~instructionsByOp.BinarySearch(new InstructionSortHelper { Op = startOp, Index = -1 }, comparer);
        var end = ~instructionsByOp.BinarySearch(new InstructionSortHelper { Op = endOp, Index = int.MaxValue }, comparer);

        if (sort)
        {
            // Sort again, but only those instructions (as previous replacements with ReplaceRefs might have changed order)
            instructionsByOp.Sort(start, end - start, comparer);
        }

        ProcessSortedInstructions(buffer, instructionsByOp, start, end, comparer);
    }

    private static void ProcessSortedInstructions(NewSpirvBuffer buffer, List<InstructionSortHelper> instructionsByOp, int start, int end, OperationComparer comparer)
    {
        for (var firstIndex = start; firstIndex < end; )
        {
            var i = buffer[instructionsByOp[firstIndex].Index];

            // Find first item that is different
            int lastIndex;
            for (lastIndex = firstIndex + 1; lastIndex < end; ++lastIndex)
            {
                var j = instructionsByOp[lastIndex];
                var firstMemoryIndex = i.Op == Op.OpName ? 1 : 2;
                if (!(i.Op == j.Op && MemoryExtensions.SequenceEqual(i.Data.Memory.Span[firstMemoryIndex..], j.Data.Memory.Span[firstMemoryIndex..])))
                    break;
            }

            // At least 2 similar items?
            if (lastIndex - firstIndex > 1)
            {
                bool isOpWithResultId = i.Op == Op.OpName || i.Op == Op.OpMemberName || i.Op == Op.OpMemberDecorate || i.Op == Op.OpMemberDecorateString;

                // Build list of IdResult matching first instruction
                Span<int> matchingRefs = new int[lastIndex - (firstIndex + 1)];
                for (var index = firstIndex + 1; index < lastIndex; ++index)
                {
                    var j = buffer[instructionsByOp[index].Index].Data;
                    if (!isOpWithResultId)
                        matchingRefs[index - (firstIndex + 1)] = j.IdResult ?? throw new InvalidOperationException();
                    SetOpNop(j.Memory.Span);
                }

                // Replace all IdResult at once to the one of first instruction
                if (!isOpWithResultId)
                    ReplaceRefs(matchingRefs, i.Data.IdResult ?? throw new InvalidOperationException(), buffer);
            }

            // Restart from last different instruction
            firstIndex = lastIndex;
        }
    }

    static void ReplaceRefs(Span<int> from, int to, NewSpirvBuffer buffer)
    {
        foreach (var i in buffer)
        {
            var opcode = i.Op;
            foreach (var op in i.Data)
            {
                if (op.Kind == OperandKind.IdRef || op.Kind == OperandKind.IdScope || op.Kind == OperandKind.IdMemorySemantics)
                {
                    foreach (ref var w in op.Words)
                    {
                        if (from.Contains(w))
                            w = to;
                    }
                }
                else if (op.Kind == OperandKind.IdResultType && from.Contains(op.Words[0]))
                    op.Words[0] = to;
                else if (op.Kind == OperandKind.PairIdRefLiteralInteger && from.Contains(op.Words[0]))
                    op.Words[0] = to;
                else if (op.Kind == OperandKind.PairLiteralIntegerIdRef && from.Contains(op.Words[1]))
                    op.Words[1] = to;
                else if (op.Kind == OperandKind.PairIdRefIdRef)
                {
                    op.Words[0] = from.Contains(op.Words[0]) ? to : op.Words[0];
                    op.Words[1] = from.Contains(op.Words[1]) ? to : op.Words[1];
                }
            }
        }
    }

    static void SetOpNop(Span<int> words)
    {
        words[0] = words.Length << 16;
        words[1..].Clear();
    }
}
