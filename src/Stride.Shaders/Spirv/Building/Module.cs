namespace Stride.Shaders.Spirv.Building;


// Should contain symbols for the SPIR-V module
public class SpirvModule()
{
    public SortedList<string, SpirvFunction> Functions { get; init; } = [];
}
