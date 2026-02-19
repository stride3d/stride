using Stride.Shaders.Core;
using Stride.Shaders.Spirv.Core;

namespace Stride.Shaders.Spirv.Building;


/// <summary>
/// Interface for SpirvBlock and SpirvFunction to avoid duplicating code.
/// </summary>
public interface IInstructionBlock
{
    public int Id { get; }
}

/// <summary>
/// Function block in SPIR-V
/// </summary>
/// <param name="id">IdResult created by the OpFunction instruction</param>
/// <param name="name">Name of the function</param>
/// <param name="type">Type of the function</param>
public struct SpirvFunction(int id, string name, FunctionType type) : IInstructionBlock
{
    public int Id { get; } = id;
    public string Name { get; } = name;
    public bool IsStage { get; set; }
    public FunctionType FunctionType { get; private set; } = type;
    public Dictionary<string, SpirvValue> Parameters { get; } = [];
    public SortedList<int, SpirvBlock> BasicBlocks { get; } = [];
}

/// <summary>
/// Represents a basic block of instructions in SPIR-V, instructions are linearly processed without control flow.
/// </summary>
/// <param name="id">IdResult created by the Label instruction</param>
/// <param name="name">Name of the block</param>
/// <param name="parent">Function where the block is declared</param>
public struct SpirvBlock(in IdRef id, SpirvFunction parent, string? name = null) : IInstructionBlock
{
    public int Id { get; set; } = id;
    public string? Name { get; set; } = name;
    public SpirvFunction Parent { get; set; } = parent;
}
