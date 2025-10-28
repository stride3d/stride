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
    public readonly void Apply(NewSpirvBuffer buffer)
    {
        for (var index = 0; index < buffer.Count; index++)
        {
            var i = buffer[index].Data;
            if (i.Op == Op.OpTypeVoid || i.Op == Op.OpTypeInt || i.Op == Op.OpTypeFloat)
            {
                for (var index2 = index + 1; index2 < buffer.Count; index2++)
                {
                    var j = buffer[index2].Data;
                    if (
                        (j.Op == Op.OpTypeVoid || j.Op == Op.OpTypeInt ||
                         j.Op == Op.OpTypeFloat)
                        && i.IdResult != j.IdResult
                        && MemoryExtensions.SequenceEqual(i.Memory.Span[2..], j.Memory.Span[2..])
                    )
                    {
                        ReplaceRefs(j.IdResult ?? -1, i.IdResult ?? -1, buffer);
                        SetOpNop(j.Memory.Span);
                    }
                }
            }
        }

        for (var index = 0; index < buffer.Count; index++)
        {
            var i = buffer[index].Data;
            if (i.Op == Op.OpTypeVector)
            {
                for (var index2 = index + 1; index2 < buffer.Count; index2++)
                {
                    var j = buffer[index2].Data;
                    if (
                        j.Op == Op.OpTypeVector
                        && i.IdResult != j.IdResult
                        && MemoryExtensions.SequenceEqual(i.Memory.Span[2..], j.Memory.Span[2..])
                        )
                    {
                        ReplaceRefs(j.IdResult ?? -1, i.IdResult ?? -1, buffer);
                        SetOpNop(j.Memory.Span);
                    }
                }
            }
        }
        for (var index = 0; index < buffer.Count; index++)
        {
            var i = buffer[index].Data;
            if (i.Op == Op.OpTypeMatrix)
            {
                for (var index2 = index + 1; index2 < buffer.Count; index2++)
                {
                    var j = buffer[index2].Data;
                    if (
                        j.Op == Op.OpTypeMatrix
                        && i.IdResult != j.IdResult
                        && MemoryExtensions.SequenceEqual(i.Memory.Span[2..], j.Memory.Span[2..])
                        )
                    {
                        ReplaceRefs(j.IdResult ?? -1, i.IdResult ?? -1, buffer);
                        SetOpNop(j.Memory.Span);
                    }
                }
            }
        }
        for (var index = 0; index < buffer.Count; index++)
        {
            var i = buffer[index].Data;
            if (i.Op == Op.OpTypePointer)
            {
                for (var index2 = index + 1; index2 < buffer.Count; index2++)
                {
                    var j = buffer[index2].Data;
                    if (
                        j.Op == Op.OpTypePointer
                        && i.IdResult != j.IdResult
                        && MemoryExtensions.SequenceEqual(i.Memory.Span[2..], j.Memory.Span[2..])
                    )
                    {
                        ReplaceRefs(j.IdResult ?? -1, i.IdResult ?? -1, buffer);
                        SetOpNop(j.Memory.Span);
                    }
                }
            }
        }
        for (var index = 0; index < buffer.Count; index++)
        {
            var i = buffer[index].Data;
            if (i.Op == Op.OpTypeFunction)
            {
                for (var index2 = index + 1; index2 < buffer.Count; index2++)
                {
                    var j = buffer[index2].Data;
                    if (
                        j.Op == Op.OpTypeFunction
                        && i.IdResult != j.IdResult
                        && MemoryExtensions.SequenceEqual(i.Memory.Span[2..], j.Memory.Span[2..])
                    )
                    {
                        ReplaceRefs(j.IdResult ?? -1, i.IdResult ?? -1, buffer);
                        SetOpNop(j.Memory.Span);
                    }
                }
            }
        }
        for (var index = 0; index < buffer.Count; index++)
        {
            var i = buffer[index].Data;
            if (i.Op == Op.OpName)
            {
                for (var index2 = index + 1; index2 < buffer.Count; index2++)
                {
                    var j = buffer[index2].Data;
                    if (
                        j.Op == Op.OpName
                        && i.Memory.Span[1] == j.Memory.Span[1]
                        && MemoryExtensions.SequenceEqual(i.Memory.Span[2..], j.Memory.Span[2..])
                    )
                    {
                        ReplaceRefs(j.IdResult ?? -1, i.IdResult ?? -1, buffer);
                        SetOpNop(j.Memory.Span);
                    }
                }
            }
        }
    }

    static void ReplaceRefs(int from, int to, NewSpirvBuffer buffer)
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
                        if (w == from)
                            w = to;
                    }
                }
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
