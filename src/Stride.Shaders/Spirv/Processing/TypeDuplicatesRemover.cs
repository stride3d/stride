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
/// Should be applied before the IdRefOffsetter.
/// </summary>
public struct TypeDuplicateRemover : INanoPass
{

    public readonly void Apply(SpirvBuffer buffer)
    {
        for (var index = 0; index < buffer.Instructions.Count; index++)
        {
            var i = buffer.Instructions[index];
            if (i.OpCode == Op.OpTypeVoid || i.OpCode == Op.OpTypeInt || i.OpCode == Op.OpTypeFloat)
            {
                for (var index2 = index + 1; index2 < buffer.Instructions.Count; index2++)
                {
                    var j = buffer.Instructions[index2];
                    if (
                        (j.OpCode == Op.OpTypeVoid || j.OpCode == Op.OpTypeInt ||
                         j.OpCode == Op.OpTypeFloat)
                        && i.ResultId != j.ResultId
                        && MemoryExtensions.SequenceEqual(i.Operands[1..], j.Operands[1..])
                    )
                    {
                        ReplaceRefs(j.ResultId ?? -1, i.ResultId ?? -1, buffer);
                        SetOpNop(j.Words);
                    }
                }
            }
        }

        for (var index = 0; index < buffer.Instructions.Count; index++)
        {
            var i = buffer.Instructions[index];
            if (i.OpCode == Op.OpTypeVector)
            {
                for (var index2 = index + 1; index2 < buffer.Instructions.Count; index2++)
                {
                    var j = buffer.Instructions[index2];
                    if (
                        j.OpCode == Op.OpTypeVector
                        && i.ResultId != j.ResultId
                        && MemoryExtensions.SequenceEqual(i.Operands[1..], j.Operands[1..])
                        )
                    {
                        ReplaceRefs(j.ResultId ?? -1, i.ResultId ?? -1, buffer);
                        SetOpNop(j.Words);
                    }
                }
            }
        }
        for (var index = 0; index < buffer.Instructions.Count; index++)
        {
            var i = buffer.Instructions[index];
            if (i.OpCode == Op.OpTypeMatrix)
            {
                for (var index2 = index + 1; index2 < buffer.Instructions.Count; index2++)
                {
                    var j = buffer.Instructions[index2];
                    if (
                        j.OpCode == Op.OpTypeMatrix
                        && i.ResultId != j.ResultId
                        && MemoryExtensions.SequenceEqual(i.Operands[1..], j.Operands[1..])
                        )
                    {
                        ReplaceRefs(j.ResultId ?? -1, i.ResultId ?? -1, buffer);
                        SetOpNop(j.Words);
                    }
                }
            }
        }
        for (var index = 0; index < buffer.Instructions.Count; index++)
        {
            var i = buffer.Instructions[index];
            if (i.OpCode == Op.OpTypePointer)
            {
                for (var index2 = index + 1; index2 < buffer.Instructions.Count; index2++)
                {
                    var j = buffer.Instructions[index2];
                    if (
                        j.OpCode == Op.OpTypePointer
                        && i.ResultId != j.ResultId
                        && MemoryExtensions.SequenceEqual(i.Operands[1..], j.Operands[1..])
                    )
                    {
                        ReplaceRefs(j.ResultId ?? -1, i.ResultId ?? -1, buffer);
                        SetOpNop(j.Words);
                    }
                }
            }
        }
        for (var index = 0; index < buffer.Instructions.Count; index++)
        {
            var i = buffer.Instructions[index];
            if (i.OpCode == Op.OpName)
            {
                for (var index2 = index + 1; index2 < buffer.Instructions.Count; index2++)
                {
                    var j = buffer.Instructions[index2];
                    if (
                        j.OpCode == Op.OpName
                        && i.Operands[0] == j.Operands[0]
                        && MemoryExtensions.SequenceEqual(i.Operands[1..], j.Operands[1..])
                    )
                    {
                        ReplaceRefs(j.ResultId ?? -1, i.ResultId ?? -1, buffer);
                        SetOpNop(j.Words);
                    }
                }
            }
        }
    }

    static void ReplaceRefs(int from, int to, SpirvBuffer buffer)
    {
        foreach (var i in buffer.Instructions)
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
    }

    static void SetOpNop(Span<int> words)
    {
        words[0] = words.Length << 16;
        words[1..].Clear();
    }
}
