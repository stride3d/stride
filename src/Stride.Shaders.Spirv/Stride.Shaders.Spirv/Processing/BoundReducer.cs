using Stride.Shaders.Spirv.Core;
using Stride.Shaders.Spirv.Core.Buffers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Stride.Shaders.Spirv.Processing;



/// <summary>
/// Makes sure indices used in spirv module are all continuous.
/// </summary>
public struct BoundReducer : INanoPass
{
    public BoundReducer() { }

    public void Apply(MultiBuffer buffer)
    {
        // First step is to find the next idResult
        // If it's previous + 1 then it's okay, previous is now updated
        // If it's above previous + 1, then it's not okay and we switch

        var finished = false;
        var previousId = 0;
        var next = Instruction.Empty;
        var countIds = 0;
        
        foreach (var i in buffer.Instructions)
            countIds += i.ResultId != null ? 1 : 0;
        while (!finished && previousId < countIds)
        {
            var countAbove = 0;
            foreach(var i in buffer.Instructions)
            {
                if(i.ResultId == previousId + 1)
                {
                    countAbove += 1;
                    previousId += 1;
                    next = i;
                    break;
                }
                else if (next.IsEmpty && i.ResultId > previousId + 1)
                {
                    countAbove += 1;
                    next = i;
                }
                else if(!next.IsEmpty && i.ResultId > previousId + 1 &&  i.ResultId < next.ResultId)
                {
                    countAbove += 1;
                    next = i;
                }
            }
            if (countAbove == 0)
                finished = true;
            else if(next.ResultId > previousId + 1)
            {
                next.AsRef().SetResultId(previousId + 1);
                ReplaceRefs(next.ResultId ?? -1, previousId + 1, buffer);
            }
        }


        buffer.RecomputeBound();
    }
    static void ReplaceRefs(int from, int to, MultiBuffer buffer)
    {
        foreach (var i in buffer.Declarations.UnorderedInstructions)
        {
            foreach (var op in i)
            {
                if (op.Kind == OperandKind.IdRef || op.Kind == OperandKind.IdResultType)
                    op.Words[0] = op.Words[0] == from ? to : op.Words[0];
                else if (op.Kind == OperandKind.PairIdRefLiteralInteger)
                    op.Words[0] = op.Words[0] == from ? to : op.Words[0];
                else if (op.Kind == OperandKind.PairLiteralIntegerIdRef)
                    op.Words[1] = op.Words[1] == from ? to : op.Words[1];
                else if (op.Kind == OperandKind.PairIdRefIdRef)
                {
                    op.Words[0] = op.Words[0] == from ? to : op.Words[0];
                    op.Words[1] = op.Words[1] == from ? to : op.Words[1];
                }
            }
        }
        foreach (var (_, f) in buffer.Functions)
            foreach (var i in f.UnorderedInstructions)
            {
                foreach (var op in i)
                {
                    if (op.Kind == OperandKind.IdRef || op.Kind == OperandKind.IdResultType)
                        op.Words[0] = op.Words[0] == from ? to : op.Words[0];
                    else if (op.Kind == OperandKind.PairIdRefLiteralInteger)
                        op.Words[0] = op.Words[0] == from ? to : op.Words[0];
                    else if (op.Kind == OperandKind.PairLiteralIntegerIdRef)
                        op.Words[1] = op.Words[1] == from ? to : op.Words[1];
                    else if (op.Kind == OperandKind.PairIdRefIdRef)
                    {
                        op.Words[0] = op.Words[0] == from ? to : op.Words[0];
                        op.Words[1] = op.Words[1] == from ? to : op.Words[1];
                    }
                }
            }
    }
    static void ReplaceRefs(int from, int to, WordBuffer func)
    {
        foreach (var i in func.UnorderedInstructions)
        {
            foreach (var op in i)
            {
                if (op.Kind == OperandKind.IdRef || op.Kind == OperandKind.IdResultType)
                    op.Words[0] = op.Words[0] == from ? to : op.Words[0];
                else if (op.Kind == OperandKind.PairIdRefLiteralInteger)
                    op.Words[0] = op.Words[0] == from ? to : op.Words[0];
                else if (op.Kind == OperandKind.PairLiteralIntegerIdRef)
                    op.Words[1] = op.Words[1] == from ? to : op.Words[1];
                else if (op.Kind == OperandKind.PairIdRefIdRef)
                {
                    op.Words[0] = op.Words[0] == from ? to : op.Words[0];
                    op.Words[1] = op.Words[1] == from ? to : op.Words[1];
                }
            }
        }
    }
}
