using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using CommunityToolkit.HighPerformance.Buffers;

namespace Stride.Shaders.Spirv.Core;

public struct Id(int id)
{
    public int Value { get; set; } = id;

    public static implicit operator Id(int v) => new(v);
    public static implicit operator int(Id id) => id.Value;
}
internal static class SPool
{
    internal static StringPool Instance { get; } = new();
    public static string GetOrAdd(ReadOnlySpan<char> value) => Instance.GetOrAdd(value);
}

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
            or LiteralValue<bool>
            or LiteralValue<(int, int)> => true,
            _ => throw new Exception("Type not supported in SPIR-V")
        };
    }
    public bool dispose;
    public MemoryOwner<int> MemoryOwner { get; private set; }
    public readonly ReadOnlySpan<int> Words => MemoryOwner.Span;
    public T Value { get; set { field = value; if(MemoryOwner is not null) UpdateMemory(); } }
    public readonly int WordCount => Words.Length;

    public LiteralValue(Span<int> words, bool dispose = false)
    {
        this.dispose = dispose;
        T value = default!;
        if (value is bool)
            Unsafe.As<T, bool>(ref value) = words[0] != 0;
        else if (value is byte)
            Unsafe.As<T, byte>(ref value) = (byte)words[0];
        else if (value is sbyte)
            Unsafe.As<T, sbyte>(ref value) = (sbyte)words[0];
        else if (value is short)
            Unsafe.As<T, short>(ref value) = (short)words[0];
        else if (value is ushort)
            Unsafe.As<T, ushort>(ref value) = (ushort)words[0];
        else if (value is uint)
            Unsafe.As<T, uint>(ref value) = (uint)words[0];
        else if (value is int)
            Unsafe.As<T, int>(ref value) = words[0];
        else if (value is float)
            Unsafe.As<T, float>(ref value) = BitConverter.Int32BitsToSingle(words[0]);
        else if (value is long)
            Unsafe.As<T, long>(ref value) = ((long)words[0] << 32) | (uint)words[1];
        else if (value is ulong)
            Unsafe.As<T, ulong>(ref value) = ((ulong)words[0] << 32) | (uint)words[1];
        else if (value is double)
            Unsafe.As<T, double>(ref value) = BitConverter.Int64BitsToDouble(((long)words[0] << 32) | (uint)words[1]);
        else if (value is ValueTuple<int, int>)
            Unsafe.As<T, (int, int)>(ref value) = (words[0], words[1]);
        else if(value is null && typeof(T) == typeof(string))
        {
            Span<char> sb = stackalloc char[words.Length * 4];
            for (int i = 0; i < words.Length; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    var c = (char)((words[i] >> (8 * j)) & 0xFF);
                    if (c == 0)
                        break;
                    sb[i * 4 + j] = c;
                }
            }
            Unsafe.As<T, string>(ref value) = SPool.GetOrAdd(sb.Contains('\0') ? sb[0..sb.IndexOf('\0')] : sb);
        }
        else if (value is Enum)
            Unsafe.As<T, int>(ref value) = words[0];
        else if (typeof(T).Name.Contains("LiteralArray"))
            throw new NotImplementedException("Use LiteralArray<T>.From instead");
        else
            throw new NotImplementedException("Cannot create LiteralValue from the provided words");

        Value = value;

        MemoryOwner = MemoryOwner<int>.Allocate(words.Length, AllocationMode.Clear);
        words.CopyTo(MemoryOwner.Span);
        UpdateMemory();
    }
    public LiteralValue(T value, bool dispose = false)
    {
        this.dispose = dispose;
        Value = value;
        MemoryOwner = MemoryOwner<int>.Empty;
        UpdateMemory();
    }

    void UpdateMemory()
    {
        int wordCount = Value switch
        {
            bool or byte or sbyte or short or ushort or Half or int or uint or float => 1,
            long or ulong or double or ValueTuple<int, int> => 2,
            Enum => 1,
            string s => s.GetWordCount(),
            null => 0,
            _ => throw new NotImplementedException("Can't compute literal value for type " + typeof(T))
        };
        if(MemoryOwner == null)
            MemoryOwner = MemoryOwner<int>.Empty;
        else MemoryOwner.Dispose();
        MemoryOwner = MemoryOwner<int>.Allocate(wordCount, AllocationMode.Clear);
        if (Value is bool or byte or sbyte or short or ushort or Half or int or uint or float)
        {
            MemoryOwner.Span[0] = Value switch
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
        else if (Value is long or double or ValueTuple<int, int>)
        {
            MemoryOwner.Span[0] = Value switch
            {
                long l => (int)(l >> 16),
                double d => (int)BitConverter.DoubleToInt64Bits(d) >> 16,
                ValueTuple<int, int> vt => vt.Item1,
                _ => throw new NotImplementedException()
            };
            MemoryOwner.Span[1] = Value switch
            {
                long l => (int)(l & 0xFFFFFFFF),
                double d => (int)(BitConverter.DoubleToInt64Bits(d) & 0xFFFFFFFF),
                ValueTuple<int, int> vt => vt.Item2,
                _ => throw new NotImplementedException()
            };
        }
        else if (Value is string s)
        {
            for (int i = 0; i < s.Length; i++)
            {
                var pos = i / 4;
                var shift = 8 * (i % 4);
                var value = i < s.Length ? s[i] : '\0';
                MemoryOwner.Span[pos] |= value << shift;
            }
        }
    }

    public static LiteralValue<T> From(Span<int> words)
    {
        T value = default!;
        return (value, words.Length) switch
        {
            (bool or byte or sbyte or short or ushort or Half or int or uint or float, 1) => new LiteralValue<T>(words),
            (long or ulong or double or ValueTuple<int, int>, 2) => new LiteralValue<T>(words),
            (string, > 0) => new LiteralValue<T>(words),
            _ => throw new NotImplementedException("Cannot create LiteralValue from the provided words")
        };
    }

    public static LiteralValue<T> From(string value)
    {
        throw new NotImplementedException();
    }

    public readonly void Dispose() => MemoryOwner.Dispose();

    public static implicit operator LiteralValue<T>(T s) => new(s);
    public static implicit operator T(LiteralValue<T> lv) => lv.Value;


    public readonly Enumerator GetEnumerator() => new(MemoryOwner, dispose);

    public ref struct Enumerator(MemoryOwner<int> memory, bool dispose)
    {
        Span<int>.Enumerator enumerator = memory.Span.GetEnumerator();

        public int Current => enumerator.Current;
        public bool MoveNext()
        {
            if (!enumerator.MoveNext())
            {
                if (dispose)
                    memory.Dispose();
                return false;
            }
            else return true;
        }
    }
}



