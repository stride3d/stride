using Stride.Shaders.Spirv.Core;
using Stride.Shaders.Spirv.Core.Buffers;

namespace Stride.Shaders.Spirv.Building;


public partial class SpirvBuilder
{
    public SpirvBlock CreateBlock(SpirvContext context, string? name = null)
    {
        var i = Buffer.InsertOpLabel(Position, context.Bound++);
        Position += i.WordCount;
        Buffer.InsertOpUnreachable(Position);
        var result = new SpirvBlock(i, CurrentFunction ?? throw new NotImplementedException(), name);
        return result;
    }

    public void Return(in SpirvValue? value = null)
    {
        Position += value switch
        {
            SpirvValue v => Buffer.InsertOpReturnValue(Position, v.Id).WordCount,
            _ => Buffer.InsertOpReturn(Position).WordCount
        };
        CleanBlock();
    }

    public void CleanBlock()
    {
        if ((Buffer.Span[Position] & 0xFFFF) == (int)SDSLOp.OpUnreachable)
        {
            var size = Buffer.Span[Position] >> 16;
            Buffer.Remove(Position);
            Position -= size;
        }
    }
}