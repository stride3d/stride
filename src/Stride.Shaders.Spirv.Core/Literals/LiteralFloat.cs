using CommunityToolkit.HighPerformance.Buffers;
using Stride.Shaders.Spirv.Core;
using Stride.Shaders.Spirv.Core.Parsing;
using System.Drawing;
using System.Numerics;
using System.Runtime.InteropServices;


namespace Stride.Shaders.Spirv.Core;


public struct LiteralFloat : ILiteralNumber, IFromSpirv<LiteralFloat>
{
    public MemoryOwner<int> Data { get; init; }
    int size;

    public readonly int WordCount => size / 32;

    public readonly ReadOnlySpan<int> Words => Data.Span;

    public LiteralFloat(Half value)
    {
        Data = MemoryOwner<int>.Allocate(1);
        Data.Span[0] = BitConverter.HalfToInt16Bits(value);
        size = 16;

    }
    public LiteralFloat(float value)
    {
        Data = MemoryOwner<int>.Allocate(1);
        Data.Span[0] = BitConverter.SingleToInt32Bits(value);
        size = sizeof(float) * 8;
    }
    public LiteralFloat(double value)
    {
        Data = MemoryOwner<int>.Allocate(2);
        Data.Span[0] = (int)(BitConverter.DoubleToInt64Bits(value) >> 32);
        Data.Span[1] = (int)(BitConverter.DoubleToInt64Bits(value) & 0xFFFFFFFF);
        size = sizeof(double) * 8;
        size = sizeof(double) * 8;

    }
    public LiteralFloat(Span<int> words)
    {
        Data = MemoryOwner<int>.Allocate(words.Length);
        if (words.Length == 2)
        {
            size = sizeof(long) * 8;
            Data = MemoryOwner<int>.Allocate(2);
            Data.Span[0] = words[0] << 32 | words[1];
        }
        else if (words.Length == 1)
        {
            size = sizeof(int) * 8;
            Data = MemoryOwner<int>.Allocate(1);
            Data.Span[0] = words[0];
        }

    }


    public static implicit operator LiteralFloat(Half value) => new(value);
    public static implicit operator LiteralFloat(float value) => new(value);
    public static implicit operator LiteralFloat(double value) => new(value);
    // public static implicit operator LiteralInteger(LiteralFloat value) => new(value.Data);
    public static implicit operator float(LiteralFloat value) => BitConverter.Int32BitsToSingle((int)value.Data.Span[0]);
    public static implicit operator double(LiteralFloat value) => BitConverter.Int64BitsToDouble(value.Data.Span[0]);



    // public readonly bool TryCast(out Half value)
    // {
    //     short bits = (short)(Data & 0X000000FF);
    //     if (size == 32)
    //     {
    //         value = BitConverter.Int16BitsToHalf(bits);
    //         return true;
    //     }
    //     else
    //     {
    //         value = Half.Zero;
    //         return false;
    //     }
    // }
    // public readonly bool TryCast(out float value)
    // {
    //     Span<int> span =
    //     [
    //         (int)(Data >> 32),
    //         (int)(Data & 0X0000FFFF)
    //     ];
    //     if (size == 32)
    //     {
    //         value = BitConverter.Int32BitsToSingle(span[1]);
    //         return true;
    //     }
    //     else
    //     {
    //         value = 0;
    //         return false;
    //     }
    // }
    // public readonly bool TryCast(out double value)
    // {
    //     if (size == 64)
    //     {
    //         value = BitConverter.Int64BitsToDouble(Data);
    //         return true;
    //     }
    //     else
    //     {
    //         value = 0;
    //         return false;
    //     }
    // }





    public static LiteralFloat From(Span<int> words) => new(words);

    public static LiteralFloat From(string value)
    {
        throw new NotImplementedException();
    }

    public readonly override string ToString()
    {
        return size switch
        {
            16 => $"{BitConverter.UInt16BitsToHalf((ushort)(Data.Span[0] & 0xFFFF))}",
            32 => $"{BitConverter.Int32BitsToSingle(Data.Span[0])}",
            64 => $"{BitConverter.Int64BitsToDouble(Data.Span[0] << 32 | Data.Span[1])}",
            _ => throw new NotImplementedException()
        };
    }

    public void Dispose()
    {
        throw new NotImplementedException();
    }
}