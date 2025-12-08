using Stride.Shaders.Spirv.Core.Buffers;
using static Stride.Shaders.Spirv.Specification;

namespace Stride.Shaders.Spirv.Core.Parsing;


/// <summary>
/// <para>An enumerator where each instructions is sorted</para>
/// <para>Instruction are grouped together in the InstructionInfo.Order file, and each groups are ordered based on the SPIR-V specification</para>
/// </summary>
public ref struct OrderedEnumerator(ISpirvBuffer buffer)
{
    int currentPosition = 0;
    bool started = false;
    

    public readonly Instruction Current => buffer.InstructionsSpan[currentPosition];

    public bool MoveNext()
    {
        // The first time find the lowest group and index 
        if (!started)
        {
            var firstGroup = int.MaxValue;
            var firstPos = int.MaxValue;
            for (var index = 0; index < buffer.InstructionsSpan.Length; index++)
            {
                var instruction = buffer.InstructionsSpan[index];
                var group = GetGroupOrder(instruction);
                if (group < firstGroup)
                {
                    firstGroup = group;
                    firstPos = index;
                }
            }

            currentPosition = firstPos;
            started = true;
            return true;
        }
        else
        {
            // We start from the current group since we've established there is no other below this one
            var currentGroup = GetGroupOrder(buffer.InstructionsSpan[currentPosition]);
            for (int group = currentGroup; group < 15; group += 1)
            {
                if(group == currentGroup)
                {
                    for (int i = currentPosition + 1; i < buffer.InstructionsSpan.Length; ++i)
                    {
                        if (GetGroupOrder(buffer.InstructionsSpan[i]) == group)
                        {
                            currentPosition = i;
                            return true;
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < buffer.InstructionsSpan.Length; ++i)
                    {
                        if (GetGroupOrder(buffer.InstructionsSpan[i]) == group)
                        {
                            currentPosition = i;
                            return true;
                        }
                    }
                }
            }
            return false;
        }

    }

    readonly int GetGroupOrder(Instruction instruction)
    {
        return InstructionInfo.GetGroupOrder(instruction.OpCode, instruction.OpCode == Op.OpVariable || instruction.OpCode == Op.OpVariableSDSL ? (StorageClass)instruction.Words[3] : null);
    }
}