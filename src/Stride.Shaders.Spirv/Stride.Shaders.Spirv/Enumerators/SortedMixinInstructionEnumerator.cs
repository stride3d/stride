using CommunityToolkit.HighPerformance;
using Stride.Shaders.Spirv.Core;

namespace Stride.Shaders.Spirv;

/// <summary>
/// Instruction enumerator that goes through many mixins. Instructions are sorted.
/// </summary>
public ref struct SortedMixinInstructionEnumerator
{

    MixinGraph Mixins { get; init; }

    int currentGroup;
    int lastMixin;
    int lastIndex;
    int boundOffset;

    public SortedMixinInstructionEnumerator(MixinGraph mixins)
    {
        Mixins = mixins;
        currentGroup = 0;
        lastIndex = -1;
        lastMixin = -1;
        boundOffset = -1;
    }
    public readonly Instruction Current => Mixins[lastMixin].Instructions[lastIndex];
    public bool MoveNext()
    {
        if (Mixins.Count == 0)
            return false;
        if (lastMixin == -1)
        {
            lastMixin = 0;
            lastIndex = 0;
            boundOffset = 0;
            return true;
        }
        else
        {
            var count = Mixins.Count;
            // If the current mixin has no other
            while (currentGroup < 14)
            {
                while (lastMixin < count)
                {
                    var offset = 1;
                    var instruction = Mixins[lastMixin].Instructions[lastIndex + offset];
                    while (lastIndex + offset < count && instruction.IsEmpty)
                    {
                        offset += 1;
                    }
                    if (!instruction.IsEmpty && InstructionInfo.GetGroupOrder(instruction.AsRef()) == currentGroup)
                    {
                        lastIndex += offset;
                        return true;
                    }
                    else
                    {
                        boundOffset += Mixins[lastMixin].Bound;
                        lastMixin += 1;
                        lastIndex = 0;
                    }
                }
                currentGroup += 1;
                lastMixin = 0;
                boundOffset = 0;
            }
            return false;
        }

    }
}
