using Stride.Shaders.Core;
using Stride.Shaders.Spirv.Core;
using Stride.Shaders.Spirv.Core.Buffers;

namespace Stride.Shaders.Spirv.Building;




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

    /// <summary>
    /// Swizzle to apply to the value.
    /// </summary>
    /// <remarks>
    /// Swizzle doesn't affect the <see cref="TypeId"/>. For example, <c>float3().xy</c> will have <see cref="TypeId"> <c>float3</c>.
    /// </remarks>
    public int[]? Swizzles { get; set; }

    public SymbolType GetValueType(SpirvContext context, bool includeSwizzles)
    {
        var type = context.ReverseTypes[TypeId];
        if (type is PointerType p)
            type = p.BaseType;

        if (includeSwizzles && Swizzles != null)
        {
            type = (type, Swizzles.Length) switch
            {
                (ScalarType s, > 1) => new VectorType(s, Swizzles.Length),
                (ScalarType s, 1) => s,
                (VectorType v, >1) => new VectorType(v.BaseType, Swizzles.Length),
                (VectorType v, 1) => v.BaseType,
            };
        }

        return type;
    }

    public void ApplySwizzles(Span<int> swizzleIndices)
    {
        var oldSwizzles = Swizzles;
        Swizzles = swizzleIndices.ToArray();
        if (oldSwizzles != null)
        {
            // Reapply swizzle on existing swizzles
            for (int i = 0; i < Swizzles.Length; ++i)
                Swizzles[i] = oldSwizzles[Swizzles[i]];
        }

        // TODO: remove swizzle if identity?
    }

    internal void ThrowIfSwizzle()
    {
        if (Swizzles != null)
            throw new InvalidOperationException("This expression doesn't handle swizzle");
    }
}


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
