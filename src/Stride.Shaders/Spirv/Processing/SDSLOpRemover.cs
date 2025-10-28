using Stride.Shaders.Spirv.Core.Buffers;
using static Stride.Shaders.Spirv.Specification;

namespace Stride.Shaders.Spirv.Processing;


/// <summary>
/// Removes SDSL specific instructions
/// </summary>
public struct NOPRemover : INanoPass
{
    public readonly void Apply(NewSpirvBuffer buffer)
    {
        for (int i = 0; i < buffer.Count; i++)
            if (buffer[i].Op == Op.OpNop)
                buffer.RemoveAt(i--);
    }    
}
