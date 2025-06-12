using System.Runtime.CompilerServices;
using Stride.Shaders.Spirv.Core.Buffers;
using Stride.Shaders.Spirv.Core.Parsing;


namespace Stride.Shaders.Spirv.Core;

/// <summary>
/// Representation of an instruction from a memory slice.
/// </summary>
/// <param name="Buffer"></param>
/// <param name="Words"></param>
public record struct Instruction(Memory<int> Memory)
{
    public static Instruction Empty { get; } = new(Memory<int>.Empty);

    public static implicit operator IdRef(Instruction i) => new(i.ResultId ?? throw new Exception("Instruction has no result id"));
    public static implicit operator IdResultType(Instruction i) => new(i.ResultId ?? throw new Exception("Instruction has no result id"));


    public Instruction(ISpirvBuffer buffer, int index) : this(Memory<int>.Empty)
    {
        var wid = 0;
        for (int i = 0; i < index; i += 1)
            wid += buffer.InstructionSpan[wid] >> 16;
        Memory = buffer.InstructionMemory.Slice(wid, buffer.InstructionSpan[wid] >> 16);
    }

    public readonly SDSLOp OpCode => (SDSLOp)(Words[0] & 0xFFFF);
    public int? ResultId { get => GetResultId(); set => SetResultId(value); }
    public int? ResultType { get => GetResultType(); set => SetResultType(value); }
    public readonly int WordCount => Words.Length;
    public readonly Span<int> Operands => Memory[1..].Span;

    public readonly Span<int> Words => Memory.Span;

    public bool IsEmpty => Words.IsEmpty;

    public TWrapper UnsafeAs<TWrapper>()
        where TWrapper : struct, IWrapperInstruction, allows ref struct
    {
        return new TWrapper()
        {
            Inner = this
        };
    }

    public T? GetOperand<T>(string name)
        where T : struct, IFromSpirv<T>
    {
        var info = InstructionInfo.GetInfo(OpCode);
        var infoEnumerator = info.GetEnumerator();
        var operandEnumerator = GetEnumerator();
        while (infoEnumerator.MoveNext())
        {
            if (operandEnumerator.MoveNext())
            {
                if (infoEnumerator.Current.Name == name)
                {
                    return operandEnumerator.Current.To<T>();
                }
            }
        }
        return null;
    }

    internal T? GetEnumOperand<T>(string name)
        where T : Enum
    {
        var info = InstructionInfo.GetInfo(OpCode);
        var infoEnumerator = info.GetEnumerator();
        var operandEnumerator = GetEnumerator();
        while (infoEnumerator.MoveNext())
        {
            if (operandEnumerator.MoveNext())
            {
                if (infoEnumerator.Current.Name == name)
                {
                    var curr = operandEnumerator.Current;
                    return Unsafe.As<int, T>(ref curr.Words[0]);
                }
            }
        }
        return default;
    }

    public readonly OperandEnumerator GetEnumerator() => new(this);

    public override string ToString()
    {
        return (ResultId == null ? "" : $"%{ResultId} = ") + $"{OpCode} {string.Join(" ", Operands.ToArray().Select(x => x.ToString()))}";
    }

        public int? GetResultId()
    {
        TryGetOperand<IdResult>("resultId", out var resultId);
        return resultId;
    }

    public void SetResultId(int? value)
    {
        foreach (var o in this)
            if (o.Kind == OperandKind.IdResult)
                o.Words[0] = value ?? -1;
    }
    public int? GetResultType()
    {
        TryGetOperand<IdResult>("resultType", out var resultId);
        return resultId;
    }
    public void SetResultType(int? value)
    {
        foreach (var o in this)
            if (o.Kind == OperandKind.IdResultType)
                o.Words[0] = value ?? -1;
    }

    public bool TryGetOperand<T>(string name, out T? operand)
        where T : struct, IFromSpirv<T>
    {
        var info = InstructionInfo.GetInfo(OpCode);
        var infoEnumerator = info.GetEnumerator();
        var operandEnumerator = GetEnumerator();
        while (infoEnumerator.MoveNext())
        {
            if (operandEnumerator.MoveNext())
            {
                if (infoEnumerator.Current.Name == name)
                {
                    operand = operandEnumerator.Current.To<T>();
                    return true;
                }
            }
        }
        operand = null;
        return false;
    }

    public void OffsetIds(int offset)
    {
        foreach (var o in this)
        {
            if (o.Kind == OperandKind.IdRef)
                o.Words[0] += offset;
            else if (o.Kind == OperandKind.IdResult)
                o.Words[0] += offset;
            else if (o.Kind == OperandKind.IdResultType)
                o.Words[0] += offset;
            else if (o.Kind == OperandKind.PairIdRefLiteralInteger)
                o.Words[0] += offset;
            else if (o.Kind == OperandKind.PairLiteralIntegerIdRef)
                o.Words[1] += offset;
            else if (o.Kind == OperandKind.PairIdRefIdRef)
            {
                o.Words[0] += offset;
                o.Words[1] += offset;
            }
        }
    }
}
