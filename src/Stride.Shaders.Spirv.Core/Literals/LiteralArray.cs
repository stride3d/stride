using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CommunityToolkit.HighPerformance.Buffers;

namespace Stride.Shaders.Spirv.Core;



public static class LiteralArrayHelper
{
    public static LiteralArray<T> Create<T>(ReadOnlySpan<T> elements)
        where T : struct, ISpirvElement, IFromSpirv<T>
    {
        return new LiteralArray<T>(elements);
    }
}

[CollectionBuilder(typeof(LiteralArrayHelper), "Create")]
public struct LiteralArray<T> : ISpirvElement, IFromSpirv<LiteralArray<T>>, IDisposable
    where T : struct, ISpirvElement, IFromSpirv<T>
{

    MemoryOwner<T> Words { get; set { field.Dispose(); field = value; } }
    public readonly int WordCount => Words.Length;

    public LiteralArray(ReadOnlySpan<T> words)
    {
        Words = MemoryOwner<T>.Allocate(words.Length);
        words.CopyTo(Words.Span);
    }

    public void Assign(LiteralArray<T> owner)
    {
        Words.Dispose();
        Words = owner.Words;
    }
    public void Assign(MemoryOwner<T> owner)
    {
        Words.Dispose();
        Words = owner;
    }

    public void Assign(Memory<T> span)
    {
        Words.Dispose();
        Words = MemoryOwner<T>.Allocate(span.Length);
        span.CopyTo(Words.Memory);
    }

    public void Assign(Span<T> span)
    {
        Words.Dispose();
        Words = MemoryOwner<T>.Allocate(span.Length);
        span.CopyTo(Words.Span);
    }

    public static LiteralArray<T> From(Span<int> words)
    {
        T tmp = default;
        if (tmp is IdRef or IdResult or IdResultType or IdScope or LiteralInteger)
        {
            using var owner = SpanOwner<T>.Allocate(words.Length, AllocationMode.Clear);
            for (int i = 0; i < words.Length; i++)
                owner.Span[i] = T.From([words[i]]);
            return new LiteralArray<T>(owner.Span);
        }
        else if (tmp is PairIdRefIdRef or PairIdRefLiteralInteger or PairLiteralIntegerIdRef)
        {
            using var owner = SpanOwner<T>.Allocate(words.Length / 2, AllocationMode.Clear);
            for (int i = 0; i < words.Length; i += 2)
                owner.Span[i / 2] = T.From([words[i], words[i + 1]]);
            return new LiteralArray<T>(owner.Span);
        }
        else throw new NotImplementedException($"Can't process type {typeof(T).FullName}");
    }

    public static LiteralArray<T> From(string value)
    {
        throw new NotImplementedException();
    }

    public readonly SpanOwner<int> AsSpanOwner()
    {
        T tmp = default;
        if (tmp is IdRef or IdResult or IdResultType or IdScope or LiteralInteger)
        {
            var owner = SpanOwner<int>.Allocate(Words.Length, AllocationMode.Clear);
            for (int i = 0; i < Words.Length; i++)
                owner.Span[i] = Words.Span[i].AsSpirvSpan()[0];
            return owner;
        }
        else if (tmp is PairIdRefIdRef or PairIdRefLiteralInteger or PairLiteralIntegerIdRef)
        {
            using var owner = SpanOwner<int>.Allocate(Words.Length * 2, AllocationMode.Clear);
            for (int i = 0; i < Words.Length; i += 2)
            {
                owner.Span[i] = Words.Span[i].AsSpirvSpan()[0];
                owner.Span[i + 1] = Words.Span[i].AsSpirvSpan()[1];
            }
            return owner;
        }
        else throw new NotImplementedException($"Can't process type {typeof(T).FullName}");
    }
    public readonly void Dispose() => Words.Dispose();

    public readonly Span<T>.Enumerator GetEnumerator() => Words.Span.GetEnumerator();
}