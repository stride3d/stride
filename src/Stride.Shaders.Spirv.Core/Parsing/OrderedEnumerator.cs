using CommunityToolkit.HighPerformance.Buffers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stride.Shaders.Spirv.Core.Buffers;

using static Spv.Specification;

namespace Stride.Shaders.Spirv.Core.Parsing;


/// <summary>
/// <para>An enumerator where each instructions is sorted</para>
/// <para>Instruction are grouped together in the InstructionInfo.Order file, and each groups are ordered based on the SPIR-V specification</para>
/// </summary>
public ref struct OrderedEnumerator(ISpirvBuffer buffer)
{
    int index = 0;
    int wordIndex = 0;
    bool started = false;
    

    readonly ISpirvBuffer wbuff = buffer;
    readonly Span<int> InstructionWords => wbuff.InstructionSpan;

    public readonly Instruction Current => new(wbuff, wbuff.InstructionMemory.Slice(wordIndex, wbuff.InstructionSpan[wordIndex] >> 16), index, wordIndex);

    public bool MoveNext()
    {
        // The first time find the lowest group and index 
        if (!started)
        {
            (var firstGroup, var firstPos) = (int.MaxValue, int.MaxValue);
            var wid = 0;
            var idx = 0;
            while(wid < InstructionWords.Length)
            {
                var group = GetGroupOrder(wid);
                if(group < firstGroup)
                {
                    firstGroup = group;
                    firstPos = wid;
                    index = idx;
                }
                idx += 1;
                wid += InstructionWords[wid] >> 16;
            }
            wordIndex = firstPos;
            started = true;
            return true;
        }
        else
        {
            // We start from the current group since we've established there is no other below this one
            var currentGroup = GetGroupOrder(wordIndex);
            for (int group = currentGroup; group < 15; group += 1)
            {
                if(group == currentGroup)
                {
                    var offset = InstructionWords[wordIndex] >> 16;
                    var idx = index + 1;
                    while(wordIndex + offset <  InstructionWords.Length)
                    {
                        if(GetGroupOrder(wordIndex + offset) == group && idx > index)
                        {
                            wordIndex += offset;
                            index = idx;
                            return true;
                        }
                        offset += InstructionWords[wordIndex + offset] >> 16;
                        idx += 1;
                    }
                }
                else
                {
                    var wid = 0;
                    var idx = 0;
                    while (wid < InstructionWords.Length)
                    {
                        var g = GetGroupOrder(wid);
                        if (g == group)
                        {
                            wordIndex = wid;
                            index = idx;
                            return true;
                        }
                        idx += 1;
                        wid += InstructionWords[wid] >> 16;
                    }
                }
            }
            return false;
        }

    }

    readonly int GetGroupOrder(int wid)
    {
        var op = (SDSLOp)(InstructionWords[wid] & 0xFFFF);
        return InstructionInfo.GetGroupOrder(op, op == SDSLOp.OpVariable ? (StorageClass)InstructionWords[wid + 3] : null);
    }
}