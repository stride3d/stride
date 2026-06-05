using CommunityToolkit.HighPerformance.Buffers;
using Stride.Shaders.Spirv.Core;
using Stride.Shaders.Spirv.Core.Parsing;

namespace Stride.Shaders.Spirv.Core;

public struct LiteralInteger : ILiteralNumber, IFromSpirv<LiteralInteger>
{
    public MemoryOwner<int> Data { get; init; }
    public int Size { get; init; }
    public readonly int WordCount => Size / 32;

    public readonly ReadOnlySpan<int> Words => Data.Span;

    public LiteralInteger(sbyte value)
    {
        Data = MemoryOwner<int>.Allocate(1);
        Data.Span[0] = value;
        Size = sizeof(sbyte) * 8;
    }
    public LiteralInteger(byte value)
    {
        Data = MemoryOwner<int>.Allocate(1);
        Data.Span[0] = value;
        Size = sizeof(byte) * 8;
    }

    public LiteralInteger(short value)
    {
        Data = MemoryOwner<int>.Allocate(1);
        Data.Span[0] = value;
        Size = sizeof(short) * 8;
    }
    public LiteralInteger(ushort value)
    {
        Data = MemoryOwner<int>.Allocate(1);
        Data.Span[0] = value;
        Size = sizeof(ushort) * 8;
    }

    public LiteralInteger(int value)
    {
        Data = MemoryOwner<int>.Allocate(1);
        Data.Span[0] = 0 | value;
        Size = sizeof(int) * 8;
    }
    public LiteralInteger(int? value)
    {
        Data = MemoryOwner<int>.Allocate(1);
        Data.Span[0] = value ?? 0;
        Size = sizeof(int) * 8;
    }
    public LiteralInteger(uint value)
    {
        Data = MemoryOwner<int>.Allocate(1);
        Data.Span[0] = (int)value;
        Size = sizeof(uint) * 8;

    }
    public LiteralInteger(long value)
    {
        Data = MemoryOwner<int>.Allocate(2);
        Data.Span[0] = (int)(value >> 32);
        Data.Span[1] = (int)(value & 0xFFFFFFFF);
        Size = sizeof(long) * 8;
    }
    public LiteralInteger(ulong value)
    {
        Data = MemoryOwner<int>.Allocate(2);
        Data.Span[0] = (int)(value >> 32);
        Data.Span[1] = (int)(value & 0xFFFFFFFF);
        Size = sizeof(ulong) * 8;

    }

    public LiteralInteger(Span<int> value)
    {
        Data = MemoryOwner<int>.Allocate(value.Length);
        value.CopyTo(Data.Span);
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
    public static implicit operator int(LiteralInteger value) => value.Data.Span[0];

    public static LiteralInteger From(Span<int> words)
    {
        return new(words);
    }

    public static LiteralInteger From(string value)
    {
        throw new NotImplementedException();
    }

    public override readonly string ToString() => $"{Data}";

    public void Dispose()
    {
        throw new NotImplementedException();
    }
}




public struct LiteralExtInstInteger : ILiteralNumber, IFromSpirv<LiteralExtInstInteger>
{
    public MemoryOwner<int> Data { get; init; }
    public int Size { get; init; }
    public readonly int WordCount => Size / 32;

    public readonly ReadOnlySpan<int> Words => Data.Span;

    public LiteralExtInstInteger(sbyte value)
    {
        Data = MemoryOwner<int>.Allocate(1);
        Data.Span[0] = value;
        Size = sizeof(sbyte) * 8;
    }
    public LiteralExtInstInteger(byte value)
    {
        Data = MemoryOwner<int>.Allocate(1);
        Data.Span[0] = value;
        Size = sizeof(byte) * 8;
    }

    public LiteralExtInstInteger(short value)
    {
        Data = MemoryOwner<int>.Allocate(1);
        Data.Span[0] = value;
        Size = sizeof(short) * 8;
    }
    public LiteralExtInstInteger(ushort value)
    {
        Data = MemoryOwner<int>.Allocate(1);
        Data.Span[0] = value;
        Size = sizeof(ushort) * 8;
    }

    public LiteralExtInstInteger(int value)
    {
        Data = MemoryOwner<int>.Allocate(1);
        Data.Span[0] = 0 | value;
        Size = sizeof(int) * 8;
    }
    public LiteralExtInstInteger(int? value)
    {
        Data = MemoryOwner<int>.Allocate(1);
        Data.Span[0] = value ?? 0;
        Size = sizeof(int) * 8;
    }
    public LiteralExtInstInteger(uint value)
    {
        Data = MemoryOwner<int>.Allocate(1);
        Data.Span[0] = (int)value;
        Size = sizeof(uint) * 8;

    }
    public LiteralExtInstInteger(long value)
    {
        Data = MemoryOwner<int>.Allocate(2);
        Data.Span[0] = (int)(value >> 32);
        Data.Span[1] = (int)(value & 0xFFFFFFFF);
        Size = sizeof(long) * 8;
    }
    public LiteralExtInstInteger(ulong value)
    {
        Data = MemoryOwner<int>.Allocate(2);
        Data.Span[0] = (int)(value >> 32);
        Data.Span[1] = (int)(value & 0xFFFFFFFF);
        Size = sizeof(ulong) * 8;

    }

    public LiteralExtInstInteger(Span<int> value)
    {
        Data = MemoryOwner<int>.Allocate(value.Length);
        value.CopyTo(Data.Span);
    }


    public static implicit operator LiteralExtInstInteger(byte value) => new(value);
    public static implicit operator LiteralExtInstInteger(sbyte value) => new(value);
    public static implicit operator LiteralExtInstInteger(ushort value) => new(value);
    public static implicit operator LiteralExtInstInteger(short value) => new(value);
    public static implicit operator LiteralExtInstInteger(int value) => new(value);
    public static implicit operator LiteralExtInstInteger(int? value) => new(value);
    public static implicit operator LiteralExtInstInteger(uint value) => new(value);
    public static implicit operator LiteralExtInstInteger(long value) => new(value);
    public static implicit operator LiteralExtInstInteger(ulong value) => new(value);
    public static implicit operator int(LiteralExtInstInteger value) => value.Data.Span[0];

    public static LiteralExtInstInteger From(Span<int> words)
    {
        return new(words);
    }

    public static LiteralExtInstInteger From(string value)
    {
        throw new NotImplementedException();
    }

    public override readonly string ToString() => $"{Data}";

    public void Dispose()
    {
        throw new NotImplementedException();
    }
}
