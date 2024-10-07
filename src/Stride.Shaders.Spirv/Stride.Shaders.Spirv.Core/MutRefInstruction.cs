using Stride.Shaders.Spirv.Core.Parsing;
using System.Runtime.CompilerServices;

namespace Stride.Shaders.Spirv.Core;

/// <summary>
/// Helps create instruction through stack allocations instead of buffers
/// </summary>
public ref struct MutRefInstruction
{
    public Span<int> Words { get; }
    public Span<int> Operands => Words [1..];
    public readonly SDSLOp OpCode 
    {
        get => (SDSLOp)(Words[0] & 0xFFFF); 
        set { unchecked { Words[0] = (Words[0] & (int)0xFFFF0000) | (int)value;}}
    }
    public readonly int WordCount
    {
        get => Words[0] >> 16; 
        set => Words[0] = value << 16 | Words[0] & 0xFFFF;
    }


    private int _index;

    public MutRefInstruction(Span<int> words)
    {
        Words = words;
        WordCount = words.Length;
        _index = 1;
    }
    public void Add(scoped Span<int> values)
    {
        values.CopyTo(Words[_index..]);
        _index += values.Length;
    }
    public void Add<T>(Span<T> values)
        where T : ISpirvElement
    {
        foreach(var e in values)
            Add(e.AsSpanOwner().Span);
    }

    public void Add<T>(T? value)
    {
        if (value != null)
        {
            if (value is int i)
                Add([i]);
            else if (value is ISpirvElement element)
                Add(element.AsSpanOwner().Span);
            else if (value is string s)
                Add(s.AsSpanOwner().Span);
            else if (value is Enum e)
                Add([Convert.ToInt32(e)]);
        }
    }

    public readonly OperandEnumerator GetEnumerator() => new(RefInstruction.ParseRef(Words));
    public readonly T GetOperand<T>(string name)
        where T : struct, IFromSpirv<T>
    {
        var info = InstructionInfo.GetInfo(OpCode);
        var infoEnumerator = info.GetEnumerator();
        var operandEnumerator = GetEnumerator();
        while (infoEnumerator.MoveNext())
        {
            operandEnumerator.MoveNext();
            if (infoEnumerator.Current.Name == name)
            {
                return operandEnumerator.Current.To<T>();
            }
        }
        throw new Exception($"Instruction {OpCode} has no operand named \"{name}\"");
    }
}