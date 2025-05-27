using CommunityToolkit.HighPerformance.Buffers;
using Stride.Shaders.Spirv.Core;
using Stride.Shaders.Spirv.Core.Parsing;

namespace Stride.Shaders.Spirv.Core;


public struct LiteralInteger : ILiteralNumber, IFromSpirv<LiteralInteger>
{
    public long Words { get; init; }
    public int Size { get; init; }
    public readonly int WordCount => Size / 32;

    public LiteralInteger(sbyte value)
    {
        Words = 0 | value;
        Size = sizeof(sbyte) * 8;
    }
    public LiteralInteger(byte value)
    {
        Words = 0 | value;
        Size = sizeof(byte) * 8;
    }

    public LiteralInteger(short value)
    {
        Words = 0 | value;
        Size = sizeof(short) * 8;
    }
    public LiteralInteger(ushort value)
    {
        Words = 0 | value;
        Size = sizeof(ushort) * 8;
    }

    public LiteralInteger(int value)
    {
        Words = 0 | value;
        Size = sizeof(int) * 8;
    }
    public LiteralInteger(int? value)
    {
        Words = 0 | value ?? 0;
        Size = sizeof(int) * 8;
    }
    public LiteralInteger(uint value)
    {
        Words = 0 | value;
        Size = sizeof(uint) * 8;

    }
    public LiteralInteger(long value)
    {
        Words = 0 | value;
        Size = sizeof(long) * 8;
    }
    public LiteralInteger(ulong value)
    {
        Words = (long)value;
        Size = sizeof(ulong) * 8;

    }

    public LiteralInteger(Span<int> value)
    {
        if (value.Length == 2)
        {
            Size = sizeof(long) * 8;
            Words = value[0] << 32 | value[1];
        }
        else if (value.Length == 1)
        {
            Size = sizeof(int) * 8;
            Words = value[0];
        }
    }


    public static implicit operator LiteralInteger(byte value) => new(value);
    public static implicit operator LiteralInteger(sbyte value) => new(value);
    public static implicit operator LiteralInteger(ushort value) => new(value);
    public static implicit operator LiteralInteger(short value) => new(value);
    public static implicit operator LiteralInteger(int value) => new(value);
    public static implicit operator LiteralInteger(int? value) => new(value);
    public static implicit operator LiteralInteger(uint value) => new(value);
    public static implicit operator LiteralInteger(long value) => new(value);
    public static implicit operator LiteralInteger(ulong value) => new(value);

    public readonly void Write(ref SpirvWriter writer)
    {
        Span<int> span =
        [
            (int)(Words >> 32),
            (int)(Words & 0X000000FF)
        ];
        if (Size < 64)
            writer.Write(span[1]);
        else
            writer.Write(span);
    }

    public static LiteralInteger From(Span<int> words)
    {
        return new(words);
    }

    public static LiteralInteger From(string value)
    {
        throw new NotImplementedException();
    }

    public readonly SpanOwner<int> AsSpanOwner()
    {
        Span<int> span = WordCount == 1 ? [(int)Words] : [(int)(Words >> 32), (int)(Words & 0xFFFFFFFF)];
        var owner = SpanOwner<int>.Allocate(span.Length, AllocationMode.Clear);
        span.CopyTo(owner.Span);
        return owner;
    }

    public override readonly string ToString() => $"{Words}";
}


