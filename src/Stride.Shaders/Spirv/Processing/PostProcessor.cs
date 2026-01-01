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
        //Apply<TypeDuplicateRemover>(buffer);
        //Apply<BoundReducer>(buffer);
        //Apply<NOPRemover>(buffer);
    }

    static void Apply<T>(NewSpirvBuffer buffer)
        where T : struct, INanoPass
    {
        var p = new T();
        p.Apply(buffer);
    }
}