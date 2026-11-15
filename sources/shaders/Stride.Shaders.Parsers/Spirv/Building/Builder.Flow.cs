using Stride.Shaders.Core;
using Stride.Shaders.Spirv.Core;
using Stride.Shaders.Spirv.Core.Buffers;
using static Stride.Shaders.Spirv.Specification;

namespace Stride.Shaders.Spirv.Building;


public partial class SpirvBuilder
{
    public record struct EscapeBlocks(int ContinueBlock, int MergeBlock, SwitchContinue? Switch = null)
    {
        /// <summary>
        /// The <see cref="ContinueBlock"/> of a switch that is not in a loop (0 is never a SPIR-V id).
        /// </summary>
        public const int NoLoop = 0;
    }

    /// <summary>
    /// A continue inside a switch (FXC rejects one there, X3708) sets this flag and leaves the switch; after the switch, the flag continues the loop.
    /// </summary>
    public sealed class SwitchContinue
    {
        public int? Flag { get; set; }
    }

    /// <summary>
    /// Goes to the next iteration of the innermost loop.
    /// </summary>
    public void Continue(SpirvContext context, EscapeBlocks escapeBlocks)
    {
        if (escapeBlocks.Switch is { } switchContinue)
        {
            switchContinue.Flag ??= AddFunctionVariable(context.GetOrRegister(new PointerType(ScalarType.Boolean, StorageClass.Function)), context.Bound++, context.CompileConstant(false).Id);
            Insert(new OpStore(switchContinue.Flag.Value, context.CompileConstant(true).Id, null, []));
            Insert(new OpBranch(escapeBlocks.MergeBlock));
        }
        else
        {
            Insert(new OpBranch(escapeBlocks.ContinueBlock));
        }
    }

    public int IfBlockCount { get; internal set; } = 0;

    public int ForBlockCount { get; internal set; } = 0;

    public int WhileBlockCount { get; internal set; } = 0;

    public int SwitchBlockCount { get; internal set; } = 0;

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
            case Op.OpSwitchIdSDSL:
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
