using CommunityToolkit.HighPerformance.Buffers;
using Stride.Shaders.Spirv.Core;
using Stride.Shaders.Spirv.Core.Buffers;
using System;
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

    record struct InstructionSortHelper(Op Op, int Index, MemoryOwner<int> Memory) : IComparable<InstructionSortHelper>
    {
        public int CompareTo(InstructionSortHelper other) => CompareOperations(this, other);

        private static int CompareOperations(InstructionSortHelper x, InstructionSortHelper y)
        {
            var comparison = x.Op.CompareTo(y.Op);
            if (comparison != 0)
                return comparison;

            // Special values for searching bounds
            if (x.Index == -1 || y.Index == int.MaxValue)
                return -1;
            if (y.Index == -1 || x.Index == int.MaxValue)
                return 1;

            // Only process types that we care about
            if (x.Op == Op.OpTypeVoid || x.Op == Op.OpTypeInt || x.Op == Op.OpTypeFloat || x.Op == Op.OpTypeBool
                || x.Op == Op.OpTypeVector || x.Op == Op.OpTypeMatrix || x.Op == Op.OpTypePointer || x.Op == Op.OpTypeFunction)
            {
                comparison = MemoryExtensions.SequenceCompareTo(x.Memory.Span[2..], y.Memory.Span[2..]);
                if (comparison != 0)
                    return comparison;
            }
            else if (x.Op == Op.OpName)
            {
                comparison = MemoryExtensions.SequenceCompareTo(x.Memory.Span[1..], y.Memory.Span[1..]);
                if (comparison != 0)
                    return comparison;
            }

            comparison = x.Index.CompareTo(y.Index);
            return comparison;
        }
    }

    public readonly void Apply(NewSpirvBuffer buffer)
    {
        var instructionsByOp = new List<InstructionSortHelper>();
        foreach (var i in buffer)
            instructionsByOp.Add(new InstructionSortHelper(i.Op, i.Index, i.Data.Memory));
        instructionsByOp.Sort();

        // Note: We process instruction by types depending on their dependencies
        // i.e. a OpTypeFloat being unified means a OpTypeVector depending on it might too

        // Covers OpTypeVoid, OpTypeBool, OpTypeInt, OpTypeFloat at the same time (no interdependencies)
        ProcessInstructions(buffer, instructionsByOp, Op.OpTypeVoid, Op.OpTypeFloat, false);
        // Covers OpTypeVector, OpTypeMatrix at the same time
        ProcessInstructions(buffer, instructionsByOp, Op.OpTypeVector, Op.OpTypeMatrix, true);

        ProcessInstructions(buffer, instructionsByOp, Op.OpTypePointer, Op.OpTypePointer, true);
        ProcessInstructions(buffer, instructionsByOp, Op.OpTypeFunction, Op.OpTypeFunction, true);

        ProcessInstructions(buffer, instructionsByOp, Op.OpSDSLImportShader, Op.OpSDSLImportShader, true);
        // Covers OpSDSLImportFunction and OpSDSLImportVariable at the same time
        ProcessInstructions(buffer, instructionsByOp, Op.OpSDSLImportFunction, Op.OpSDSLImportVariable, true);

        ProcessInstructions(buffer, instructionsByOp, Op.OpName, Op.OpName, true);
    }

    private static void ProcessInstructions(NewSpirvBuffer buffer, List<InstructionSortHelper> instructionsByOp, Op startOp, Op endOp, bool sort)
    {
        var start = ~instructionsByOp.BinarySearch(new InstructionSortHelper { Op = startOp, Index = -1 });
        var end = ~instructionsByOp.BinarySearch(new InstructionSortHelper { Op = endOp, Index = int.MaxValue });

        if (sort)
        {
            // Sort again, but only those instructions (as previous replacements with ReplaceRefs might have changed order)
            instructionsByOp.Sort(start, end - start, Comparer<InstructionSortHelper>.Default);
        }

        ProcessSortedInstructions(buffer, instructionsByOp, start, end);
    }

    private static void ProcessSortedInstructions(NewSpirvBuffer buffer, List<InstructionSortHelper> instructionsByOp, int start, int end)
    {
        for (var firstIndex = start; firstIndex < end; )
        {
            var i = buffer[instructionsByOp[firstIndex].Index].Data;

            // Find first item that is different
            int lastIndex;
            for (lastIndex = firstIndex + 1; lastIndex < end; ++lastIndex)
            {
                var j = instructionsByOp[lastIndex];
                var firstMemoryIndex = i.Op == Op.OpName ? 1 : 2;
                if (!(i.Op == j.Op && MemoryExtensions.SequenceEqual(i.Memory.Span[firstMemoryIndex..], j.Memory.Span[firstMemoryIndex..])))
                    break;
            }

            // At least 2 similar items?
            if (lastIndex - firstIndex > 1)
            {
                // Build list of IdResult matching first instruction
                Span<int> matchingRefs = new int[lastIndex - (firstIndex + 1)];
                for (var index = firstIndex + 1; index < lastIndex; ++index)
                {
                    var j = buffer[instructionsByOp[index].Index].Data;
                    if (i.Op != Op.OpName)
                        matchingRefs[index - (firstIndex + 1)] = j.IdResult ?? throw new InvalidOperationException();
                    SetOpNop(j.Memory.Span);
                }

                // Replace all IdResult at once to the one of first instruction
                if (i.Op != Op.OpName)
                    ReplaceRefs(matchingRefs, i.IdResult ?? throw new InvalidOperationException(), buffer);
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
