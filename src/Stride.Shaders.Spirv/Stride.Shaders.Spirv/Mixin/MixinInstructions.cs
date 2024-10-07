using Stride.Shaders.Spirv.Core;
using Stride.Shaders.Spirv.Core.Parsing;

namespace Stride.Shaders.Spirv;

/// <summary>
/// Helper to enumerate instructions from many mixins
/// </summary>
public ref struct MixinInstructions
{
    Mixin mixin;
    public MixinInstructions(Mixin mixin)
    {
        this.mixin = mixin;
    }

    public Instruction this[int index]
    {
        get
        {
            var count = mixin.Buffer.Length;
            if(index >= count) return Instruction.Empty;
            var enumerator = GetEnumerator();
            for(int i = 0; enumerator.MoveNext() && i < index; i++);
            return enumerator.Current;
        }
    }

    public InstructionEnumerator GetEnumerator() => mixin.Buffer.GetEnumerator();
}