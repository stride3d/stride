namespace Stride.Shaders.Spirv.Building;


// Should contain symbols for the SPIR-V module
public class SpirvModule()
{
    public List<SpirvFunction> Functions { get; init; } = [];
}