public static class LiteralValueExtensions
{
    public static LiteralValue<T> AsLiteralValue<T>(this T value) where T : struct, INumber<T> => new(value);
    public static LiteralValue<T> AsLiteralValue<T>(this T? value) where T : struct, INumber<T> => new(value ?? default);
    public static LiteralValue<bool> AsLiteralValue(this bool value) => new(value);
    public static LiteralValue<(int, int)> AsLiteralValue(this (int, int) value) => new(value);
    public static LiteralValue<string> AsLiteralValue(this string value) => new(value);

    public static LiteralValue<T> AsDisposableLiteralValue<T>(this T value) where T : struct, INumber<T> => new(value, true);
    public static LiteralValue<T> AsDisposableLiteralValue<T>(this T? value) where T : struct, INumber<T> => new(value ?? default, true);
    public static LiteralValue<bool> AsDisposableLiteralValue(this bool value) => new(value, true);
    public static LiteralValue<(int, int)> AsDisposableLiteralValue(this (int, int) value) => new(value, true);
    public static LiteralValue<string> AsDisposableLiteralValue(this string value) => new(value, true);
}


static class TemporaryArrayBuilder
{
    public static TemporaryArray Create(ReadOnlySpan<int> values) => new(values);


    static void Something()
    {
        var x = "hello".AsLiteralValue();
        TemporaryArray tmp = [.. x];
    }
}


[CollectionBuilder(typeof(TemporaryArrayBuilder), "Create")]
struct TemporaryArray : IDisposable
{
    MemoryOwner<int> Memory { get; set; }

    int Length { get; set; }

    public TemporaryArray(ReadOnlySpan<int> initialValues)
    {
        Memory = MemoryOwner<int>.Allocate((int)BitOperations.RoundUpToPowerOf2((uint)initialValues.Length), AllocationMode.Clear);
        initialValues.CopyTo(Memory.Span);
        Length = initialValues.Length;
    }


    void Expand(int size)
    {
        if (Length + size > Memory.Length)
        {
            MemoryOwner<int> newMemory = MemoryOwner<int>.Allocate(Memory.Length * 2, AllocationMode.Clear);
            Memory.Span.CopyTo(newMemory.Span);
            Memory.Dispose();
            Memory = newMemory;
        }
    }

    public void Add<T>(LiteralValue<T> item)
    {
        Expand(item.WordCount);
        item.Words.CopyTo(Memory.Span[Length..]);
        Length += item.WordCount;
    }
    public void Add<T>(LiteralArray<T> item)
    {
        Expand(item.WordCount);
        item.Words.CopyTo(Memory.Span[Length..]);
        Length += item.WordCount;
    }
    public void Add(int value)
    {
        Expand(1);
        Memory.Span[Length] = value;
        Length += 1;
    }

    public readonly Span<int>.Enumerator GetEnumerator() => Memory.Span[..Length].GetEnumerator();
    public readonly void Dispose() => Memory.Dispose();
}