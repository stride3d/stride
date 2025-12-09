using CommunityToolkit.HighPerformance.Buffers;
using Stride.Shaders.Spirv.Core;
using Stride.Shaders.Spirv.Core.Buffers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Stride.Shaders.Spirv.Specification;

namespace Stride.Shaders.Spirv.Processing;




/// <summary>
/// Remove duplicate simple types.
/// Should be applied after the IdRefOffsetter.
/// </summary>
public struct TypeDuplicateRemover : INanoPass
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

        // If it's a fake instruction for OpName/OpMember, we can also use Target instead of Memory.Span[1] 
        public int? TargetOverride { get; init; }

        public override string ToString() => Data.Memory != null ? Data.ToString() : $"{Op} Target:{TargetOverride}";
    }

    class OperationComparer(List<InstructionSortHelper> NameInstructions) : IComparer<InstructionSortHelper>
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
            if (x.Index == -1 || y.Index == int.MaxValue)
                return -1;
            if (y.Index == -1 || x.Index == int.MaxValue)
                return 1;

            // Only for OpName and OpMember
            if (x.Op == Op.OpName || x.Op == Op.OpMemberName || x.Op == Op.OpMemberDecorate || x.Op == Op.OpMemberDecorateString)
            {
                // Use TargetOverride if defined, otherwise Memory.Span[1] (where target would be stored)
                comparison = (x.TargetOverride ?? x.Data.Memory.Span[1]).CompareTo(y.TargetOverride ?? y.Data.Memory.Span[1]);
                if (comparison != 0)
                    return comparison;
            }

            // Only process types that we care about
            if (x.Op == Op.OpTypeVoid || x.Op == Op.OpTypeInt || x.Op == Op.OpTypeFloat || x.Op == Op.OpTypeBool
                || x.Op == Op.OpTypeVector || x.Op == Op.OpTypeMatrix || x.Op == Op.OpTypePointer || x.Op == Op.OpTypeFunction
                || x.Op == Op.OpTypeArray || x.Op == Op.OpTypeRuntimeArray
                || x.Op == Op.OpTypeStruct
                || x.Op == Op.OpSDSLImportShader || x.Op == Op.OpSDSLImportFunction || x.Op == Op.OpSDSLImportVariable || x.Op == Op.OpSDSLImportStruct)
            {
                comparison = MemoryExtensions.SequenceCompareTo(x.Data.Memory.Span[2..], y.Data.Memory.Span[2..]);
                if (comparison != 0)
                    return comparison;

                // For struct, we have some additional checks: same name and member info
                if (x.Op == Op.OpTypeStruct)
                {
                    comparison = CompareStructMetadata(x, y);
                    if (comparison != 0)
                        return comparison;
                }
            }
            else if (x.Op == Op.OpName || x.Op == Op.OpMemberName || x.Op == Op.OpMemberDecorate || x.Op == Op.OpMemberDecorateString)
            {
                // Use actual op (they were all remapped to same ID in RemapOp() to be grouped by TargetId first)
                comparison = x.Op.CompareTo(y.Op);
                if (comparison != 0)
                    return comparison;

                comparison = MemoryExtensions.SequenceCompareTo(x.Data.Memory != null ? x.Data.Memory.Span[2..] : [], y.Data.Memory != null ? y.Data.Memory.Span[2..] : []);
                if (comparison != 0)
                    return comparison;
            }

            comparison = x.Index.CompareTo(y.Index);
            return comparison;
        }

        public int CompareStructMetadata(InstructionSortHelper x, InstructionSortHelper y)
        {
            // Note: With RemapOp(), this will also find OpMember instructions
            var target1 = x.Data.Memory.Span[1];
            var namesStart1 = ~NameInstructions.BinarySearch(new InstructionSortHelper { Op = Op.OpName, TargetOverride = target1 }, this);
            var namesEnd1 = ~NameInstructions.BinarySearch(new InstructionSortHelper { Op = Op.OpName, TargetOverride = target1 + 1 }, this);

            var target2 = y.Data.Memory.Span[1];
            var namesStart2 = ~NameInstructions.BinarySearch(new InstructionSortHelper { Op = Op.OpName, TargetOverride = target2 }, this);
            var namesEnd2 = ~NameInstructions.BinarySearch(new InstructionSortHelper { Op = Op.OpName, TargetOverride = target2 + 1 }, this);

            // Compare sequences (they should be the same)
            for (int i = 0; i < Math.Max(namesEnd1 - namesStart1, namesEnd2 - namesStart2); ++i)
            {
                // If one sequence is longer than the other, define an ordering
                if (i >= namesEnd1 - namesStart1)
                    return -1;
                if (i >= namesEnd2 - namesStart2)
                    return 1;

                var comparison = Compare(NameInstructions[namesStart1 + i], NameInstructions[namesStart2 + i]);
                if (comparison != 0)
                    return comparison;
            }

            return 0;
        }
    }

    public readonly void Apply(NewSpirvBuffer buffer)
    {
        var instructionsByOp = new List<InstructionSortHelper>();
        var namesByOp = new List<InstructionSortHelper>();
        foreach (var i in buffer)
        {
            switch (i.Op)
            {
                case Op.OpName or Op.OpMemberName or Op.OpMemberDecorate or Op.OpMemberDecorateString:
                    // Target is always in operand 1 for all those instructions
                    namesByOp.Add(new InstructionSortHelper(i.Op, i.Index, i.Data));
                    break;
                default:
                    instructionsByOp.Add(new InstructionSortHelper(i.Op, i.Index, i.Data));
                    break;
            }
        }

        var comparer = new OperationComparer(namesByOp);
        // Note: since it contains no OpTypeStruct, it should not access OperationComparer.NameInstructions
        namesByOp.Sort(comparer);
        instructionsByOp.Sort(comparer);

        // Note: We process instruction by types depending on their dependencies
        // i.e. a OpTypeFloat being unified means a OpTypeVector depending on it might too

        // Covers OpTypeVoid, OpTypeBool, OpTypeInt, OpTypeFloat at the same time (no interdependencies)
        ProcessInstructions(buffer, instructionsByOp, Op.OpTypeVoid, Op.OpTypeFloat, false, comparer);
        // Covers OpTypeVector, OpTypeMatrix at the same time
        ProcessInstructions(buffer, instructionsByOp, Op.OpTypeVector, Op.OpTypeMatrix, true, comparer);

        ProcessInstructions(buffer, instructionsByOp, Op.OpTypeArray, Op.OpTypeRuntimeArray, true, comparer);

        ProcessInstructions(buffer, instructionsByOp, Op.OpTypeStruct, Op.OpTypeStruct, true, comparer);

        ProcessInstructions(buffer, instructionsByOp, Op.OpTypePointer, Op.OpTypePointer, true, comparer);
        ProcessInstructions(buffer, instructionsByOp, Op.OpTypeFunction, Op.OpTypeFunction, true, comparer);

        ProcessInstructions(buffer, instructionsByOp, Op.OpSDSLImportShader, Op.OpSDSLImportShader, true, comparer);
        ProcessInstructions(buffer, instructionsByOp, Op.OpSDSLImportStruct, Op.OpSDSLImportStruct, true, comparer);
        // Covers OpSDSLImportFunction and OpSDSLImportVariable at the same time
        ProcessInstructions(buffer, instructionsByOp, Op.OpSDSLImportFunction, Op.OpSDSLImportVariable, true, comparer);

        // Note: due to RemapOp, this will also cover OpMemberDecorate and OpMemberName
        ProcessInstructions(buffer, namesByOp, Op.OpName, Op.OpName, true, comparer);
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

                if (i.Op == Op.OpTypeStruct)
                {
                    if (comparer.CompareStructMetadata(new InstructionSortHelper(i), j) != 0)
                        break;
                }
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
                if (op.Kind == OperandKind.IdRef)
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
