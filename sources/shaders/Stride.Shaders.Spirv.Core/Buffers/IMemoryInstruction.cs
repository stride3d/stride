using CommunityToolkit.HighPerformance.Buffers;

namespace Stride.Shaders.Spirv.Core.Buffers;

public interface IMemoryInstruction
{
    ref OpData OpData { get; }
    void Attach(OpDataIndex dataIndex);
    MemoryOwner<int> InstructionMemory { get; }
    public void UpdateInstructionMemory();
}