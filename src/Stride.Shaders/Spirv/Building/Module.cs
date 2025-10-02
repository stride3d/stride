using Stride.Shaders.Core;

namespace Stride.Shaders.Spirv.Building;


// Should contain symbols for the SPIR-V module
public class SpirvModule()
{
    public Dictionary<string, List<SpirvFunction>> Functions { get; init; } = [];

    public List<ShaderSymbol> InheritedMixins { get; } = [];

    public Dictionary<string, SpirvValue> InheritedVariables { get; } = [];
    public Dictionary<string, List<SpirvFunction>> InheritedFunctions { get; } = [];
}
