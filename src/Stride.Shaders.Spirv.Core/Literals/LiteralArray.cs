using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CommunityToolkit.HighPerformance.Buffers;

namespace Stride.Shaders.Spirv.Core;



public static class LiteralArrayHelper
{
    public static LiteralArray<T> Create<T>(ReadOnlySpan<T> elements)
    {
        return new LiteralArray<T>(elements);
    }
}

[CollectionBuilder(typeof(LiteralArrayHelper), "Create")]
public struct LiteralArray<T> : ISpirvElement, IFromSpirv<LiteralArray<T>>, IDisposable
{
    static LiteralArray()
    {
        LiteralArray<T> v = default;
        _ = v switch
        {
            LiteralArray<sbyte>
            or LiteralArray<short>
            or LiteralArray<int>
            or LiteralArray<long>
            or LiteralArray<byte>
            or LiteralArray<ushort>
            or LiteralArray<uint>
            or LiteralArray<ulong>
            or LiteralArray<Half>
            or LiteralArray<float>
            or LiteralArray<double>
            or LiteralArray<string>
            or LiteralArray<bool>
            or LiteralArray<(int, int)> => true,
            _ => throw new Exception("Type not supported in SPIR-V")
        };
    }

    MemoryOwner<int> Memory { get; set { field?.Dispose(); field = value; } }
    public readonly ReadOnlySpan<int> Words => Memory is not null ? Memory.Span : [];
    public MemoryOwner<T> Elements { get; set { field?.Dispose(); field = value; } }
    public readonly int WordCount => Elements?.Length ?? -1;


    public LiteralArray()
    {
        Elements = MemoryOwner<T>.Empty;
        Memory = MemoryOwner<int>.Empty;
    }
    
    public LiteralArray(MemoryOwner<T> elements)
    {
        Elements = elements;
        Memory = MemoryOwner<int>.Empty;
        UpdateWords();
    }
    public LiteralArray(ReadOnlySpan<T> elements)
    {
        Elements = MemoryOwner<T>.Allocate(elements.Length);
        elements.CopyTo(Elements.Span);
        Memory = MemoryOwner<int>.Empty;
        UpdateWords();
    }

    public void Assign(LiteralArray<T> owner)
    {
        Elements?.Dispose();
        Elements = owner.Elements;
        UpdateWords();
    }
    public void Assign(MemoryOwner<T> owner)
    {
        Elements.Dispose();
        Elements = owner;
    }

    public void Assign(Memory<T> span)
    {
        Elements.Dispose();
        Elements = span.Length == 0 ? MemoryOwner<T>.Empty : MemoryOwner<T>.Allocate(span.Length);
        span.CopyTo(Elements.Memory);
    }

    public void Assign(Span<T> span)
    {
        Elements.Dispose();
        Elements = span.Length == 0 ? MemoryOwner<T>.Empty : MemoryOwner<T>.Allocate(span.Length);
        span.CopyTo(Elements.Span);
    }

    public readonly void Dispose() => Elements.Dispose();

    void UpdateWords()
    {
        Memory?.Dispose();
        var memorySize = Elements.Length > 0 && Elements.Span[0] is long or ulong or double or ValueTuple<int, int> ? Elements.Length * 2 : Elements.Length;
        Memory = MemoryOwner<int>.Allocate(memorySize, AllocationMode.Clear);
        var pos = 0;
        foreach (var element in Elements.Span)
        {
            if (element is bool or byte or sbyte or short or ushort or int or uint or float)
            {
                Memory.Span[pos++] = element switch
                {
                    bool b => b ? 1 : 0,
                    byte b => b,
                    sbyte sb => sb,
                    short s => s,
                    ushort us => us,
                    int i => i,
                    uint ui => (int)ui,
                    float f => BitConverter.SingleToInt32Bits(f),
                    _ => throw new NotImplementedException()
                };
            }
            else if (element is long or ulong or double or ValueTuple<int, int>)
            {
                Memory.Span[pos++] = element switch
                {
                    long l => (int)(l >> 32),
                    ulong ul => (int)(ul >> 32),
                    double d => (int)(BitConverter.DoubleToInt64Bits(d) >> 32),
                    ValueTuple<int, int> vt => vt.Item1,
                    _ => throw new NotImplementedException()
                };
                Memory.Span[pos++] = element switch
                {
                    long l => (int)(l & 0xFFFFFFFF),
                    ulong ul => (int)(ul & 0xFFFFFFFF),
                    double d => (int)(BitConverter.DoubleToInt64Bits(d) & 0xFFFFFFFF),
                    ValueTuple<int, int> vt => vt.Item2,
                    _ => throw new NotImplementedException()
                };
            }
            else throw new NotImplementedException();
        }
        
    }

    public readonly Span<T>.Enumerator GetEnumerator() => Elements.Span.GetEnumerator();

    public static LiteralArray<T> From(Span<int> words)
    {
        T value = default!;
        if (value is bool or byte or sbyte or short or ushort or int or uint or float)
        {
            var owner = MemoryOwner<T>.Allocate(words.Length);
            for (int i = 0; i < words.Length; i++)
            {
                if (value is bool)
                {
                    bool b = words[i] != 0;
                    owner.Span[i] = Unsafe.As<bool, T>(ref b);
                }
                else if (value is byte)
                {
                    byte b = (byte)words[i];
                    owner.Span[i] = Unsafe.As<byte, T>(ref b);
                }
                else if (value is sbyte)
                {
                    sbyte b = (sbyte)words[i];
                    owner.Span[i] = Unsafe.As<sbyte, T>(ref b);
                }
                else if (value is short)
                {
                    short b = (short)words[i];
                    owner.Span[i] = Unsafe.As<short, T>(ref b);
                }
                else if (value is ushort)
                {
                    ushort b = (ushort)words[i];
                    owner.Span[i] = Unsafe.As<ushort, T>(ref b);
                }
                else if (value is int)
                {
                    int b = words[i];
                    owner.Span[i] = Unsafe.As<int, T>(ref b);
                }
                else if (value is uint)
                {
                    uint b = (uint)words[i];
                    owner.Span[i] = Unsafe.As<uint, T>(ref b);
                }
                else if (value is float)
                {
                    float b = BitConverter.Int32BitsToSingle(words[i]);
                    owner.Span[i] = Unsafe.As<float, T>(ref b);
                }
            }
            return new(owner);
        }
        else if (value is long or double && words.Length % 2 == 0)
        {
            var owner = MemoryOwner<T>.Allocate(words.Length / 2);
            for (int i = 0; i < words.Length; i += 2)
            {
                if (value is long)
                {
                    long b = words[i] << 32 | words[i + 1];
                    owner.Span[i] = Unsafe.As<long, T>(ref b);
                }
                else if (value is double)
                {
                    double b = BitConverter.Int64BitsToDouble(words[i] << 32 | words[i + 1]);
                    owner.Span[i] = Unsafe.As<double, T>(ref b);
                }
            }
            return new(owner);
        }
        else throw new NotImplementedException();
    }

    public static LiteralArray<T> From(string value)
    {
        throw new NotImplementedException();
    }
}