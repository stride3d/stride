using System.Numerics;
using System.Runtime.InteropServices;
using CommunityToolkit.HighPerformance.Buffers;

namespace Stride.Shaders.Spirv.Core;


public struct LiteralValue<T> : ISpirvElement, IFromSpirv<LiteralValue<T>>
{
    static LiteralValue()
    {
        LiteralValue<T> v = default;
        _ = v switch
        {
            LiteralValue<sbyte>
            or LiteralValue<short>
            or LiteralValue<int>
            or LiteralValue<long>
            or LiteralValue<byte>
            or LiteralValue<ushort>
            or LiteralValue<uint>
            or LiteralValue<ulong>
            or LiteralValue<Half>
            or LiteralValue<float>
            or LiteralValue<double>
            or LiteralValue<string>
            or LiteralValue<bool> => true,

            _ => throw new Exception("Type not supported in SPIR-V")
        };
    }
    public MemoryOwner<int> Words { get; private set; }
    public T Value { get; set { field = value; UpdateMemory(); } }
    public readonly int WordCount => Words.Length;

    public LiteralValue(T value)
    {
        Value = value;
        Words = MemoryOwner<int>.Empty;
    }

    void UpdateMemory()
    {
        int wordCount = Value switch
        {
            bool or byte or sbyte or short or ushort or Half or int or uint or float => 1,
            long or ulong or double => 2,
            string => throw new NotImplementedException("Can't compute string literal value yet"),
            _ => throw new NotImplementedException()
        };
        Words.Dispose();
        Words = MemoryOwner<int>.Allocate(wordCount, AllocationMode.Clear);
        if (Value is bool or byte or sbyte or short or ushort or Half or int or uint or float)
        {
            Words.Span[0] = Value switch
            {
                bool b => b ? 1 : 0,
                byte b => b,
                sbyte b => b,
                short s => s,
                ushort s => s,
                // Half h => h,
                int i => i,
                uint i => (int)i,
                float f => BitConverter.SingleToInt32Bits(f),
                _ => throw new NotImplementedException()
            };
        }
        else if (Value is long or double)
        {
            Words.Span[0] = Value switch
            {
                long l => (int)(l >> 16),
                double d => (int)BitConverter.DoubleToInt64Bits(d) >> 16,
                _ => throw new NotImplementedException()
            };
            Words.Span[1] = Value switch
            {
                long l => (int)(l & 0xFFFFFFFF),
                double d => (int)(BitConverter.DoubleToInt64Bits(d) & 0xFFFFFFFF),
                _ => throw new NotImplementedException()
            };
        }
        else if (Value is string)
        {
            throw new NotImplementedException("Can't process strings yet");
        }
    }

    public static LiteralValue<T> From(Span<int> words)
    {
        throw new NotImplementedException();
    }

    public static LiteralValue<T> From(string value)
    {
        throw new NotImplementedException();
    }

    public readonly void Dispose() => Words.Dispose();

    public static implicit operator LiteralValue<T>(T s) => new(s);
    public static implicit operator T(LiteralValue<T> lv) => lv.Value;
}