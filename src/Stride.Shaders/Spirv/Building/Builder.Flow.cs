using Stride.Shaders.Spirv.Core;
using Stride.Shaders.Spirv.Core.Buffers;

namespace Stride.Shaders.Spirv.Building;


public partial class SpirvBuilder
{
    public SpirvBlock CreateBlock(SpirvContext context, string? name = null)
    {
        var i = Buffer.Insert(Position++, new OpLabel(context.Bound++));
        Buffer.Insert(Position, new OpUnreachable());
        var result = new SpirvBlock(i.ResultId, CurrentFunction ?? throw new NotImplementedException(), name);

        CurrentFunction.Value.BasicBlocks.Add(result.Id, result);

        return result;
    }

    public void Return(in SpirvValue? value = null)
    {
        _ = value switch
        {
            SpirvValue v => Buffer.InsertData(Position++, new OpReturnValue(v.Id)),
            _ => Buffer.InsertData(Position++, new OpReturn())
        };
        CleanBlock();
    }

    public void CleanBlock()
    {
        if (Buffer[Position].Op == Specification.Op.OpUnreachable)
            Buffer.RemoveAt(Position);
    }
}