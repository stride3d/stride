using System.Text;
using CommunityToolkit.HighPerformance.Buffers;
using Stride.Shaders.Spirv.Core.Parsing;

namespace Stride.Shaders.Spirv.Core.Buffers;

public struct OpData : IDisposable, IComparable<OpData>
{
    public MemoryOwner<int> Memory { get; internal set { field?.Dispose(); field = value; } }
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
        Memory = MemoryOwner<int>.Empty;
    }

    public OpData(MemoryOwner<int> memory)
    {
        Memory = memory;
    }
    public OpData(Span<int> memory)
    {
        Memory = MemoryOwner<int>.Allocate(memory.Length);
        memory.CopyTo(Memory.Span);
    }

    public readonly void Dispose() => Memory.Dispose();

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
