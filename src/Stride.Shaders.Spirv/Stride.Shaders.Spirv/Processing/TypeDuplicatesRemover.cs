using Stride.Shaders.Spirv.Core;
using Stride.Shaders.Spirv.Core.Buffers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stride.Shaders.Spirv.Processing;




/// <summary>
/// Remove duplicate simple types.
/// Should be applied before the IdRefOffsetter.
/// </summary>
public struct TypeDuplicateRemover : INanoPass
{

    public readonly void Apply(MultiBuffer buffer)
    {
        foreach (var i in buffer.Declarations.UnorderedInstructions)
        {
            if (i.OpCode == SDSLOp.OpTypeInt || i.OpCode == SDSLOp.OpTypeFloat)
            {
                foreach (var j in buffer.Declarations.UnorderedInstructions)
                {
                    if (
                        (j.OpCode == SDSLOp.OpTypeInt || j.OpCode == SDSLOp.OpTypeFloat)
                        && i.ResultId != j.ResultId
                        && MemoryExtensions.SequenceEqual(i.Operands.Span[1..], j.Operands.Span[1..])
                        )
                    {
                        ReplaceRefs(j.ResultId ?? -1, i.ResultId ?? -1, buffer);
                        SetOpNop(j.Words.Span);
                    }
                }
            }
        }
        foreach (var i in buffer.Declarations.UnorderedInstructions)
        {
            if (i.OpCode == SDSLOp.OpTypeVector)
            {
                foreach (var j in buffer.Declarations.UnorderedInstructions)
                {
                    if (
                        j.OpCode == SDSLOp.OpTypeVector
                        && i.ResultId != j.ResultId
                        && MemoryExtensions.SequenceEqual(i.Operands.Span[1..], j.Operands.Span[1..])
                        )
                    {
                        ReplaceRefs(j.ResultId ?? -1, i.ResultId ?? -1, buffer);
                        SetOpNop(j.Words.Span);
                    }
                }
            }
        }
        foreach (var i in buffer.Declarations.UnorderedInstructions)
        {
            if (i.OpCode == SDSLOp.OpTypeMatrix)
            {
                foreach (var j in buffer.Declarations.UnorderedInstructions)
                {
                    if (
                        j.OpCode == SDSLOp.OpTypeMatrix
                        && i.ResultId != j.ResultId
                        && MemoryExtensions.SequenceEqual(i.Operands.Span[1..], j.Operands.Span[1..])
                        )
                    {
                        ReplaceRefs(j.ResultId ?? -1, i.ResultId ?? -1, buffer);
                        SetOpNop(j.Words.Span);
                    }
                }
            }
        }
        //var idx1 = 0;
        //// First base types
        //foreach (var i in buffer.Declarations.UnorderedInstructions)
        //{
        //    if (i.OpCode == SDSLOp.OpTypeInt || i.OpCode == SDSLOp.OpTypeFloat)
        //    {
        //        var idx2 = 0;
        //        foreach (var j in buffer.Declarations)
        //        {
        //            if (j.OpCode == i.OpCode && idx1 != idx2 && i.Operands.Span[1..].SequenceEqual(j.Operands.Span[1..]))
        //            {
        //                ReplaceRefs(j.ResultId ?? -1, i.ResultId ?? -1, buffer);
        //                SetOpNop(j.Words.Span);
        //            }
        //            idx2 += 1;
        //        }
        //    }
        //    else if (i.OpCode == SDSLOp.OpTypeVoid || i.OpCode == SDSLOp.OpTypeBool)
        //    {
        //        var idx2 = 0;
        //        foreach (var j in buffer.Declarations)
        //        {
        //            if (j.OpCode == i.OpCode && idx1 != idx2)
        //            {
        //                ReplaceRefs(j.ResultId ?? -1, i.ResultId ?? -1, buffer);
        //                SetOpNop(j.Words.Span);
        //            }
        //            idx2 += 1;
        //        }
        //    }
        //    idx1 += 1;
        //}
        //idx1 = 0;
        //// Then vectors
        //foreach (var i in buffer.Declarations.UnorderedInstructions)
        //{
        //    if (i.OpCode == SDSLOp.OpTypeVector)
        //    {
        //        var idx2 = 0;
        //        foreach (var j in buffer.Declarations)
        //        {
        //            if (j.OpCode == i.OpCode && idx1 != idx2 && i.Operands.Span[1..].SequenceEqual(j.Operands.Span[1..]))
        //            {
        //                ReplaceRefs(j.ResultId ?? -1, i.ResultId ?? -1, buffer);
        //                SetOpNop(j.Words.Span);
        //            }
        //            idx2 += 1;
        //        }
        //    }
        //    idx1 += 1;
        //}
        //idx1 = 0;

        //// Then matrices
        //foreach (var i in buffer.Declarations.UnorderedInstructions)
        //{
        //    if (i.OpCode == SDSLOp.OpTypeMatrix)
        //    {
        //        var idx2 = 0;
        //        foreach (var j in buffer.Declarations)
        //        {
        //            if (j.OpCode == i.OpCode && idx1 != idx2 && i.Operands.Span[1..].SequenceEqual(j.Operands.Span[1..]))
        //            {
        //                ReplaceRefs(j.ResultId ?? -1, i.ResultId ?? -1, buffer);
        //                SetOpNop(j.Words.Span);
        //            }
        //            idx2 += 1;
        //        }
        //    }
        //    idx1 += 1;
        //}

    }

    static void ReplaceRefs(int from, int to, MultiBuffer buffer)
    {
        foreach (var i in buffer.Declarations.UnorderedInstructions)
        {
            var opcode = i.OpCode;
            foreach (var op in i)
            {
                if (op.Kind == OperandKind.IdRef && op.Words[0] == from)
                    op.Words[0] = to;
                else if (op.Kind == OperandKind.IdResultType && op.Words[0] == from)
                    op.Words[0] = to;
                else if (op.Kind == OperandKind.PairIdRefLiteralInteger && op.Words[0] == from)
                    op.Words[0] = op.Words[0] == from ? to : op.Words[0];
                else if (op.Kind == OperandKind.PairLiteralIntegerIdRef && op.Words[1] == from)
                    op.Words[1] = op.Words[1] == from ? to : op.Words[1];
                else if (op.Kind == OperandKind.PairIdRefIdRef)
                {
                    if (op.Words[0] == from || op.Words[1] == from)
                    {
                        op.Words[0] = op.Words[0] == from ? to : op.Words[0];
                        op.Words[1] = op.Words[1] == from ? to : op.Words[1];
                    }
                }
            }
        }
        foreach (var (_, f) in buffer.Functions)
            foreach (var i in f.UnorderedInstructions)
            {
                foreach (var op in i)
                {
                    if (op.Kind == OperandKind.IdRef && op.Words[0] == from)
                        op.Words[0] = to;
                    else if (op.Kind == OperandKind.IdResultType && op.Words[0] == from)
                        op.Words[0] = to;
                    else if (op.Kind == OperandKind.PairIdRefLiteralInteger && op.Words[0] == from)
                        op.Words[0] = op.Words[0] == from ? to : op.Words[0];
                    else if (op.Kind == OperandKind.PairLiteralIntegerIdRef && op.Words[1] == from)
                        op.Words[1] = op.Words[1] == from ? to : op.Words[1];
                    else if (op.Kind == OperandKind.PairIdRefIdRef)
                    {
                        if (op.Words[0] == from || op.Words[1] == from)
                        {
                            op.Words[0] = op.Words[0] == from ? to : op.Words[0];
                            op.Words[1] = op.Words[1] == from ? to : op.Words[1];
                        }
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
