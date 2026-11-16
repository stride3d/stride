namespace Stride.Shaders.Spirv.Core.Buffers;

public interface IMemoryInstruction
{
    void Attach(OpDataIndex dataIndex);
    Memory<int> InstructionMemory { get; }
    public void UpdateInstructionMemory();
}
