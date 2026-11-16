using System.Text;
using Stride.Shaders.Spirv.Core.Parsing;

namespace Stride.Shaders.Spirv.Core.Buffers;

/// <summary>
/// The words of one instruction: an array of its own, or a view into the words of a <see cref="SpirvBuffer"/>.
/// </summary>
/// <remarks>
/// A view is valid until its buffer changes, as the buffer may then move its words: take it from the buffer again after that.
/// Debug builds check it.
/// </remarks>
public struct OpData : IComparable<OpData>
{
    private Memory<int> memory;
#if DEBUG
    private SpirvBuffer? viewOf;
    private int viewVersion;
#endif

    public Memory<int> Memory
    {
        readonly get
        {
#if DEBUG
            if (viewOf is not null && viewOf.WordsVersion != viewVersion)
                throw new InvalidOperationException("These words were taken from a SpirvBuffer that changed since then: take them from the buffer again.");
#endif
            return memory;
        }
        internal set
        {
            memory = value;
#if DEBUG
            viewOf = null;
#endif
        }
    }
    public readonly Specification.Op Op => (Specification.Op)(Memory.Span[0] & 0xFFFF);

    public readonly int? IdResult
    {
        get => InstructionInfo.GetInfo(this).GetResultIndex(out var index) ? Memory.Span[index + 1] : null;
        set
        {
            if (InstructionInfo.GetInfo(this).GetResultIndex(out var index) && value is not null)
                Memory.Span[index + 1] = value ?? 0;
        }
    }
    public readonly int? IdResultType
    {
        get => InstructionInfo.GetInfo(this).GetResultTypeIndex(out var index) ? Memory.Span[index + 1] : null;
        set
        {
            if (InstructionInfo.GetInfo(this).GetResultTypeIndex(out var index) && value is not null)
                Memory.Span[index + 1] = value ?? 0;
        }
    }

    public OpData()
    {
        memory = Memory<int>.Empty;
    }

    /// <summary>
    /// Uses <paramref name="memory"/> as the words, without copying them.
    /// </summary>
    public OpData(Memory<int> memory)
    {
        this.memory = memory;
    }

    /// <summary>
    /// Copies <paramref name="words"/> into an array of its own.
    /// </summary>
    public OpData(Span<int> words)
    {
        memory = words.ToArray();
    }

    internal OpData(SpirvBuffer buffer, Memory<int> memory)
    {
        this.memory = memory;
#if DEBUG
        viewOf = buffer;
        viewVersion = buffer.WordsVersion;
#endif
    }

    public readonly SpvOperand Get(string name)
    {
        foreach (var o in this)
        {
            if (name == o.Name && (o.Kind.ToString().Contains("Literal") || o.Kind.ToString().Contains("Id")))
                return o;
        }
        throw new Exception($"No operand '{name}' in op {Op}");
    }

    public readonly bool TryGet<T>(string name, out T operand)
    {
        foreach (var o in this)
        {
            if (name == o.Name)
            {
                operand = o.To<T>();
                return true;
            }
        }
        operand = default!;
        return false;
    }

    public readonly T Get<T>(string name)
    {
        foreach (var o in this)
        {
            if (name == o.Name && (o.Kind.ToString().Contains("Literal") || o.Kind.ToString().Contains("Id")))
                return o.To<T>();
        }
        throw new Exception($"No operand '{name}' in op {Op}");
    }
    public readonly T GetEnum<T>(string name)
        where T : Enum
    {
        foreach (var o in this)
        {
            if (name == o.Name && !o.Kind.ToString().Contains("Literal") && !o.Kind.ToString().Contains("Id"))
                return o.ToEnum<T>();
        }
        throw new Exception($"No enum operand '{name}' in op {Op}");
    }

    public readonly OpDataEnumerator GetEnumerator() => new(Memory.Span);

    public readonly int CompareTo(OpData other)
    {
        var group = InstructionInfo.GetGroupOrder(this);
        var otherGroup = InstructionInfo.GetGroupOrder(other);
        return group.CompareTo(otherGroup);
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        // Check for IdResult first
        foreach (var op in this)
        {
            switch (op.Kind)
            {
                case OperandKind.IdResult:
                    sb.Append("%");
                    sb.Append(op.Words[0]);
                    sb.Append(" = ");
                    break;
            }
        }

        sb.Append(Op);
        foreach (var op in this)
        {
            if (op.Kind == OperandKind.IdResult)
                continue;
            sb.Append(" ");
            switch (op.Kind)
            {
                case OperandKind.IdResultType:
                case OperandKind.IdRef:
                    for (var index = 0; index < op.Words.Length; index++)
                    {
                        if (index > 0)
                            sb.Append(" ");
                        sb.Append("%");
                        sb.Append(op.Words[index]);
                    }
                    break;
                case OperandKind.LiteralInteger when op.Words.Length == 1:
                    foreach (var e in op.Words)
                        sb.Append(e);
                    break;
                case OperandKind.LiteralString:
                    sb.Append('"');
                    sb.Append(op.ToLiteral<string>());
                    sb.Append('"');
                    break;
                case OperandKind k when k.IsEnum():
                    for (var index = 0; index < op.Words.Length; index++)
                    {
                        if (index > 0)
                            sb.Append(" ");
                        sb.Append(k.ConvertEnumValueToString(op.Words[index]));
                    }
                    break;
                default:
                    sb.Append($"unknown_{op.Kind}");
                    if (op.Words.Length != 1)
                        sb.Append($"_{op.Words.Length}");
                    break;
            }
        }
        return sb.ToString();
    }
}
