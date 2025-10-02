using Stride.Shaders.Core;
using System.Reflection;

namespace Stride.Shaders.Spirv.Building;


// Should contain symbols for the SPIR-V module
public class SpirvModule()
{
    public Dictionary<string, List<SpirvFunction>> Functions { get; init; } = [];

    public Dictionary<string, List<SpirvFunction>> InheritedFunctions { get; } = [];

    public List<SpirvFunction> FindFunctions(string name)
    {
        var result = new List<SpirvFunction>();
        if (Functions.TryGetValue(name, out var funcGroup))
            result.AddRange(funcGroup);
        if (InheritedFunctions.TryGetValue(name, out funcGroup))
            result.AddRange(funcGroup);
        return result;
    }
}
