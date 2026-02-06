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
public struct BoundReducer() : INanoPass
{

    public readonly void Apply(NewSpirvBuffer buffer)
    {
        // First step is to find the next idResult
        // If it's previous + 1 then it's okay, previous is now updated
        // If it's above previous + 1, then it's not okay and we switch

        var finished = false;
        var previousId = 0;
        OpData? next = null!;
        var countIds = 0;
        
        foreach (var i in buffer)
            countIds += i.Data.IdResult != null ? 1 : 0;
        while (!finished && previousId < countIds)
        {
            var countAbove = 0;
            foreach(var i in buffer)
            {
                if(i.Data.IdResult == previousId + 1)
                {
                    countAbove += 1;
                    previousId += 1;
                    next = i.Data;
                    break;
                }
                else if (next is null && i.Data.IdResult > previousId + 1)
                {
                    countAbove += 1;
                    next = i.Data;
                }
                else if(next is not null && i.Data.IdResult > previousId + 1 &&  i.Data.IdResult < (next?.IdResult ?? 0))
                {
                    countAbove += 1;
                    next = i.Data;
                }
            }
            if (countAbove == 0)
                finished = true;
            else if(next is OpData && (next?.IdResult ?? 0) > previousId + 1)
            {
                next?.IdResult = previousId + 1;
                ReplaceRefs(next?.IdResult ?? -1, previousId + 1, buffer);
            }
        }


        
    }
    static void ReplaceRefs(int from, int to, NewSpirvBuffer buffer)
    {
        foreach (var i in buffer)
        {
            foreach (var op in i.Data)
            {
                if (op.Kind == OperandKind.IdRef || op.Kind == OperandKind.IdResultType || op.Kind == OperandKind.IdScope || op.Kind == OperandKind.IdMemorySemantics)
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
