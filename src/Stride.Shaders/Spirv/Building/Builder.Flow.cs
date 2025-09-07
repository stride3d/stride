using Stride.Shaders.Spirv.Core;
using Stride.Shaders.Spirv.Core.Buffers;

namespace Stride.Shaders.Spirv.Building;


public partial class SpirvBuilder
{
    public SpirvBlock CreateBlock(SpirvContext context, string? name = null)
    {
        // var i = Buffer.InsertOpLabel(Position++, context.Bound++);
        // Buffer.InsertOpUnreachable(Position);
        // var result = new SpirvBlock(i, CurrentFunction ?? throw new NotImplementedException(), name);
        // return result;
        #warning replace
        throw new NotImplementedException();
    }

    public void Return(in SpirvValue? value = null)
    {
        // _ = value switch
        // {
        //     SpirvValue v => Buffer.InsertOpReturnValue(Position++, v.Id).WordCount,
        //     _ => Buffer.InsertOpReturn(Position++).WordCount
        // };
        // CleanBlock();
        #warning replace
        throw new NotImplementedException();
    }

    public void CleanBlock()
    {
        if (Buffer.Instructions[Position].OpCode == Specification.Op.OpUnreachable)
        {
            Buffer.Instructions.RemoveAt(Position);
        }
    }
}