using Stride.Shaders.Spirv.Core.Buffers;

namespace Stride.Shaders.Spirv.Core;

/// <summary>
/// A SPIR-V value representing the result of an instruction
/// </summary>
public struct SpirvValue
{ 
    /// <param name="id">IdResult of the instruction</param>
    /// <param name="typeId">IdResultType of the instruction</param>
    /// <param name="name">Optional name attached to the value</param>
    public SpirvValue(IdRef id, IdRef typeId, string? name = null)
    {
        Id = id;
        TypeId = typeId;
        Name = name;
    }

    public SpirvValue(OpData instruction, string? name = null)
    {
        if (InstructionInfo.GetInfo(instruction).GetResultIndex(out var index))
            Id = instruction.Memory.Span[index + 1];
        if (InstructionInfo.GetInfo(instruction).GetResultTypeIndex(out var typeIndex))
            TypeId = instruction.Memory.Span[typeIndex + 1];
        Name = name;
    }
    public int Id { get; set; }
    public int TypeId { get; set; }
    public string? Name { get; set; }
}
