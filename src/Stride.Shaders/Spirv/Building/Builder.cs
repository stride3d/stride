using Stride.Shaders.Core;
using Stride.Shaders.Spirv.Core;
using Stride.Shaders.Spirv.Core.Buffers;
using Stride.Shaders.Spirv.Tools;
using System;
using static Stride.Shaders.Spirv.Specification;

namespace Stride.Shaders.Spirv.Building;



// Should have utility functions to add instruction to the buffer
public partial class SpirvBuilder()
{
    NewSpirvBuffer Buffer { get; init; } = new();
    public SpirvFunction? CurrentFunction { get; internal set; }
    public SpirvBlock? CurrentBlock { get; internal set; }
    public int Position { get; internal set; } = 0;

    public void AddFunctionVariable(int paramType, int paramVariable)
    {
        if (CurrentFunction is not SpirvFunction f)
            throw new InvalidOperationException();

        var currentPosition = Position;
        var currentBlock = CurrentBlock;

        SetPositionTo(f.BasicBlocks.First().Value, true);
        // Go after label and the last OpVariable
        Position++;
        while (Buffer[Position].Op == Op.OpVariable)
            Position++;
        Insert(new OpVariable(paramType, paramVariable, StorageClass.Function, null));

        Position = currentPosition + 1;
        CurrentBlock = currentBlock;
    }


    public void SetPositionTo<TBlock>(TBlock block, bool beggining = false)
        where TBlock : IInstructionBlock
    {
        if (block is SpirvBlock bb)
            SetPositionTo(bb.Parent);
        bool blockFound = false;
        Span<int> blockTermination = [
            (int)Op.OpBranch,
            (int)Op.OpBranchConditional,
            (int)Op.OpSwitch,
            (int)Op.OpReturn,
            (int)Op.OpReturnValue,
            (int)Op.OpKill,
            (int)Op.OpUnreachable,
            (int)Op.OpTerminateInvocation
        ];
        var pos = -1;
        foreach (var e in Buffer)
        {
            pos += 1;
            if (e.Data.IdResult is int id && id == block.Id)
            {
                blockFound = true;
                // In case we want to top at the beginning of the block
                if (beggining)
                {
                    Position = pos;
                    return;
                }
            }
            if (block is SpirvBlock block2 && blockFound && IsBlockTermination(e.Op))
            {
                CurrentBlock = block2;
                Position = pos;
                return;
            }
            else if (block is SpirvFunction && blockFound && e.Op == Op.OpFunctionEnd)
            {
                Position = pos;
                return;
            }
        }
        Position = Buffer.Count;
    }


    public T Insert<T>(in T value)
        where T : struct, IMemoryInstruction
        => Buffer.Insert(Position++, value);

    [Obsolete("Use the insert method instead")]
    public NewSpirvBuffer GetBuffer() => Buffer;

    public override string ToString()
    {
        return Spv.Dis(Buffer);
    }
}
