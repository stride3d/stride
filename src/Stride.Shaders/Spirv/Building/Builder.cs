using Stride.Shaders.Core;
using Stride.Shaders.Spirv.Core;
using Stride.Shaders.Spirv.Core.Buffers;

namespace Stride.Shaders.Spirv.Building;



// Should have utility functions to add instruction to the buffer
public partial class SpirvBuilder() : IDisposable
{
    public SpirvBuffer Buffer { get; init; } = new();
    public SpirvFunction? CurrentFunction { get; private set; }
    public SpirvBlock? CurrentBlock { get; private set; }
    public int Position { get; private set; } = 5;

    public void SetPositionTo<TBlock>(TBlock block, bool beggining = false)
        where TBlock : IInstructionBlock
    {
        if (block is SpirvBlock bb)
            SetPositionTo(bb.Parent);
        bool blockFound = false;
        Span<int> blockTermination = [
            (int)SDSLOp.OpBranch,
            (int)SDSLOp.OpBranchConditional,
            (int)SDSLOp.OpSwitch,
            (int)SDSLOp.OpReturn,
            (int)SDSLOp.OpReturnValue,
            (int)SDSLOp.OpKill,
            (int)SDSLOp.OpUnreachable,
            (int)SDSLOp.OpTerminateInvocation
        ];
        foreach (var e in Buffer)
        {
            if (e.ResultId is int id && id == block.Id)
            {
                blockFound = true;
                // In case we want to top at the beginning of the block
                if(beggining)
                {
                    Position = e.WordIndex + e.WordCount;
                    return;
                }
            }
            if (block is SpirvBlock && blockFound && blockTermination.Contains((int)e.OpCode))
            {
                Position = e.WordIndex;
                return;
            }
            else if (block is SpirvFunction && blockFound && e.OpCode == SDSLOp.OpFunctionEnd)
            {
                Position = e.WordIndex;
                return;
            }
        }
        Position = Buffer.Length;
    }

    public SpirvBuffer Build(SpirvContext context)
    {
        context.Buffer.Sort();
        return SpirvBuffer.Merge(context.Buffer, Buffer);
    }

    public void Dispose() => Buffer.Dispose();
}
