using Stride.Shaders.Spirv.Core;

namespace Stride.Shaders.Spirv;


/// <summary>
/// Wrapper over Instruction for mixins
/// </summary>
public ref struct MixinInstruction
{
    public string MixinName { get; init; }
    public Instruction Instruction { get; init; }

    public SDSLOp OpCode => Instruction.OpCode;
    public int? ResultId => Instruction.ResultId;
    public int? ResultType => Instruction.ResultType;
    public Span<int> Words => Instruction.Words.Span;
    public bool IsEmpty => Instruction.IsEmpty;

    public static implicit operator MixinInstruction(Instruction instruction) => new("",instruction);
    public static implicit operator IdRef(MixinInstruction mi) => mi.ResultId ?? throw new Exception("This instruction has no ResultId");    
    public static implicit operator IdResultType(MixinInstruction mi) => mi.ResultId ?? throw new Exception("This instruction has no ResultId");    

    public MixinInstruction(string mixinName, Instruction instruction)
    {
        MixinName = mixinName;
        Instruction = instruction;
    }
}
