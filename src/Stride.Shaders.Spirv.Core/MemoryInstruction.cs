using Stride.Shaders.Spirv.Core.Buffers;
using Stride.Shaders.Spirv.Core.Parsing;


namespace Stride.Shaders.Spirv.Core;

/// <summary>
/// Representation of an instruction from a memory slice.
/// </summary>
/// <param name="Buffer"></param>
/// <param name="Words"></param>
public record struct Instruction(ISpirvBuffer Buffer, Memory<int> Words)
{
    public static Instruction Empty { get; } = new(null!, Memory<int>.Empty);

    public static implicit operator IdRef(Instruction i) => new(i.ResultId ?? throw new Exception("Instruction has no result id"));
    public static implicit operator IdResultType(Instruction i) => new(i.ResultId ?? throw new Exception("Instruction has no result id"));


    public Instruction(ISpirvBuffer buffer, int index) : this(buffer, Memory<int>.Empty)
    {
        Buffer = buffer;
        var wid = 0;
        for (int i = 0; i < index; i += 1)
            wid += buffer.InstructionSpan[wid] >> 16;
        Words = buffer.InstructionMemory.Slice(wid, buffer.InstructionSpan[wid] >> 16);
    }

    public SDSLOp OpCode => AsRef().OpCode;
    public readonly int? ResultId { get => AsRef().ResultId; set => AsRef().SetResultId(value); }
    public readonly int? ResultType { get => AsRef().ResultType; set => AsRef().SetResultType(value); }
    public readonly int WordCount => Words.Length;
    public readonly Memory<int> Operands => Words[1..];

    public bool IsEmpty => Words.IsEmpty;

    public readonly RefInstruction AsRef() => RefInstruction.ParseRef(Words.Span);
    public readonly TWrapper UnsafeAs<TWrapper>() where TWrapper : struct, IWrapperInstruction, allows ref struct
        => RefInstruction.ParseRef(Words.Span).UnsafeAs<TWrapper>();

    public readonly T? GetOperand<T>(string name) where T : struct, IFromSpirv<T>
        => AsRef().GetOperand<T>(name);

    public readonly bool TryGetOperand<T>(string name, out T? operand) where T : struct, IFromSpirv<T>
        => AsRef().TryGetOperand(name, out operand);

    public readonly OperandEnumerator GetEnumerator() => AsRef().GetEnumerator();

    public override string ToString()
    {
        return (ResultId == null ? "" : $"%{ResultId} = ") + $"{OpCode} {string.Join(" ", Operands.ToArray().Select(x => x.ToString()))}";
    }
}
