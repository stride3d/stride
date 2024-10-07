using CommunityToolkit.HighPerformance;
using Stride.Shaders.Spirv.Core;
using Stride.Shaders.Spirv.Core.Parsing;


namespace Stride.Shaders.Spirv;

/// <summary>
/// Instruction enumerator that goes through many mixins with filters.
/// </summary>
public ref struct MixinFilteredInstructionEnumerator
{
    MixinGraph Mixins { get; init; }

    MixinInstructionEnumerator enumerator;

    readonly SDSLOp? filter1;
    readonly SDSLOp? filter2;
    readonly SDSLOp? filter3;
    readonly SDSLOp? filter4;

    public MixinFilteredInstructionEnumerator(MixinGraph mixins, SDSLOp f1)
    {
        Mixins = mixins;
        enumerator = mixins.Instructions.GetEnumerator();
        filter1 = f1;
    }
    public MixinFilteredInstructionEnumerator(MixinGraph mixins, SDSLOp f1, SDSLOp f2)
    {
        Mixins = mixins;
        enumerator = mixins.Instructions.GetEnumerator();
        filter1 = f1;
        filter2 = f2;
    }
    public MixinFilteredInstructionEnumerator(MixinGraph mixins, SDSLOp f1, SDSLOp f2, SDSLOp f3)
    {
        Mixins = mixins;
        enumerator = mixins.Instructions.GetEnumerator();
        filter1 = f1;
        filter2 = f2;
        filter2 = f3;
    }
    public MixinFilteredInstructionEnumerator(MixinGraph mixins, SDSLOp f1, SDSLOp f2, SDSLOp f3, SDSLOp f4)
    {
        Mixins = mixins;
        enumerator = mixins.Instructions.GetEnumerator();
        filter1 = f1;
        filter2 = f2;
        filter3 = f3;
        filter4 = f4;
    }
    public MixinInstruction Current => enumerator.Current;
    public bool MoveNext()
    {
        while(enumerator.MoveNext())
        {
            if(
                (filter2 == null && enumerator.Current.OpCode == filter1)
                || (filter2 != null && filter3 == null && enumerator.Current.OpCode == filter1 || enumerator.Current.OpCode == filter2)
                || (filter3 != null && filter4 == null && enumerator.Current.OpCode == filter1 || enumerator.Current.OpCode == filter2 || enumerator.Current.OpCode == filter3)
                || (filter4 != null && enumerator.Current.OpCode == filter1 || enumerator.Current.OpCode == filter2 || enumerator.Current.OpCode == filter3 || enumerator.Current.OpCode == filter4)
            )
                return true;
        }
        return false;
    }
}
