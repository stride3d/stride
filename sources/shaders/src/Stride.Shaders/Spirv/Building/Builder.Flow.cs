using Stride.Shaders.Spirv.Core;
using Stride.Shaders.Spirv.Core.Buffers;
using static Stride.Shaders.Spirv.Specification;

namespace Stride.Shaders.Spirv.Building;


public partial class SpirvBuilder
{
    public record struct EscapeBlocks(int ContinueBlock, int MergeBlock);

    public int IfBlockCount { get; internal set; } = 0;

    public int ForBlockCount { get; internal set; } = 0;

    public EscapeBlocks? CurrentEscapeBlocks { get; internal set; }

    public static bool IsFunctionTermination(Op op)
    {
        switch (op)
        {
            case Op.OpReturn:
            case Op.OpReturnValue:
            case Op.OpKill:
            case Op.OpUnreachable:
            case Op.OpTerminateInvocation:
                return true;
            default:
                return false;
        }
    }

    public static bool IsBlockTermination(Op op)
    {
        switch (op)
        {
            case Op.OpReturn:
            case Op.OpReturnValue:
            case Op.OpKill:
            case Op.OpUnreachable:
            case Op.OpTerminateInvocation:
            case Op.OpBranch:
            case Op.OpBranchConditional:
            case Op.OpSwitch:
                return true;
            default:
                return false;
        }
    }

    public SpirvBlock CreateBlock(SpirvContext context, int blockId, string? name = null)
    {
        var i = Buffer.Insert(Position++, new OpLabel(blockId));
        if (name != null)
            context.AddName(i.ResultId, name);
        var result = new SpirvBlock(i.ResultId, CurrentFunction ?? throw new NotImplementedException(), name);

        CurrentFunction.Value.BasicBlocks.Add(result.Id, result);
        CurrentBlock = result;

        return result;
    }

    public SpirvBlock CreateBlock(SpirvContext context, string? name = null)
    {
        var i = Buffer.Insert(Position++, new OpLabel(context.Bound++));
        if (name != null)
            context.AddName(i.ResultId, name);
        var result = new SpirvBlock(i.ResultId, CurrentFunction ?? throw new NotImplementedException(), name);

        CurrentFunction.Value.BasicBlocks.Add(result.Id, result);
        CurrentBlock = result;

        return result;
    }

    public void Return(in SpirvValue? value = null)
    {
        _ = value switch
        {
            SpirvValue v => Buffer.InsertData(Position++, new OpReturnValue(v.Id)),
            _ => Buffer.InsertData(Position++, new OpReturn())
        };
    }
}