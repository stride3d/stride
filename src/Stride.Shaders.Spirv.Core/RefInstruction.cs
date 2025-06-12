using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using Stride.Shaders.Spirv.Core.Buffers;
using Stride.Shaders.Spirv.Core.Parsing;
using static Spv.Specification;


namespace Stride.Shaders.Spirv.Core;

public interface IWrapperInstruction
{
    RefInstruction Inner { get; set; }
}


/// <summary>
/// A ref struct representation of an instruction in a buffer.
/// </summary>
public ref struct RefInstruction
{

    public static RefInstruction Empty => new() { Words = [], Operands = [] };


    /// <summary>
    /// Word Count is the high-order 16 bits of word 0 of the instruction, holding its total WordCount. 
    /// <br/> If the instruction takes a variable number of operands, Word Count also says "+ variable", after stating the minimum size of the instruction.
    /// </summary>
    public readonly int WordCount => Words[0] >> 16;
    public readonly SDSLOp OpCode => (SDSLOp)(Words[0] & 0xFFFF);
    public int? ResultId { get => GetResultId(); set => SetResultId(value); }
    public int? ResultType { get => GetResultType(); set => SetResultType(value); }
    public Span<int> Operands { get; init; }
    public int WordIndex { get; set; }
    public Span<int> Words { get; init; }

    public readonly bool IsEmpty => Words.IsEmpty;




    public OperandEnumerator GetEnumerator() => new(this);


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

    public static RefInstruction Parse(Memory<int> owner, int ownerIndex)
    {
        var words = owner.Span.Slice(ownerIndex, owner.Span[ownerIndex] >> 16);
        return new RefInstruction()
        {
            Operands = words[1..],
            WordIndex = ownerIndex,
            Words = words
        };
    }
    // public static RefInstruction ParseRef(ReadOnlySpan<int> words)
    // {
    //     return new RefInstruction()
    //     {
    //         Operands = words[1..],
    //         Words = words,
    //     };
    // }
    public static RefInstruction ParseRef(Span<int> words, int? wordIndex = null)
    {
        return new RefInstruction()
        {
            Words = words,
            WordIndex = wordIndex ?? -1,
            Operands = words[1..]
        };
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

    public readonly Instruction ToOwned(SpirvBuffer buffer)
    {
        return new(buffer.Memory[WordIndex..(WordIndex + WordCount)]);
    }

    public override string ToString()
    {
        var builder = new StringBuilder();
        builder.Append(OpCode).Append(' ');
        foreach (var o in this)
        {
            builder.Append(o.ToString()).Append(' ');
        }
        return builder.ToString();
    }

    public TWrapper UnsafeAs<TWrapper>()
        where TWrapper : struct, IWrapperInstruction, allows ref struct
    {
        return new TWrapper()
        {
            Inner = this
        };
    }
}