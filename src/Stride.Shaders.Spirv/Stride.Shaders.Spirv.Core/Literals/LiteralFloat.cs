using CommunityToolkit.HighPerformance.Buffers;
using Stride.Shaders.Spirv.Core;
using Stride.Shaders.Spirv.Core.Parsing;
using System.Drawing;
using System.Numerics;


namespace Stride.Shaders.Spirv.Core;


public struct LiteralFloat : ILiteralNumber, IFromSpirv<LiteralFloat>
{
    public long Words { get; init; }
    int size;

    public readonly int WordCount => size / 32;

    public LiteralFloat(Half value)
    {
        Words = BitConverter.HalfToInt16Bits(value);
        size = 16;

    }
    public LiteralFloat(float value)
    {
        Words = BitConverter.SingleToInt32Bits(value); ;
        size = sizeof(float) * 8;
    }
    public LiteralFloat(double value)
    {
        Words = BitConverter.DoubleToInt64Bits(value);
        size = sizeof(double) * 8;

    }
    public LiteralFloat(Span<int> words)
    {
        if (words.Length == 2)
        {
            size = sizeof(long) * 8;
            Words = words[0] << 32 | words[1];
        }
        else if (words.Length == 1)
        {
            size = sizeof(int) * 8;
            Words = words[0];
        }

    }


    public static implicit operator LiteralFloat(Half value) => new(value);
    public static implicit operator LiteralFloat(float value) => new(value);
    public static implicit operator LiteralFloat(double value) => new(value);
    public static implicit operator LiteralInteger(LiteralFloat value) => new(value.Words);



    public readonly bool TryCast(out Half value)
    {
        short bits = (short)(Words & 0X000000FF);
        if (size == 32)
        {
            value = BitConverter.Int16BitsToHalf(bits);
            return true;
        }
        else
        {
            value = Half.Zero;
            return false;
        }
    }
    public readonly  bool TryCast(out float value)
    {
        Span<int> span = stackalloc int[]
        {
            (int)(Words >> 32),
            (int)(Words & 0X0000FFFF)
        };
        if (size == 32)
        {
            value = BitConverter.Int32BitsToSingle(span[1]);
            return true;
        }
        else
        {
            value = 0;
            return false;
        }
    }
    public readonly  bool TryCast(out double value)
    {
        if (size == 64)
        {
            value = BitConverter.Int64BitsToDouble(Words);
            return true;
        }
        else
        {
            value = 0;
            return false;
        }
    }



    public readonly void Write(ref SpirvWriter writer)
    {
        Span<int> span =
        [
            (int)(Words >> 32),
            (int)(Words & 0xFFFFFFFF)
        ];
        if (size < 64)
            writer.Write(span[1]);
        else
            writer.Write(span);
    }

    public static LiteralFloat From(Span<int> words) => new(words);

    public static LiteralFloat From(string value)
    {
        throw new NotImplementedException();
    }
    public readonly SpanOwner<int> AsSpanOwner()
    {
        Span<int> span = WordCount == 1 ? [ (int)Words ] : [ (int)(Words >> 32), (int)(Words & 0xFFFFFFFF) ];
        var owner = SpanOwner<int>.Allocate(span.Length, AllocationMode.Clear);
        span.CopyTo(owner.Span);
        return owner;
    }
}