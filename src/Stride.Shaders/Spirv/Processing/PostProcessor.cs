using Stride.Shaders.Spirv.Core;
using Stride.Shaders.Spirv.Core.Buffers;
using Stride.Shaders.Spirv.Processing;

namespace Stride.Shaders.Spirv.PostProcessing;

/// <summary>
/// Nano pass merger/optimizer/compiler
/// </summary>
public static class SpirvProcessor
{
    public static void Process(NewSpirvBuffer buffer)
    {
        // Apply<IOVariableDecorator>(buffer);
        // Apply<SDSLVariableReplace>(buffer);
        // Apply<FunctionVariableOrderer>(buffer);
        Apply<TypeDuplicateRemover>(buffer);
        // Apply<MemoryModelDuplicatesRemover>(buffer);
        // Apply<BoundReducer>(buffer);
        // Apply<OpRemover>(buffer);
    }

    static void Apply<T>(NewSpirvBuffer buffer)
        where T : struct, INanoPass
    {
        var p = new T();
        p.Apply(buffer);
    }
}